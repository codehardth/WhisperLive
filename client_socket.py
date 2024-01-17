import asyncio
import json
from websockets.server import serve

from whisper_live.client import TranscriptionClient

file = "./tests/jfk.flac"

async def echo(websocket):
    async def handler(data : frozenset[str]) -> any:        
        list = [e for e in data]
        res = json.dumps({
            'messages': list
        })

        print(res)

        await websocket.send(res)

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

    client(audio=file)

    await websocket.close()

    print('connection closed gracefully.')

async def main():
    port = 8765
    async with serve(echo, "0.0.0.0", port):
        print(f'Client started at port {port}')
        await asyncio.Future()

asyncio.run(main())
