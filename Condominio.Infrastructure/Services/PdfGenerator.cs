using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuestPDF.Drawing;
using Condominio.Domain.Entities; // <--- Agrega esta línea

namespace Condominio.Infrastructure.Services
{
    public class PdfGenerator
    {
        public byte[] GenerarAviso(FacturaMes? factura, int totalCasas)
        {
            // Declaración de licencia
            QuestPDF.Settings.License = LicenseType.Community;

            // AQUÍ ESTÁ LA SOLUCIÓN: Permitir que use la fuente Arial de tu Windows
            QuestPDF.Settings.UseSystemFonts = true;

            // Colores extraídos de tu imagen
            string verdeOscuro = "#1A4D2E";
            string verdeClaro = "#8B9D77";
            string grisClaroFondo = "#F8F9FA";
            string amarilloAviso = "#FEF3C7";


            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

                    // 1. ENCABEZADO
                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("ADMINISTRACIÓN DE CONDOMINIO").FontColor(verdeClaro).FontSize(10).Bold();
                                col.Item().Text("URBANIZACION LA ENSENADA").FontSize(22).Bold().FontColor(verdeOscuro);
                                col.Item().Text("Asociación Civil Asoensenada San Joaquin, Edo. Carabobo").FontSize(9).FontColor(Colors.Grey.Darken2);
                                col.Item().Text("R.I.F.: J-29747927-9").FontSize(9).Bold();
                            });

