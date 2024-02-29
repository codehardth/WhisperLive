import gc
from logging import getLogger
import torch
from transformers import pipeline
from whisper_live.abstraction.transcriber_base import Segment, TranscriberBase, TranscriptionInfo


class ThonburianTranscriber(TranscriberBase):
    __info__ = TranscriptionInfo(
        language="th",
        language_probability=1.0, # FIXME: find a way to calculate the prob
        duration=0,
        duration_after_vad=0,
        transcription_options=None,
        vad_options=None,
        all_language_probs=[]
    )

    def __init__(
        self,
        language: str = "th",
        model: str = "biodatlab/whisper-th-medium-combined",
        device: str = "cpu"
    ):
        self.logger = getLogger()
        pipe = pipeline(
            task="automatic-speech-recognition",
            model=model,
            chunk_length_s=30,
            device=device,
        )
        self.__pipe = pipe

    def transcribe(
        self,
        audio,
        **kwargs
    ):
        try:
            res = self.__pipe(
                audio,
                generate_kwargs={"language": "<|th|>", "task": "transcribe"},
                return_timestamps=True,
                batch_size=16)
            text = res["text"]
            segments = res["chunks"]

            results = [
                Segment(
                    id=index,
                    seek=-1,
                    start=0,
                    end=0,
                    text=a['text'],
                    tokens=[],
                    temperature=-1,
                    avg_logprob=-1,
                    compression_ratio=-1,
                    no_speech_prob=-1,
                    words=[]
                )
                for index, a 
                in enumerate(segments)
            ]

            return results, self.__info__
        finally:
            gc.collect()
            torch.cuda.empty_cache()