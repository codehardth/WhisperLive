import asyncio
import json
import os
from typing import Tuple
from websockets import WebSocketServerProtocol
from websockets.server import serve
from websockets.protocol import State

from whisper_live.client import TranscriptionClient
from whisper_live.enums.model_type import ModelType

transcription_server_address = os.environ['TRASCRIPTION_SERVER_ADDR']
transcription_server_port = os.environ['TRANSCRIPTION_SERVER_PORT']
client_address = os.environ['CLIENT_ADDR']
client_port = os.environ['CLIENT_PORT']

async def echo(websocket: WebSocketServerProtocol):
    async def handler(data : Tuple) -> any:        
        list = [e for e in data]
        res = json.dumps({
            'messages': list
        }, ensure_ascii=False)

        await websocket.send(res)

    headers = websocket.request_headers
    cookieText = headers.get("Cookie")
    cookies = dict(item.split('=') for item in cookieText.split('; '))

    device_index_str = cookies.get('x-device-index')
    device_index = int(device_index_str) if device_index_str is not None else None

    hls_uri = cookies.get('x-hls-url')

    file_path = cookies.get('x-file-path')

    model_type = ModelType.parse(cookies.get('x-model-type'))
    model_size = cookies.get('x-model-size')
    lang = cookies.get('x-language')
    is_multilingual_str = cookies.get('x-is-multilang')
    is_multilingual = is_multilingual_str.lower() == 'true' if is_multilingual_str is not None else False

    client = TranscriptionClient(
        transcription_server_address if transcription_server_address is not None else "0.0.0.0",
        int(transcription_server_port) if transcription_server_port is not None else 9090,
        is_multilingual=is_multilingual,
        lang=lang,
        translate=False,
        model_type=model_type,
        model_size=model_size,
        callback=handler,
        replay_playback=True,
        playback_device_index=device_index,
    )

    client.start(audio=file_path, hls_url=hls_uri)

    while websocket.state is State.OPEN:
        await asyncio.sleep(1)

    client.stop()

    await websocket.close()

    print('connection closed gracefully.')

async def main():
    addr = client_address if client_address is not None else "0.0.0.0"
    port = int(client_port) if client_port is not None else 8765
    async with serve(echo, addr, port):
        print(f'Client started at port {port}')
        await asyncio.Future()

asyncio.run(main())
