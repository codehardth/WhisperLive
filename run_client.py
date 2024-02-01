import asyncio
from whisper_live.client import TranscriptionClient
from whisper_live.enums.model_type import ModelType

async def handler(data):
    print(data)

    await asyncio.sleep(1)

fun = handler

client = TranscriptionClient(
    "0.0.0.0",
    9090,
    is_multilingual=True,
    lang="th",
    translate=False,
    model_type=ModelType.WhisperX,
    model_size="large-v2",
    callback=handler,
    replay_playback=True,
    timeout_second=300,
    playback_device_index=8
)

client.start()

print(list(map(lambda fl: list(fl), client.transcribed_messages())))
