import asyncio
from whisper_live.client import TranscriptionClient

async def handler(data):
    print(data)

    await asyncio.sleep(1)

fun = handler

client = TranscriptionClient(
    "localhost",
    9090,
    is_multilingual=True,
    lang="th",
    translate=False,
    model_size="large-v2",
    callback=handler,
    replay_playback=True,
    timeout_second=300
)

file="/home/deszolate/Downloads/voice.m4a"
client(audio=file)

print(list(map(lambda fl: list(fl), client.transcribed_messages())))
