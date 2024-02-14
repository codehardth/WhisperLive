using Codehard.SpeechToText.Api;
using Codehard.SpeechToText.Api.Hubs;
using Transcriptor.Py.Wrapper.Abstraction;
using Transcriptor.Py.Wrapper.Implementation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddLogging(c => c.AddConsole());

builder.Services.AddScoped<FileStorage>();
builder.Services.AddScoped<ITranscriptor>(
    static _ => new WhisperTranscriptor(new Uri("ws://localhost:8765")));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.MapDiscoverableHub<TranscriptionHub>();

app.MapControllers();

app.Run();