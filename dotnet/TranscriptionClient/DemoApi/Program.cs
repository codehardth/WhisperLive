using DemoApi.Handlers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Transcriptor.Py.Wrapper.Abstraction;
using Transcriptor.Py.Wrapper.Implementation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(config =>
    config.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddSingleton<ITranscriptor>(
    static _ => new WhisperTranscriptor(new Uri("ws://localhost:8765")));

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

app.Run();