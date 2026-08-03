using System.Drawing;
using System.Drawing.Printing;

namespace Kiosk.Pos.Services;

public interface IPosPrinter
{
    void ImprimirTicket(string contenido);
}

public sealed class ImpresoraArchivo : IPosPrinter
{
    public void ImprimirTicket(string contenido)
    {
        var directorio = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Kiosk.Pos", "tickets");
        Directory.CreateDirectory(directorio);
        var archivo = Path.Combine(directorio, $"ticket_{DateTime.Now:yyyyMMdd_HHmmss_fff}.txt");
        File.WriteAllText(archivo, contenido);

        try
        {
            var lineas = contenido.Replace("\r\n", "\n").Split('\n');
            using var documento = new PrintDocument();
            documento.PrintPage += (_, e) =>
            {
                using var fuente = new Font("Consolas", 9);
                float y = e.MarginBounds.Top;
                foreach (var linea in lineas)
                {
                    e.Graphics!.DrawString(linea, fuente, Brushes.Black, e.MarginBounds.Left, y);
                    y += fuente.GetHeight(e.Graphics);
                    if (y > e.MarginBounds.Bottom)
                    {
                        e.HasMorePages = true;
                        return;
                    }
                }

                e.HasMorePages = false;
            };

            documento.Print();
        }
        catch (Exception ex)
        {
            // La impresión nunca debe interrumpir la venta: el ticket queda en archivo.
            System.Diagnostics.Debug.WriteLine($"No se pudo imprimir el ticket: {ex.Message}");
        }
    }
}
