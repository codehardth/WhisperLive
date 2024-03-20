using Docker.DotNet;
using Docker.DotNet.Models;
using Transcriptor.Py.Wrapper.Abstraction;
using Transcriptor.Py.Wrapper.Configurations;

namespace Transcriptor.Py.Wrapper.Implementation;

public class TranscriptionServerManager(
    IDockerClient client,
    TranscriptionManagerOptions options)
    : ITranscriptionServerManager
{
    public async Task<string> StartInstanceAsync(
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

        var uri = options.BaseEndpoint;
        var publicPort = port.ToString();

        // Stopped containers are not bound to any port
        var container =
            containers
                .SingleOrDefault(c =>
                    c.Ports.Any(p => p.IP == uri.Host && p.PublicPort == port));
        if (container is not null)
        {
            if (container.State == "running")
            {
                return container.ID;
            }

            var canStart = await client.Containers.StartContainerAsync(
                container.ID, new ContainerStartParameters(),
                cancellationToken: cancellationToken);

            if (!canStart)
            {
                throw new Exception($"Unable to start container {container.ID} normally.");
            }

            return container.ID;
        }

        const string containerPort = "9090/tcp";
        var portBindings = new Dictionary<string, IList<PortBinding>>();
        var hostPortBinding = new PortBinding
        {
            HostIP = uri.Host,
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
            }
        }, cancellationToken);

        var succ = await client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters { },
            cancellationToken);

        if (!succ)
        {
            throw new Exception($"Unable to start container {response.ID} normally.");
        }

        return response.ID;
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