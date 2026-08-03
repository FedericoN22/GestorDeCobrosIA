namespace Kiosk.Pos.Services;

public static class ConfiguracionPos
{
    public static string ApiBaseUrl => "http://localhost:5165";

    public static TimeSpan IntervaloSync { get; set; } = TimeSpan.FromSeconds(15);

    public static string RutaBase =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Kiosk.Pos");
}
