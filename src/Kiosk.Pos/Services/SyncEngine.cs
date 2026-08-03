using System.Text.Json;
using Kiosk.Pos.Models;

namespace Kiosk.Pos.Services;

public sealed class EstadoSync
{
    public bool Online { get; init; }
    public int Pendientes { get; init; }
    public int Errores { get; init; }
    public DateTime? UltimaSincronizacion { get; init; }
}

public sealed class SyncEngine : IAsyncDisposable
{
    private readonly ApiClient _api;
    private readonly AlmacenLocal _almacen;
    private readonly SesionManager _sesiones;
    private readonly TimeSpan _intervalo;
    private CancellationTokenSource? _cts;
    private Task? _bucle;
    private bool _sincronizando;

    public SyncEngine(ApiClient api, AlmacenLocal almacen, SesionManager sesiones, TimeSpan? intervalo = null)
    {
        _api = api;
        _almacen = almacen;
        _sesiones = sesiones;
        _intervalo = intervalo ?? TimeSpan.FromSeconds(15);
    }

    public event EventHandler<EstadoSync>? EstadoActualizado;
    public event EventHandler<string>? OperacionConError;
    public event EventHandler<ResultadoOperacionDto>? OperacionRechazada;

    public void Iniciar()
    {
        if (_bucle is not null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _bucle = Task.Run(() => BucleAsync(_cts.Token));
        _ = SincronizarAhoraAsync();
    }

    public async Task DetenerAsync()
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();
        try
        {
            await _bucle!.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        _bucle = null;
        _cts.Dispose();
        _cts = null;
    }

    private async Task BucleAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_intervalo);
        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            await SincronizarAhoraAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task SincronizarAhoraAsync(CancellationToken ct = default)
    {
        if (_sincronizando)
        {
            return;
        }

        _sincronizando = true;
        try
        {
            await SincronizarAsync(ct).ConfigureAwait(false);
            NotificarEstado();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            NotificarEstado(online: false);
        }
        finally
        {
            _sincronizando = false;
        }
    }

    private async Task SincronizarAsync(CancellationToken ct)
    {
        var sesion = _sesiones.Actual;
        if (sesion is null)
        {
            return;
        }

        var pendientes = _almacen.ObtenerPendientes();
        if (pendientes.Count > 0)
        {
            var okParaAck = new List<Guid>();
            var operaciones = new List<OperacionBatchDto>(pendientes.Count);
            foreach (var op in pendientes)
            {
                JsonElement? payload = null;
                if (!string.IsNullOrWhiteSpace(op.Payload))
                {
                    payload = JsonSerializer.Deserialize<JsonElement>(op.Payload, PosJson.Options);
                }

                operaciones.Add(new OperacionBatchDto(op.OperationId, op.Tipo, payload));
            }

            var resultado = await _api.ProcesarBatchAsync(sesion.Token, operaciones, ct).ConfigureAwait(false);
            if (!resultado.Ok)
            {
                NotificarEstado(online: _api.Online);
                return;
            }

            foreach (var res in resultado.Valor!.Resultados)
            {
                var op = pendientes.FirstOrDefault(p => p.OperationId == res.OperationId);
                if (op is null)
                {
                    continue;
                }

                if (res.Ok)
                {
                    _almacen.MarcarOperacionOk(op, DateTime.UtcNow);
                    okParaAck.Add(op.OperationId);
                }
                else
                {
                    _almacen.MarcarOperacionError(op, $"{res.Error}: {res.Message}");
                    OperacionConError?.Invoke(this, $"[{op.Tipo}] {res.Error}: {res.Message}");
                    OperacionRechazada?.Invoke(this, res);
                }
            }

            if (okParaAck.Count > 0)
            {
                await _api.ConfirmarAsync(sesion.Token, okParaAck, ct).ConfigureAwait(false);
            }
        }

        var estado = await _api.ObtenerEstadoAsync(sesion.Token, null, ct).ConfigureAwait(false);
        if (!estado.Ok)
        {
            return;
        }

        _almacen.ReemplazarCatalogo(estado.Valor!.Categorias, estado.Valor.Productos);
        NotificarEstado(online: true, ultimaSync: estado.Valor.Cursor);
    }

    private void NotificarEstado(bool? online = null, DateTime? ultimaSync = null)
    {
        var sesion = _sesiones.Actual;
        EstadoActualizado?.Invoke(this, new EstadoSync
        {
            Online = online ?? _api.Online,
            Pendientes = sesion is null ? 0 : _almacen.ContarPendientes(),
            Errores = sesion is null ? 0 : _almacen.ContarConErrores(),
            UltimaSincronizacion = ultimaSync ?? DateTime.UtcNow
        });
    }

    public ValueTask DisposeAsync() => new(DetenerAsync());
}
