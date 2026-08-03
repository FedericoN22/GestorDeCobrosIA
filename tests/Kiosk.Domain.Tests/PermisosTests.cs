using Kiosk.Domain.Usuarios;

namespace Kiosk.Domain.Tests;

public class PermisosTests
{
    [Fact]
    public void Admin_TieneTodosLosPermisos()
    {
        var permisos = Permisos.Para(Rol.ADMIN);

        Assert.True(Permisos.Todos.IsSubsetOf(permisos));
        Assert.Equal(Permisos.Todos.Count, permisos.Count);
    }

    [Fact]
    public void Cajero_TienePermisosDeOperacion()
    {
        var permisos = Permisos.Para(Rol.CAJERO);

        Assert.Contains(Permisos.ProductosConsultar, permisos);
        Assert.Contains(Permisos.StockConsultar, permisos);
        Assert.Contains(Permisos.VentasRegistrar, permisos);
        Assert.Contains(Permisos.VentasConsultar, permisos);
        Assert.Contains(Permisos.CajasAbrir, permisos);
        Assert.Contains(Permisos.CajasCerrar, permisos);
        Assert.Contains(Permisos.CajasConsultar, permisos);
        Assert.Contains(Permisos.SyncOperar, permisos);
    }

    [Fact]
    public void Cajero_NoTienePermisosDeGestion()
    {
        var permisos = Permisos.Para(Rol.CAJERO);

        Assert.DoesNotContain(Permisos.ProductosGestionar, permisos);
        Assert.DoesNotContain(Permisos.StockGestionar, permisos);
        Assert.DoesNotContain(Permisos.ReportesVer, permisos);
        Assert.DoesNotContain(Permisos.GananciasVer, permisos);
        Assert.DoesNotContain(Permisos.UsuariosGestionar, permisos);
        Assert.DoesNotContain(Permisos.ConfigGestionar, permisos);
        Assert.DoesNotContain(Permisos.AuditoriaVer, permisos);
        Assert.DoesNotContain(Permisos.WhatsappOperar, permisos);
    }

    [Fact]
    public void Cajero_PermisosSonSubconjuntoDelAdmin()
    {
        Assert.True(Permisos.Para(Rol.CAJERO).IsSubsetOf(Permisos.Todos));
    }
}
