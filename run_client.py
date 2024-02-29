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
    model_type=ModelType.WhisperX,
    model_size="medium",
    callback=handler,
    replay_playback=True,
    timeout_second=30,
    playback_device_index=None
)

# client.start(audio="./tests/we_can_work_it_out.wav")
# client.start(hls_url="https://cdn-live.tpchannel.org/v1/0180e10a4a7809df73070d7d8760/0180e10adac40b8ed59433d5f3ce/main.m3u8")
client.start()

print(list(map(lambda fl: list(fl), client.transcribed_messages())))
