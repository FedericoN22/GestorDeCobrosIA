using Kiosk.Application.Abstractions;
using Kiosk.Application.Puertos;
using Kiosk.Application.Puertos.Repositorios;
using Kiosk.Domain.Ventas;

namespace Kiosk.Application.CasosUso.Ventas;

public sealed record AbrirCajaCommand(Guid ComercioId, Guid UsuarioId, int MontoInicialCentavos);

public sealed record CerrarCajaCommand(Guid ComercioId, int MontoEsperadoCentavos, int MontoDeclaradoCentavos);

public sealed record CerrarCajaResult(Guid CajaId, int DiferenciaCentavos);

public sealed class ServicioCaja
{
    private readonly ICajaRepository _cajas;
    private readonly IUnitOfWork _unitOfWork;

    public ServicioCaja(ICajaRepository cajas, IUnitOfWork unitOfWork)
    {
        _cajas = cajas;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CerrarCajaResult>.Ok(new CerrarCajaResult(caja.Id, caja.DiferenciaCentavos!.Value));
    }
}
