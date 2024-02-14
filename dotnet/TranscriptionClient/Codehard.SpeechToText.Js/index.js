const signalR = require("@microsoft/signalr");

let connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/transcription")
    .build();

const model = 1;
const modelSize = "large-v2";
const language = "en"
const multiLang = false;

// Transcribe from uploaded file
function streamFromUploadedFile() {
    const fileId = "e4c99a1f-e0bb-4e70-8234-8f6ea5fbc5b6";
    const stream = connection.stream("file", fileId, model, modelSize, language, multiLang);

    stream.subscribe({
        next: (message) => {
            console.log(message);
        },
        complete: () => {
            console.log('--- done ---');
        },
        error: (err) => {
            console.error(err);
        }
    });
}

// Transcribe directly from microphone
function streamFromMicrophone() {
    const deviceIndex = 7;
    const stream = connection.stream("microphone", deviceIndex, model, modelSize, language, multiLang);

    stream.subscribe({
        next: (message) => {
            console.log(message);
        },
        complete: () => {
            console.log('--- done ---');
        },
        error: (err) => {
            console.error(err);
        }
    });
}

// Transcribe from hls streaming
function streamFromHlsStreaming() {
    const uri = "https://cdn-live.tpchannel.org/v1/0180e10a4a7809df73070d7d8760/0180e10adac40b8ed59433d5f3ce/main.m3u8";
    const stream = connection.stream("stream", uri, model, modelSize, language, multiLang);

    stream.subscribe({
        next: (message) => {
            console.log(message);
        },
        complete: () => {
            console.log('--- done ---');
        },
        error: (err) => {
            console.error(err);
        }
    });
}

connection.start().then(streamFromHlsStreaming);