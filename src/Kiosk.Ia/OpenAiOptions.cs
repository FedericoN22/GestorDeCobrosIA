namespace Kiosk.Ia;

public sealed record OpenAiOptions(
    string ApiKey,
    string Modelo = "gpt-4o-mini",
    string ModeloWhisper = "whisper-1");
