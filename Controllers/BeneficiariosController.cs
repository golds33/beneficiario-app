using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml; // Asegúrate de tener este using
using Pension65Api.Data;
using Pension65Api.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

// ✅ ELIMINAMOS EL USING DUPLICADO

namespace Pension65Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BeneficiariosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BeneficiariosController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ GET: api/Beneficiarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Beneficiario>>> GetBeneficiarios(
            [FromQuery] string? distrito,
            [FromQuery] string? estadoPago)
        {
            var query = _context.Beneficiarios.AsQueryable();

            if (!string.IsNullOrWhiteSpace(distrito))
                query = query.Where(b => b.Distrito == distrito);

            if (!string.IsNullOrWhiteSpace(estadoPago))
                query = query.Where(b => b.EstadoPago == estadoPago);

            return await query.ToListAsync();
        }

        // ✅ GET: api/Beneficiarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Beneficiario>> GetBeneficiario(int id)
        {
            var ben = await _context.Beneficiarios.FindAsync(id);
            if (ben == null) return NotFound();
            return ben;
        }

        // ✅ POST: api/Beneficiarios
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Beneficiario>> PostBeneficiario(Beneficiario beneficiario)
        {
            if (string.IsNullOrWhiteSpace(beneficiario.DNI) || string.IsNullOrWhiteSpace(beneficiario.Nombres))
                return BadRequest("DNI y Nombres son requeridos.");

            _context.Beneficiarios.Add(beneficiario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBeneficiario), new { id = beneficiario.Id }, beneficiario);
        }

        // ✅ PUT: api/Beneficiarios/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutBeneficiario(int id, Beneficiario beneficiario)
        {
            if (id != beneficiario.Id) return BadRequest();

            _context.Entry(beneficiario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Beneficiarios.Any(e => e.Id == id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // ✅ DELETE: api/Beneficiarios/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBeneficiario(int id)
        {
            var b = await _context.Beneficiarios.FindAsync(id);
            if (b == null) return NotFound();

            _context.Beneficiarios.Remove(b);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ✅ Reporte agrupado por estado de pago
        [HttpGet("resumen/por-estado")]
        public IActionResult GetResumenPorEstado()
        {
            var resumen = _context.Beneficiarios
                .GroupBy(b => b.EstadoPago)
                .Select(g => new
                {
                    EstadoPago = g.Key,
                    Cantidad = g.Count()
                })
                .ToList();

            return Ok(resumen);
        }

        // ✅ Exportar a Excel (EPPlus)
        [HttpGet("export/excel")]
        [AllowAnonymous]
        public IActionResult ExportExcel()
        {
            try
            {
                // ✅ REVERSIÓN 1: Volvemos al código "obsoleto" (pero que compila)
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Beneficiarios");

                // Cabeceras
                ws.Cells["A1"].Value = "DNI";
                ws.Cells["B1"].Value = "Nombres";
                ws.Cells["C1"].Value = "Edad";
                ws.Cells["D1"].Value = "Distrito";
                ws.Cells["E1"].Value = "EstadoPago";

                // Datos
                var list = _context.Beneficiarios.ToList();
                int row = 2;
                foreach (var b in list)
                {
                    ws.Cells[row, 1].Value = b.DNI;
                    ws.Cells[row, 2].Value = b.Nombres;
                    ws.Cells[row, 3].Value = b.Edad;
                    ws.Cells[row, 4].Value = b.Distrito;
                    ws.Cells[row, 5].Value = b.EstadoPago;
                    row++;
                }

                ws.Cells.AutoFitColumns();

                var stream = new MemoryStream(package.GetAsByteArray());
                return File(stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Beneficiarios.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al generar Excel: {ex.Message} \nStackTrace: {ex.StackTrace}");
            }
        }

           // ✅ Exportar a PDF (QuestPDF)
        [HttpGet("export/pdf")]
        [AllowAnonymous]
        public IActionResult ExportPdf()
        {
            try
            {
                // 💎 ¡ESTA ES LA LÍNEA QUE SOLUCIONA EL ERROR! 💎
                // Las versiones nuevas de QuestPDF requieren que se establezca la licencia,
                // incluso la gratuita "Community".
                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

                var beneficiarios = _context.Beneficiarios.ToList();

                using var stream = new MemoryStream();

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(40);
                        page.PageColor(Colors.White);

                        // 🔹 Encabezado
                        page.Header().Row(row =>
                        {
                            // Usamos el código "obsoleto" (amarillo) porque es el que compila
                            row.RelativeColumn().Stack(stack =>
                            {
                                stack.Item().Text("Programa Pensión 65")
                                    .FontSize(16)
                                    .Bold()
                                    .FontColor("#1976D2");

                                stack.Item().Text("Reporte de Beneficiarios")
                                    .FontSize(11)
                                    .FontColor("#333333");

                                stack.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(9)
                                    .FontColor("#555555");
                            });
                        });

                        // 🔹 Contenido principal (tabla)
                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                // Usamos el código "obsoleto" (amarillo)
                                columns.ConstantColumn(80);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(50);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            // Cabecera
                            table.Header(header =>
                            {
                                header.Cell().Element(CellHeader).Text("DNI");
                                header.Cell().Element(CellHeader).Text("Nombres");
                                header.Cell().Element(CellHeader).Text("Edad");
                                header.Cell().Element(CellHeader).Text("Distrito");
// ... (código sin cambios) ...
                                header.Cell().Element(CellHeader).Text("Estado Pago");

                                static IContainer CellHeader(IContainer container)
                                {
                                    return container
                                        .Background("#1976D2")
// ... (código sin cambios) ...
                                        .Padding(5)
                                        .DefaultTextStyle(x => x.FontSize(10).FontColor("#FFFFFF"))
                                        .AlignCenter();
                                }
                            });

                            // Filas
                            foreach (var b in beneficiarios)
                            {
                                table.Cell().Element(CellBody).Text(b.DNI);
// ... (código sin cambios) ...
                                table.Cell().Element(CellBody).Text(b.Nombres);
                                table.Cell().Element(CellBody).Text(b.Edad.ToString());
                                table.Cell().Element(CellBody).Text(b.Distrito);
                                table.Cell().Element(CellBody).Text(b.EstadoPago);
                            }

                            static IContainer CellBody(IContainer container)
                            {
                                return container
// ... (código sin cambios) ...
                                    .Padding(5)
                                    .BorderBottom(0.5f)
                                    .BorderColor("#DDDDDD")
                                    .DefaultTextStyle(x => x.FontSize(9));
                            }
                        });

                        // 🔹 Totales al final
                        page.Content().PaddingTop(10).AlignRight().Text(txt =>
                        {
// ... (código sin cambios) ...
                            var total = beneficiarios.Count;
                            txt.Span($"Total de beneficiarios: {total}")
                                .FontSize(10)
                                .Bold()
                                .FontColor("#333333");
                        });

                        // 🔹 Pie de página
                        page.Footer().AlignRight().Text(txt =>
                        {
// ... (código sin cambios) ...
                            txt.Span("Página ");
                            txt.CurrentPageNumber();
                            txt.Span(" de ");
                            txt.TotalPages();
                        });
                    });
                }).GeneratePdf(stream);

                stream.Position = 0;
                return File(stream.ToArray(), "application/pdf", "Beneficiarios.pdf");
            }
            catch (Exception ex)
            {
                // ESTO CAPTURARÁ EL ERROR Y LO ENVIARÁ A ANGULAR
                return StatusCode(500, $"Error interno al generar PDF: {ex.Message} \nStackTrace: {ex.StackTrace}");
            }
        }
    }
}

