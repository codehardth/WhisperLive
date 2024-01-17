from whisper_live.client import TranscriptionClient

def handler(data : frozenset[str]) -> any:
    print(data)

client = TranscriptionClient(
    "localhost",
    9090,
    is_multilingual=True,
    lang="en",
    translate=False,
    model_size="small",
    callback=handler,
    replay_playback=False
)

file="./tests/jfk.flac"
client(audio=file)

print(list(map(lambda fl: list(fl), client.transcribed_messages())))
