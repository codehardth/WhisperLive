namespace Codehard.SpeechToText.Api.Abstractions;

public interface IDiscoverableHub
{
    static abstract string Route { get; }
}