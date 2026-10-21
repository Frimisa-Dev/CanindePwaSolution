using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Linq;

namespace FalcaoWebServer.Services.Utils
{
    public class PdfReportBuilder
    {
        private string _logoPath = string.Empty;
        private string _titulo = string.Empty;
        private string _subTitulo = string.Empty;
        private string _rodape = string.Empty;
        private string _autor = "Falcao Sistemas";
        private readonly List<(string Texto, bool Linha)> _conteudos = new();
        private IEnumerable<object>? _tabela;

        public static PdfReportBuilder CriarDocumentoPaisagem() => new PdfReportBuilder();

        public void AdicionarLogo(string logoFile) => _logoPath = logoFile;
        public void AdicionarTitulo(string titulo, string autor)
        {
            _titulo = titulo;
            _autor = autor;
        }

        public void AdicionarSubTitulo(string sub) => _subTitulo = sub;
        public void AdicionarTexto(string texto) => _conteudos.Add((texto, false));
        public void AdicionarLinha() => _conteudos.Add((string.Empty, true));
        public void AdicionarTabela(IEnumerable<object> data) => _tabela = data;
        public void AdicionarRodape(string assinatura) => _rodape = assinatura;

        public void Finalizar(string fullPath)
        {
            var doc = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                    // ---- Cabeçalho ----
                    page.Header().Row(row =>
                    {
                        if (File.Exists(_logoPath))
                            row.ConstantItem(120).Image(_logoPath).FitHeight();

                        row.RelativeItem().AlignCenter().Column(col =>
                        {
                            col.Item().Text(_titulo).Bold().FontSize(16);
                            if (!string.IsNullOrWhiteSpace(_subTitulo))
                                col.Item().Text(_subTitulo).FontSize(10);
                        });
                    });

                    // ---- Conteúdo ----
                    page.Content().Column(col =>
                    {
                        foreach (var (texto, linha) in _conteudos)
                        {
                            if (linha)
                                col.Item().LineHorizontal(0.5f);
                            else
                                col.Item().Text(texto).FontSize(10);
                        }

                        if (_tabela != null)
                        {
                            col.Spacing(10);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3); // Produto
                                    columns.RelativeColumn(2); // Cor
                                    columns.RelativeColumn(2); // Tipo
                                    columns.RelativeColumn(1); // Quantidade
                                    columns.RelativeColumn(2); // Preço
                                    columns.RelativeColumn(2); // Total
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Produto").Bold();
                                    header.Cell().Text("Cor").Bold();
                                    header.Cell().Text("Tipo").Bold();
                                    header.Cell().Text("Qtd").Bold();
                                    header.Cell().Text("Preço").Bold();
                                    header.Cell().Text("Total").Bold();
                                });

                                foreach (var item in _tabela)
                                {
                                    var props = item.GetType().GetProperties();
                                    foreach (var p in props)
                                        table.Cell().Text(p.GetValue(item)?.ToString() ?? string.Empty);
                                }
                            });
                        }
                    });

                    // ---- Rodapé ----
                    page.Footer().AlignCenter().Text(_rodape).FontSize(9).Italic();
                });
            });

            var pdfBytes = doc.GeneratePdf();
            File.WriteAllBytes(fullPath, pdfBytes);
        }
    }
}


