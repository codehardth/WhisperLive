const cookie = require('cookie');
const WebSocket = require('ws');


const cookies = [
    {
        key: 'x-hls-url',
        value: 'https://cdn-live.tpchannel.org/v1/0180e10a4a7809df73070d7d8760/0180e10adac40b8ed59433d5f3ce/main.m3u8'
    },
    {
        key: 'x-model-type',
        value: 'WhisperX'
    },
    {
        key: 'x-model-size',
        value: 'large-v2'
    },
    {
        key: 'x-language',
        value: 'th'
    },
    {
        key: 'x-is-multilang',
        value: 'true'
    }
];

const cookieString = cookies.map(c => `${c.key}=${c.value}`).join('; ');

const ws = new WebSocket("ws://localhost:8765", [], {
    'headers': {
        'Cookie': cookieString
    }
});

ws.onmessage = ev => {
    const messages = JSON.parse(ev.data);
    console.log(messages);
    // messages.forEach(message => {
    //     console.log(message.text);
    // });
};

setTimeout(() => console.log('done'), 1000000);