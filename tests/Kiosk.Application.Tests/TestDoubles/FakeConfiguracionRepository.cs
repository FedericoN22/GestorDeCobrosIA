using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Configuracion;

namespace Kiosk.Application.Tests.TestDoubles;

public sealed class FakeConfiguracionRepository : IConfiguracionRepository
{
    private readonly Dictionary<(Guid ComercioId, string Clave), Configuracion> _configuraciones = [];

    public void Set(Guid comercioId, string clave, string valor)
        => _configuraciones[(comercioId, clave)] = Configuracion.Crear(comercioId, clave, valor);

    public Task<Configuracion?> GetAsync(Guid comercioId, string clave, CancellationToken cancellationToken = default)
        => Task.FromResult(_configuraciones.GetValueOrDefault((comercioId, clave)));

    public void Add(Configuracion configuracion)
        => _configuraciones[(configuracion.ComercioId, configuracion.Clave)] = configuracion;
}
