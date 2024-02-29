import subprocess
import torch
import whisperx
import numpy as np
import os
import contextlib
import wave
from pyannote.core import Segment
from pyannote.audio import Pipeline, Audio, Inference, Model
from pyannote.audio.pipelines.speaker_verification import PretrainedSpeakerEmbedding
from sklearn.cluster import AgglomerativeClustering

dr_model = "pyannote/speaker-diarization@2.1"
dr_token = "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"

def convert_audio_file(path: str) -> str:
    split = path.split('.')
    last = split[-1]

    if last == 'wav':
        return path
    
    new_path = '.'.join(split[:-1]) + '.wav'

    if os.path.exists(new_path) is False:
        subprocess.call(['ffmpeg', '-i', path, new_path, '-y'])

    return new_path

def transcribe(path: str, model_size: str):
    model = whisperx.load_model(
        whisper_arch=model_size,
        device="cuda"
    )
    result = model.transcribe(path)
    segments = result["segments"]

    return segments

def get_duration(path: str):
    with contextlib.closing(wave.open(path, 'r')) as f:
        frames = f.getnframes()
        rate = f.getframerate()
        duration = frames / float(rate)

        return duration

# infer_model = Model.from_pretrained("pyannote/embedding", use_auth_token=dr_token)
# audio = Inference(infer_model, window="whole")
# audio.to(torch.device("cuda"))
audio = Audio()
emb_model = "pyannote/embedding"
embedding_model = PretrainedSpeakerEmbedding(emb_model)
def segment_embedding(segment: dict, path: str, duration: float):
    start = segment["start"]
    end = min(duration, segment["end"])

    clip = Segment(start, end)
    waveform, sample_rate = audio.crop(path, clip)
    # embedding = audio.crop(path, clip)

    inp = waveform[None]
    return embedding_model(inp)
    # return embedding

pipeline = Pipeline.from_pretrained(
    checkpoint_path=dr_model,
    use_auth_token=dr_token)

number_of_speakers = 2
lang = 'th'
asr_model_size = 'medium'

# file = "./tests/multi.mp3"
path = "./tests/multi.mp3"
path = convert_audio_file(path)
duration = get_duration(path)

segments = transcribe(path, asr_model_size)
embeddings = np.zeros(shape=(len(segments), 192))
for i, segment in enumerate(segments):
  embeddings[i] = segment_embedding(segment, path, duration)

embeddings = np.nan_to_num(embeddings)

clustering = AgglomerativeClustering(number_of_speakers).fit(embeddings)
labels = clustering.labels_
for i in range(len(segments)):
  segments[i]["speaker"] = 'SPEAKER ' + str(labels[i] + 1)

print(segments)

# diarization = pipeline(path)

# print(diarization)