namespace Kiosk.Domain.Usuarios;

public static class Permisos
{
    public const string ProductosGestionar = "productos.gestionar";
    public const string ProductosConsultar = "productos.consultar";
    public const string StockGestionar = "stock.gestionar";
    public const string StockConsultar = "stock.consultar";
    public const string VentasRegistrar = "ventas.registrar";
    public const string VentasConsultar = "ventas.consultar";
    public const string CajasAbrir = "cajas.abrir";
    public const string CajasCerrar = "cajas.cerrar";
    public const string CajasConsultar = "cajas.consultar";
    public const string SyncOperar = "sync.operar";
    public const string ReportesVer = "reportes.ver";
    public const string GananciasVer = "ganancias.ver";
    public const string UsuariosGestionar = "usuarios.gestionar";
    public const string ConfigGestionar = "config.gestionar";
    public const string AuditoriaVer = "auditoria.ver";
    public const string WhatsappOperar = "whatsapp.operar";

    public static readonly IReadOnlySet<string> Todos = new HashSet<string>
    {
        ProductosGestionar,
        ProductosConsultar,
        StockGestionar,
        StockConsultar,
        VentasRegistrar,
        VentasConsultar,
        CajasAbrir,
        CajasCerrar,
        CajasConsultar,
        SyncOperar,
        ReportesVer,
        GananciasVer,
        UsuariosGestionar,
        ConfigGestionar,
        AuditoriaVer,
        WhatsappOperar
    };

    public static readonly IReadOnlySet<string> Cajero = new HashSet<string>
    {
        ProductosConsultar,
        StockConsultar,
        VentasRegistrar,
        VentasConsultar,
        CajasAbrir,
        CajasCerrar,
        CajasConsultar,
        SyncOperar
    };

    public static IReadOnlySet<string> Para(Rol rol) => rol switch
    {
        Rol.CAJERO => Cajero,
        _ => Todos
    };
}
