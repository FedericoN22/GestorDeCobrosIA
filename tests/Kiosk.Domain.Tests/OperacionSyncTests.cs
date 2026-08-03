using Kiosk.Domain.Sync;

namespace Kiosk.Domain.Tests;

public class OperacionSyncTests
{
    [Fact]
    public void Registrar_AsignaPropiedades()
    {
        var comercioId = Guid.NewGuid();
        var operationId = Guid.NewGuid();

        var op = OperacionSync.Registrar(comercioId, operationId, "VENTA", """{"ventaId":"abc"}""");

        Assert.Equal(comercioId, op.ComercioId);
        Assert.Equal(operationId, op.OperationId);
        Assert.Equal("VENTA", op.Tipo);
        Assert.Equal("""{"ventaId":"abc"}""", op.ResultadoJson);
        Assert.Null(op.ConfirmadaEn);
    }

    [Fact]
    public void Registrar_ConOperationIdVacio_LanzaError()
    {
        AssertHelper.ThrowsDomain(
            "OPERATION_ID_INVALIDO",
            () => OperacionSync.Registrar(Guid.NewGuid(), Guid.Empty, "VENTA", "{}"));
    }

    [Fact]
    public void Registrar_ConTipoVacio_LanzaError()
    {
        AssertHelper.ThrowsDomain(
            "OPERACION_TIPO_REQUERIDO",
            () => OperacionSync.Registrar(Guid.NewGuid(), Guid.NewGuid(), "   ", "{}"));
    }

    [Fact]
    public void Confirmar_AsignaFecha()
    {
        var op = OperacionSync.Registrar(Guid.NewGuid(), Guid.NewGuid(), "VENTA", "{}");

        op.Confirmar();

        Assert.NotNull(op.ConfirmadaEn);
    }
}
