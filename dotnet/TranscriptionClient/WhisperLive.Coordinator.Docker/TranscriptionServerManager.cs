using Docker.DotNet;
using Docker.DotNet.Models;
using WhisperLive.Abstraction;
using WhisperLive.Abstraction.Models;
using WhisperLive.Coordinator.Docker.Configurations;

namespace WhisperLive.Coordinator.Docker;

public class TranscriptionServerManager(
    IDockerClient client,
    TranscriptionManagerOptions options)
    : ITranscriptionServerManager
{
    public async Task<AsrInstanceInfo> StartInstanceAsync(
        int port,
        string tag = "latest",
        CancellationToken cancellationToken = default)
    {
        const string imageName = "chcommon.azurecr.io/whisper-asr-server";

        var progress = new Progress<JSONMessage>();

        var images = await client.Images.ListImagesAsync(new ImagesListParameters { }, cancellationToken);

        if (images != null && !images.Any(i => i.RepoTags.Any(t => t == $"{imageName}:{tag}")))
        {
            var fileName = $"{imageName.Replace("/", "_")}.{tag}.tar";
            var path = Path.Combine(options.ImageCacheDirectory, fileName);
            await using var fs = File.Open(path, FileMode.Open);
            await client.Images.LoadImageAsync(new ImageLoadParameters
            {
                Quiet = true,
            }, fs, progress, cancellationToken);
        }

        var containers =
            await client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true,
            }, cancellationToken);

        var uri = options.HostIp;
        var publicPort = port.ToString();

        // Stopped containers are not bound to any port
        var container =
            containers
                .SingleOrDefault(c =>
                    c.Ports.Any(p => p.IP == options.HostIp && p.PublicPort == port));
        if (container is not null)
        {
            if (container.State == "running")
            {
                return new AsrInstanceInfo(container.ID, new UriBuilder(uri) { Port = port }.Uri);
            }

            var canStart = await client.Containers.StartContainerAsync(
                container.ID, new ContainerStartParameters(),
                cancellationToken: cancellationToken);

            if (!canStart)
            {
                throw new Exception($"Unable to start container {container.ID} normally.");
            }

            return new AsrInstanceInfo(container.ID, new UriBuilder(uri) { Port = port }.Uri);
        }

        const string containerPort = "9090/tcp";
        var portBindings = new Dictionary<string, IList<PortBinding>>();
        var hostPortBinding = new PortBinding
        {
            HostIP = options.HostIp,
            HostPort = publicPort,
        };

        var exposedPorts = new Dictionary<string, EmptyStruct>
        {
            {
                containerPort, new EmptyStruct()
            }
        };

        portBindings.Add(containerPort, new List<PortBinding> { hostPortBinding });

        var response = await client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = $"{imageName}:{tag}",
            AttachStdout = true,
            AttachStderr = true,
            ExposedPorts = exposedPorts,
            HostConfig = new HostConfig
            {
                PortBindings = portBindings,
                Runtime = tag switch
                {
                    "gpu" => "nvidia",
                    _ => null,
                },
                Devices = tag == "gpu"
                    ?
                    [
                        new DeviceMapping
                        {
                            PathOnHost = "/dev/nvidia0",
                            PathInContainer = "/dev/nvidia0",
                            CgroupPermissions = "rwm",
                        }
                    ]
                    : Array.Empty<DeviceMapping>(),
            }
        }, cancellationToken);

        var succ = await client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters { },
            cancellationToken);

        if (!succ)
        {
            throw new Exception($"Unable to start container {response.ID} normally.");
        }

        return new AsrInstanceInfo(response.ID, new UriBuilder(uri) { Port = port }.Uri);
    }

    public async Task StopInstanceAsync(string id, CancellationToken cancellationToken = default)
    {
        var containers =
            await client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true,
            }, cancellationToken);

        if (containers.All(c => c.ID != id))
        {
            return;
        }

        await client.Containers.StopContainerAsync(id, new ContainerStopParameters { }, cancellationToken);
    }

    public async Task RemoveInstanceAsync(string id, CancellationToken cancellationToken = default)
    {
        var containers =
            await client.Containers.ListContainersAsync(
                new ContainersListParameters
                {
                    All = true,
                }, cancellationToken);

        var container = containers.SingleOrDefault(c => c.ID == id);

        if (container is not null)
        {
            await client.Containers.RemoveContainerAsync(
                container.ID,
                new ContainerRemoveParameters
                {
                    Force = true,
                },
                cancellationToken);
        }
    }
}