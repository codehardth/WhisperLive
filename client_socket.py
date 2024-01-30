import asyncio
import json
from typing import Tuple
from websockets import WebSocketServerProtocol
from websockets.server import serve
from websockets.protocol import State

from whisper_live.client import TranscriptionClient

file = "./tests/jfk.flac"

async def echo(websocket: WebSocketServerProtocol):
    async def handler(data : Tuple) -> any:        
        list = [e for e in data]
        res = json.dumps({
            'messages': list
        }, ensure_ascii=False)

        await websocket.send(res)

    headers = websocket.request_headers
    device_index = int(headers.get('x-device-index'))

    client = TranscriptionClient(
        "192.168.0.98",
        9090,
        is_multilingual=True,
        lang="th",
        translate=False,
        model_size="large-v2",
        callback=handler,
        replay_playback=False,
        playback_device_index=device_index,
    )

    client.start()

    while websocket.state is State.OPEN:
        await asyncio.sleep(1)

    client.stop()

    await websocket.close()

    print('connection closed gracefully.')

async def main():
    port = 8765
    async with serve(echo, "0.0.0.0", port):
        print(f'Client started at port {port}')
        await asyncio.Future()

asyncio.run(main())