                            row.ConstantItem(130).Border(1).BorderColor(verdeOscuro).Padding(10).AlignCenter().AlignMiddle().Column(col =>
                            {
                                col.Item().Text("AVISO DE COBRO").FontSize(13).Bold().FontColor(verdeOscuro);
                                col.Item().LineHorizontal(1).LineColor(verdeOscuro);
                            });
                        });
                        header.Item().PaddingTop(10).LineHorizontal(2).LineColor(verdeOscuro);
                    });

                    // 2. CONTENIDO (LA TABLA)
                    page.Content().PaddingVertical(15).Column(content =>
                    {
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4); // Descripción
                                columns.RelativeColumn(2); // Monto Total
                                columns.RelativeColumn(2); // Bs.
                                columns.RelativeColumn(2); // USD
                            });

                            // CABECERA DE LA TABLA
                            table.Header(header =>
                            {
                                header.Cell().RowSpan(2).Background(verdeOscuro).Padding(5).AlignMiddle().Text("Descripción").FontColor(Colors.White).Bold();
                                header.Cell().RowSpan(2).Background(verdeOscuro).Padding(5).AlignMiddle().AlignCenter().Text("Monto Total\n$USD").FontColor(Colors.White).Bold();
                                header.Cell().ColumnSpan(2).Background(verdeOscuro).Padding(2).AlignCenter().Text("Monto por Co-Propietario").FontColor(Colors.White).Bold();

                                header.Cell().Background(verdeOscuro).Padding(2).AlignCenter().Text("Bs.").FontColor(Colors.White).Bold();
                                header.Cell().Background(verdeOscuro).Padding(2).AlignCenter().Text("$USD").FontColor(Colors.White).Bold();
                            });

                            // --- CUERPO DE LA TABLA ---


                            // 1. RECORREMOS LAS CATEGORÍAS (Títulos)
                            var hijos = factura.FacturaMesHijos.ToList();
                            decimal totalAPagarPorCasa = 0;
                            decimal sumaTotalGastos = 0;    // Para la columna 2 (Monto Total original)

                            for (int i = 0; i < hijos.Count; i++)
                            {
                                var gasto = hijos[i];
                                var titulo = (gasto.CategoriaGasto != null) ? gasto.CategoriaGasto.Nombre : gasto.Motivo;


                                decimal montoDividido = totalCasas > 0 ? ((decimal)gasto.Monto / (decimal)totalCasas) : 0m;
                                sumaTotalGastos += (decimal)gasto.Monto;      // Suma original
                                totalAPagarPorCasa += montoDividido;

                                // 2. Lógica del efecto cebra: Si es par, blanco; si es impar, gris clarito
                                // Puedes cambiar "#FFFFFF" (blanco) y "#F8F9FA" (gris muy claro) por los que prefieras.
                                string colorFondo = (i % 2 == 0) ? "#FFFFFF" : "#F4F6F8";



                                // 3. Aplicamos el color de fondo a las 4 columnas y mantenemos el borde inferior
                                table.Cell()
                                     .Background(colorFondo)
                                     .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                     .Padding(5)
                                     .Text(titulo);

                                table.Cell()
                                     .Background(colorFondo)
                                     .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                     .Padding(5).AlignCenter()
                                     .Text(gasto.Monto.ToString("N2"));

                                table.Cell()
                                     .Background(colorFondo)
                                     .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                     .Padding(5).AlignCenter()
                                     .Text("");

                                table.Cell()
                                     .Background(colorFondo)
                                     .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                     .Padding(5).AlignCenter()
                                     .Text(montoDividido > 0 ? montoDividido.ToString("N2") : ""); // USD

                            }
                            // Opcional: Una línea al final de todos los hijos
                            table.Cell().ColumnSpan(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

                            // Opcional: Una línea al final de todos los hijos
                            table.Cell().ColumnSpan(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                            // FILA FINAL VERDE
                            table.Cell().Background(verdeOscuro).Padding(5).Text("TOTAL GASTOS MAS FONDOS DE RESERVA").FontColor(Colors.White).Bold();
                            table.Cell().Background(verdeOscuro).Padding(5).AlignCenter().Text(sumaTotalGastos.ToString("N2")).FontColor(Colors.White).Bold();
                            table.Cell().Background(verdeOscuro).Padding(5).AlignCenter().Text("00.00").FontColor(Colors.White).Bold();
                            table.Cell().Background(verdeOscuro).Padding(5).AlignCenter().Text(totalAPagarPorCasa.ToString("N2")).FontColor(Colors.White).Bold();

                        });

                        // 3. PIE DE PÁGINA
                        content.Item().PaddingTop(15).Border(1).BorderColor(verdeOscuro).Padding(10).Column(footer =>
                        {
                            footer.Item().AlignCenter().Text("PUEDE REALIZAR SU PAGO POR TRANSFERENCIA A NOMBRE DE:").Bold().FontSize(10);
                            footer.Item().AlignCenter().Text("ASOCIACION CIVIL ASOENSENADA, RIF J-29747927-9, BANCO BANCAMIGA").FontSize(9);
                            footer.Item().AlignCenter().Text("CUENTA CTE 0172 0204 92 2045833346 / CUENTA CASH $ 0172 0204 98 2045855729").FontSize(9);
                            footer.Item().AlignCenter().Text("PAGO MOVIL 0412-792.71.03 J-29747927-9 (0172) BANCAMIGA").FontSize(9);

                            footer.Item().PaddingTop(10).AlignCenter().Text("IMPORTANTE:").Bold().FontColor("#C08B5C").FontSize(12);

                            footer.Item().PaddingTop(8).Background(amarilloAviso).Padding(4).AlignCenter().Text("condominioensenadaunidos@gmail.com").Bold().FontSize(9);
                            footer.Item().PaddingTop(4).Background(amarilloAviso).Padding(4).AlignCenter().Text("INDICAR EN EL CONCEPTO DE LA TRANSF NO CASA Y MES QUE CANCELA").Bold().FontSize(9);
                            footer.Item().PaddingTop(4).Background(amarilloAviso).Padding(4).AlignCenter().Text("EL RECIBO DEBE SER PAGADO A LA TASA DEL B.C.V. DEL DIA EN QUE SE REALICE EL PAGO").Bold().FontSize(9);

                            footer.Item().PaddingTop(8).AlignCenter().Text("SE CONSIDERA MOROSO CON DOS MESES SIN CANCELAR Y").Bold().FontSize(9);
                            footer.Item().AlignCenter().Text("SE APLICARA SANCION INHERENTE A LA MOROSIDAD").Bold().FontSize(9);
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Urbanización La Ensenada — Aviso de Cobro").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            // Retorna el archivo en un arreglo de bytes
            return document.GeneratePdf();
        }
    }
}
