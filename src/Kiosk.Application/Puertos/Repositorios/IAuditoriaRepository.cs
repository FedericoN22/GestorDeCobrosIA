using Kiosk.Domain.Auditoria;

namespace Kiosk.Application.Puertos.Repositorios;

public interface IAuditoriaRepository
{
    void Add(AuditoriaEvento evento);
}
