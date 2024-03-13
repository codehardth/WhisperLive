import asyncio
from whisper_live.client import TranscriptionClient
from whisper_live.enums.model_type import ModelType

class bcolors:
    HEADER = '\033[95m'
    OKBLUE = '\033[94m'
    OKCYAN = '\033[96m'
    OKGREEN = '\033[92m'
    WARNING = '\033[93m'
    FAIL = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'
    UNDERLINE = '\033[4m'

async def handler(speaker, data):
    color = bcolors.OKBLUE if speaker == "Speaker 1" else bcolors.OKGREEN
    print(f"{color}--- {speaker}{bcolors.ENDC}")
    for d in data:
        print(f"{color}{d['text']}{bcolors.ENDC}")

    await asyncio.sleep(1)

fun = handler

client = TranscriptionClient(
    "192.168.20.98",
    9090,
    is_multilingual=False,
    lang="th",
    translate=False,
    model_type=ModelType.Default,
    model_size="CodeHardThailand/whisper-th-medium-combined-ct2",
    callback=handler,
    replay_playback=True,
    timeout_second=30,
    playback_device_index=None,
    num_speaker=1
)

# client.start(audio="./tests/we_can_work_it_out.wav")
client.start(hls_url="https://livestream.parliament.go.th/lives/playlist.m3u8")
# client.start()

print(list(map(lambda fl: list(fl), client.transcribed_messages())))
