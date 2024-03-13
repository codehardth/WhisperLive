using DemoApi.Handlers;
using Hangfire;
using Hangfire.MemoryStorage;
using MediatR;
using Transcriptor.Py.Wrapper.Abstraction;
using Transcriptor.Py.Wrapper.Enums;
using Transcriptor.Py.Wrapper.Implementation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(config =>
    config.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();

builder.Services.AddSingleton(
    new WhisperTranscriptorOptions(
        ModelType.Default,
        "large-v2",
        null,
        true,
        1));
builder.Services.AddTransient<ITranscriptor>(
    static _ => new WhisperTranscriptor(new Uri("ws://192.168.20.98:8765")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet(
        "/api/transcribe/devices",
        (ITranscriptor transcriptor) => transcriptor.GetInputInterfacesAsync())
    .WithName("Get input devices")
    .WithOpenApi();

app.MapPost("/api/transcribe/{index:int}",
    async (int index,
        IMediator mediator,
        CancellationToken cancellationToken = default) =>
    {
        var sessionId = await mediator.Send(new MessageTranscribeStartRequest(index), cancellationToken);

        return Results.Ok(sessionId);
    });

app.MapPost("/api/sessions/{sessionId:guid}",
    async (
        Guid sessionId,
        IMediator mediator,
        CancellationToken cancellationToken = default) =>
    {
        await mediator.Send(new MessageTranscribeStopRequest(sessionId), cancellationToken);

        return Results.NoContent();
    });

app.Run();