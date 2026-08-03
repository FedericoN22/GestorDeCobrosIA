using System.Text.Json;

namespace Kiosk.Pos.Services;

public static class PosJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
