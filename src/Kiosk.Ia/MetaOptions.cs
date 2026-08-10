namespace Kiosk.Ia;

public sealed record MetaOptions(
    string? TokenAcceso,
    string? PhoneNumberId,
    string? VerifyToken,
    string? AppSecret)
{
    public bool ModoSimulacion =>
        string.IsNullOrWhiteSpace(TokenAcceso) || string.IsNullOrWhiteSpace(PhoneNumberId);
}
