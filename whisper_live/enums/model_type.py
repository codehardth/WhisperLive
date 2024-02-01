
from enum import StrEnum


class ModelType(StrEnum):
    Default = "Default"
    WhisperX = "WhisperX"
    Thonburian = "Thonburian"

    @staticmethod
    def parse(text: str):
        if text == "WhisperX":
            return ModelType.WhisperX
        elif text == "Thonburian":
            return ModelType.Thonburian
        else:
            return ModelType.Default
