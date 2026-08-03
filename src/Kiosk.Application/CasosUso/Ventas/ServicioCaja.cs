using Kiosk.Application.Abstractions;
using Kiosk.Application.Auditoria;
using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Auditoria;
using Kiosk.Domain.Common;
using Kiosk.Domain.Ventas;

namespace Kiosk.Application.CasosUso.Ventas;

public sealed record AbrirCajaCommand(Guid ComercioId, Guid UsuarioId, int MontoInicialCentavos, string Actor, Canal Origen);

public sealed record CerrarCajaCommand(Guid ComercioId, int MontoEsperadoCentavos, int MontoDeclaradoCentavos, string Actor, Canal Origen);

public sealed record CerrarCajaResult(Guid CajaId, int DiferenciaCentavos);

public sealed class ServicioCaja
{
    private readonly ICajaRepository _cajas;
    private readonly IAuditoriaRepository _auditoria;
    private readonly IUnitOfWork _unitOfWork;

    public ServicioCaja(ICajaRepository cajas, IAuditoriaRepository auditoria, IUnitOfWork unitOfWork)
    {
        _cajas = cajas;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> AbrirAsync(AbrirCajaCommand command, CancellationToken cancellationToken = default)
    {
        if (await _cajas.ExisteActivaAsync(command.ComercioId, cancellationToken))
        {
            return Result<Guid>.Fail(new Error("CAJA_YA_ABIERTA", "Ya existe una caja abierta para este comercio."));
        }

        var caja = Caja.Abrir(command.ComercioId, command.UsuarioId, command.MontoInicialCentavos);
        _cajas.Add(caja);
        AuditoriaRegistrador.Registrar(
            _auditoria,
            command.ComercioId,
            command.Origen,
            command.Actor,
            AuditoriaTipos.CajaAbierta,
            new { caja.Id, command.MontoInicialCentavos });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(caja.Id);
    }

    public async Task<Result<CerrarCajaResult>> CerrarAsync(CerrarCajaCommand command, CancellationToken cancellationToken = default)
    {
        var caja = await _cajas.GetActivaAsync(command.ComercioId, cancellationToken);
        if (caja is null)
        {
            return Result<CerrarCajaResult>.Fail(new Error("CAJA_NO_ABIERTA", "No hay una caja abierta para este comercio."));
        }

        caja.Cerrar(command.MontoEsperadoCentavos, command.MontoDeclaradoCentavos);
        AuditoriaRegistrador.Registrar(
            _auditoria,
            command.ComercioId,
            command.Origen,
            command.Actor,
            AuditoriaTipos.CajaCerrada,
            new
            {
                caja.Id,
                caja.MontoEsperadoCentavos,
                caja.MontoDeclaradoCentavos,
                caja.DiferenciaCentavos
            });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CerrarCajaResult>.Ok(new CerrarCajaResult(caja.Id, caja.DiferenciaCentavos!.Value));
    }

    public async Task<Caja?> ObtenerActivaAsync(Guid comercioId, CancellationToken cancellationToken = default)
        => await _cajas.GetActivaAsync(comercioId, cancellationToken);
}
