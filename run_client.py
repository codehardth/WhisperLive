import asyncio
from whisper_live.client import TranscriptionClient

async def handler(data):
    print(data)

    await asyncio.sleep(1)

fun = handler

client = TranscriptionClient(
    "192.168.0.98",
    9090,
    is_multilingual=True,
    lang="th",
    translate=False,
    model_size="large-v2",
    callback=handler,
    replay_playback=False,
    timeout_second=300,
    playback_device_index=7
)

client.start(audio="/home/deszolate/Downloads/overreview_resampled.wav")

print(list(map(lambda fl: list(fl), client.transcribed_messages())))
