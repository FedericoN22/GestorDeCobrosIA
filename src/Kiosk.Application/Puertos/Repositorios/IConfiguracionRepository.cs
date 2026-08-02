using Kiosk.Domain.Configuracion;

namespace Kiosk.Application.Puertos.Repositorios;

public interface IConfiguracionRepository
{
    Task<Configuracion?> GetAsync(Guid comercioId, string clave, CancellationToken cancellationToken = default);
    void Add(Configuracion configuracion);
}
