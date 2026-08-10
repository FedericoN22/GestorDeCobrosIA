using System.Globalization;
using System.Text;

namespace Kiosk.Application.CasosUso.Whatsapp;

public static class RespuestasBot
{
    public const string NoAutorizado =
        "Tu número no está autorizado para operar por WhatsApp. Contactá al administrador.";

    public const string MultiComando =
        "Recibí más de una instrucción en un solo mensaje. Enviá un solo comando por mensaje, por favor.";

    public const string OperacionCancelada =
        "Listo, la operación quedó cancelada.";

    public const string Error =
        "Ups, algo salió mal al procesar tu mensaje. Intentalo de nuevo en unos minutos.";

    public const string LimiteExcedido =
        "Estás enviando demasiados mensajes por minuto. Esperá un momento y volvé a intentarlo.";

    public const string TimeoutExpirado =
        "El tiempo para confirmar la operación anterior expiró, así que quedó cancelada. Enviá de nuevo tu pedido si todavía lo querés hacer.";

    public static string Ayuda(string botNombre) =>
        $"Hola 👋 Soy {botNombre}. Puedo ayudarte con:\n" +
        "• \"¿Cuánto stock hay de [producto]?\"\n" +
        "• \"¿Cuánto sale [producto] [presentación]?\"\n" +
        "• \"Agregar [producto] [presentación], cantidad N, precio N\"\n" +
        "• \"Cambiar precio de [producto] [presentación] a N\"\n" +
        "• \"Crear producto [nombre], presentación [X], precio N\"\n" +
        "• \"Eliminar [producto] [presentación]\"";

    public static string NoInterpretado(string botNombre) =>
        $"No pude entender tu mensaje. Escribí \"ayuda\" para ver qué puedo hacer, {botNombre}.";

    public static string ConfianzaBaja(string botNombre) =>
        $"No entendí bien tu pedido y prefiero no adivinar. Reescibilo con más claridad o escribí \"ayuda\" para ver ejemplos, {botNombre}.";

    public static string FaltanCampos(IReadOnlyList<string> faltantes, IReadOnlyList<string> ambiguos)
    {
        var sb = new StringBuilder("Me falta información para procesar tu pedido:");
        if (faltantes.Count > 0)
        {
            sb.Append('\n').Append("• Falta: ").Append(string.Join(", ", faltantes));
        }

        if (ambiguos.Count > 0)
        {
            sb.Append('\n').Append("• Ambiguo: ").Append(string.Join(", ", ambiguos));
        }

        sb.Append("\nMandame de nuevo la instrucción completa con esos datos.");
        return sb.ToString();
    }

    public static string NoEncontrado(string motivo) =>
        $"{motivo} Verificá el nombre y volvé a intentarlo, o escribí \"ayuda\".";

    public static string ElegiPresentacion(string producto, IReadOnlyList<string> presentaciones)
    {
        var sb = new StringBuilder($"El producto \"{producto}\" tiene varias presentaciones. ¿Cuál te referís?");
        foreach (var pres in presentaciones)
        {
            sb.Append('\n').Append("• ").Append(pres);
        }

        sb.Append("\nReescribí tu pedido indicando la presentación.");
        return sb.ToString();
    }

    public static string ConfirmarModificarPrecio(string producto, string presentacion, int precioActual, int precioNuevo) =>
        $"¿Confirmás que querés cambiar el precio de {producto} {presentacion} de {Pesos(precioActual)} a {Pesos(precioNuevo)}?\n" +
        "Respondé SI, CONFIRMO, OK o DALE para confirmar, o NO/CANCELAR para cancelar.";

    public static string ConfirmarEliminar(string producto, string presentacion, int stockActual) =>
        $"¿Confirmás que querés eliminar {producto} {presentacion}?\n" +
        $"Stock actual: {stockActual}. Queda: 0.\n" +
        "Respondé SI, CONFIRMO, OK o DALE para confirmar, o NO/CANCELAR para cancelar.";

    public static string StockConsultado(string producto, string presentacion, int stock, int? stockMinimo)
    {
        var baseMsg = $"Stock de {producto} {presentacion}: {stock} unidad(es).";
        if (stockMinimo.HasValue && stock <= stockMinimo.Value)
        {
            baseMsg += " ⚠️ Está por debajo del mínimo.";
        }

        return baseMsg;
    }

    public static string PrecioConsultado(string producto, string presentacion, int precioVenta) =>
        $"Precio de {producto} {presentacion}: {Pesos(precioVenta)}.";

    public static string ProductosListados(string detalle, bool hayMas) =>
        $"Productos:\n{detalle}" + (hayMas ? "\n… y más. Pedime un producto puntual para ver el detalle." : string.Empty);

    public static string StockAgregado(string producto, string presentacion, int cantidad, int stockActual) =>
        $"Listo, se agregaron {cantidad} unidades de {producto} {presentacion}. Stock actual: {stockActual}.";

    public static string ProductoCreado(string producto, string? presentacion, int? precioVenta)
    {
        if (string.IsNullOrWhiteSpace(presentacion))
        {
            return $"Listo, se creó el producto \"{producto}\". Ahora escribime \"agregar {producto} [presentación], cantidad N\" para cargarle presentación y stock.";
        }

        return $"Listo, se creó \"{producto}\" con la presentación {presentacion} a {Pesos(precioVenta ?? 0)}.";
    }

    public static string PrecioModificado(string producto, string presentacion, int precioNuevo) =>
        $"Precio actualizado: {producto} {presentacion} ahora sale {Pesos(precioNuevo)}.";

    public static string ProductoEliminado(string producto, string presentacion) =>
        $"Listo, se eliminó {producto} {presentacion}.";

    public static string ErrorConMotivo(string motivo) =>
        $"No se pudo completar la operación: {motivo}";

    public static string CanceladoPorError(string motivo) =>
        $"No se pudo ejecutar la operación confirmada ({motivo}). Quedó cancelada.";

    public static string Pesos(int centavos)
    {
        var signo = centavos < 0 ? "-" : string.Empty;
        var abs = Math.Abs(centavos);
        var pesos = abs / 100;
        var resto = abs % 100;
        var esAr = CultureInfo.GetCultureInfo("es-AR");
        return $"{signo}${pesos.ToString("N0", esAr)},{resto:00}";
    }
}
