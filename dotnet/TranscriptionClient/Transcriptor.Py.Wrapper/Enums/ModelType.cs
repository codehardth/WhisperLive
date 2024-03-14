namespace Transcriptor.Py.Wrapper.Enums;

public enum ModelType
{
    Default,
    [Obsolete("WhisperX is no longer maintained, please use `Default` with CTranslate2 model instead.")]
    WhisperX,
    [Obsolete("Thonburian is no longer maintained, please use `Default` with CTranslate2 model instead.")]
    Thonburian,
}