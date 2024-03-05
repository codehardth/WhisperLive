import asyncio
from whisper_live.client import TranscriptionClient
from whisper_live.enums.model_type import ModelType

async def handler(speaker, data):
    print("---" + speaker)
    for d in data:
        print(d['text'])

    await asyncio.sleep(1)

fun = handler

client = TranscriptionClient(
    "0.0.0.0",
    9090,
    is_multilingual=False,
    lang="th",
    translate=False,
    model_type=ModelType.Default,
    model_size="CodeHardTH/whisper-th-medium-combined-ct2",
    callback=handler,
    replay_playback=True,
    timeout_second=30,
    playback_device_index=None
)

# client.start(audio="./tests/we_can_work_it_out.wav")
client.start(hls_url="https://livestream.parliament.go.th/lives/playlist.m3u8")
# client.start()

print(list(map(lambda fl: list(fl), client.transcribed_messages())))
