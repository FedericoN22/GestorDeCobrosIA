using Kiosk.Application.Puertos;

namespace Kiosk.Application.Tests.TestDoubles;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly List<Action> _accionesAlGuardar = [];

    public FakeUnitOfWork(params Action[] accionesAlGuardar)
    {
        _accionesAlGuardar.AddRange(accionesAlGuardar);
    }

    public int SaveChangesLlamadas { get; private set; }

    public bool LanzarError { get; set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesLlamadas++;

        if (LanzarError)
        {
            throw new InvalidOperationException("Falla simulada de persistencia.");
        }

        foreach (var accion in _accionesAlGuardar)
        {
            accion();
        }

        return Task.FromResult(1);
    }
}
