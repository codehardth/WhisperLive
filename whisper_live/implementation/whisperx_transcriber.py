from faster_whisper.utils import get_logger
import numpy as np
import whisperx
import gc
import torch
from whisper_live.abstraction.transcriber_base import TranscriberBase

from whisper_live.implementation.transcriber import Segment, TranscriptionInfo

class WhisperXModel(TranscriberBase):
    __cache_model_dir : str = "/tmp/whisperx_cache"

    def __init__(
        self,
        compute_type: str,
        align_model: str = None,
        language: str = "en",
        model: str = "base",
        device: str = "cpu"
    ):
        self.logger = get_logger()
        self.compute_type = compute_type
        self.align_model = align_model
        self.language = language
        self.device = device
        self.__model = whisperx.load_model(
            whisper_arch=model,
            device=device, 
            compute_type=compute_type,
            language=language,
            download_root=self.__cache_model_dir)
        
    def transcribe(
            self,
            audio,
            **kwargs
    ):
        batch_size = kwargs.get("batch_size", 4)
        
        try:
            audio_source = None

            if isinstance(audio, np.ndarray):
                audio_source = audio
            elif isinstance(audio, str):
                audio_source = whisperx.load_audio(audio)
            else:
                raise TypeError(f"Type {type(audio)} is not supported in this context.")

            result = self.__model.transcribe(audio_source, batch_size)

            language = result["language"]
            segments = result["segments"]
            
            for e in segments:
                e["words"] = e["text"]

            # align_model, metadata = whisperx.load_align_model(
            #     language_code=language,
            #     device=self.device,
            #     model_name=self.align_model,
            #     model_dir=self.__cache_model_dir)

            # align_result = whisperx.align(
            #     segments, 
            #     align_model, 
            #     metadata, 
            #     audio, 
            #     self.device, 
            #     return_char_alignments=False)
            
            # aligned_segments = align_result["segments"]

            info = TranscriptionInfo(
                language=language,
                language_probability=1.0, # FIXME: find a way to calculate the prob
                duration=0,
                duration_after_vad=0,
                transcription_options=None,
                vad_options=None,
                all_language_probs=[]
            )
            result = [
                Segment(
                    id=index,
                    seek=-1,
                    start=a['start'],
                    end=a['end'],
                    text=a['text'],
                    tokens=[],
                    temperature=-1,
                    avg_logprob=-1,
                    compression_ratio=-1,
                    no_speech_prob=-1,
                    words=a['words']
                )
                for index, a 
                in enumerate(segments)
            ]

            return result, info

        finally:
            gc.collect()
            torch.cuda.empty_cache()