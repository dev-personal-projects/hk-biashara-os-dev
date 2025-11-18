using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ApiWorker.Documents.Entities;
using ApiWorker.Authentication.Entities;

namespace ApiWorker.Documents.Templates.Default;

/// <summary>
/// Generates PDF files for transactional documents using QuestPDF.
/// </summary>
public static class QuestPdfDocumentGenerator
{
    public static byte[] GenerateDocumentPdf(TransactionalDocument document, Business business)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });

            void ComposeHeader(IContainer container)
            {
                container.Row(row =>
                {
                    if (!string.IsNullOrEmpty(business.LogoUrl))
                    {
                        var logoBytes = DownloadImage(business.LogoUrl);
                        if (logoBytes.Length > 0)
                        {
                            row.ConstantItem(60).Image(logoBytes).FitArea();
                            row.ConstantItem(10);
                        }
                    }

                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text(business.Name).FontSize(18).Bold();
                        if (!string.IsNullOrEmpty(business.Phone))
                            column.Item().Text(business.Phone).FontSize(9);
                        if (!string.IsNullOrEmpty(business.Email))
                            column.Item().Text(business.Email).FontSize(9);
                    });

                    row.RelativeItem().AlignRight().Column(column =>
                    {
                        var documentTitle = GetDocumentTitle(document.Type);
                        column.Item().Text(documentTitle).FontSize(18).Bold();
                        column.Item().Text($"#{document.Number}").FontSize(11);
                        column.Item().Text($"Date: {document.IssuedAt:dd/MM/yyyy}").FontSize(9);
                        if (document.DueAt.HasValue)
                            column.Item().Text($"Due: {document.DueAt.Value:dd/MM/yyyy}").FontSize(9);
                    });
                });
            }

            void ComposeContent(IContainer container)
            {
                container.PaddingVertical(20).Column(column =>
                {
                    column.Spacing(10);

                    var customerLabel = document.Type == DocumentType.Receipt ? "Paid By:" : "Bill To:";
                    column.Item().Text(customerLabel).Bold();
                    column.Item().Text(document.CustomerName ?? "");
                    if (!string.IsNullOrEmpty(document.CustomerPhone))
                        column.Item().Text(document.CustomerPhone);
                    if (!string.IsNullOrEmpty(document.CustomerEmail))
                        column.Item().Text(document.CustomerEmail);
                    if (!string.IsNullOrEmpty(document.BillingAddressLine1))
                        column.Item().Text(document.BillingAddressLine1);
                    if (!string.IsNullOrEmpty(document.BillingAddressLine2))
                        column.Item().Text(document.BillingAddressLine2);
                    if (!string.IsNullOrEmpty(document.BillingCity) || !string.IsNullOrEmpty(document.BillingCountry))
                        column.Item().Text($"{document.BillingCity}{(string.IsNullOrEmpty(document.BillingCity) || string.IsNullOrEmpty(document.BillingCountry) ? "" : ", ")}{document.BillingCountry}");

                    if (!string.IsNullOrEmpty(document.Reference))
                    {
                        column.Item().PaddingTop(10);
                        column.Item().Text($"Reference: {document.Reference}").FontSize(9).Italic();
                    }

                    column.Item().PaddingTop(20);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Item").Bold();
                            header.Cell().Element(CellStyle).AlignRight().Text("Qty").Bold();
                            header.Cell().Element(CellStyle).AlignRight().Text("Price").Bold();
                            header.Cell().Element(CellStyle).AlignRight().Text("Total").Bold();

                            static IContainer CellStyle(IContainer container) =>
                                container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                        });

                        foreach (var line in document.Lines)
                        {
                            table.Cell().Element(CellStyle).Text(line.Name);
                            table.Cell().Element(CellStyle).AlignRight().Text(line.Quantity.ToString("N2"));
                            table.Cell().Element(CellStyle).AlignRight().Text($"{document.Currency} {line.UnitPrice:N2}");
                            table.Cell().Element(CellStyle).AlignRight().Text($"{document.Currency} {line.LineTotal:N2}");

                            static IContainer CellStyle(IContainer container) =>
                                container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5);
                        }
                    });

                    column.Item().PaddingTop(10);

                    column.Item().AlignRight().Row(row =>
                    {
                        row.RelativeItem().Text("Subtotal:").Bold();
                        row.RelativeItem().AlignRight().Text($"{document.Currency} {document.Subtotal:N2}");
                    });

                    if (document.Tax > 0)
                    {
                        column.Item().AlignRight().Row(row =>
                        {
                            row.RelativeItem().Text("Tax:");
                            row.RelativeItem().AlignRight().Text($"{document.Currency} {document.Tax:N2}");
                        });
                    }

                    column.Item().AlignRight().Row(row =>
                    {
                        row.RelativeItem().Text("Total:").FontSize(14).Bold();
                        row.RelativeItem().AlignRight().Text($"{document.Currency} {document.Total:N2}").FontSize(14).Bold();
                    });

                    if (!string.IsNullOrEmpty(document.Notes))
                    {
                        column.Item().PaddingTop(20);
                        column.Item().Text("Notes:").Bold();
                        column.Item().Text(document.Notes).FontSize(9);
                    }
                });
            }
        }).GeneratePdf();
    }

    public static byte[] GenerateDocumentPreview(TransactionalDocument document, Business business)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Content().Element(c => ComposePreview(c, document, business));
            });
        }).GenerateImages().First();
    }

    private static void ComposePreview(IContainer container, TransactionalDocument document, Business business)
    {
        var documentTitle = GetDocumentTitle(document.Type);
        container.Column(column =>
        {
            column.Item().Text(business.Name).FontSize(20).Bold();
            column.Item().Text($"{documentTitle} #{document.Number}").FontSize(16);
            column.Item().Text($"Total: {document.Currency} {document.Total:N2}").FontSize(14).Bold();
        });
    }

    private static string GetDocumentTitle(DocumentType type)
    {
        return type switch
        {
            DocumentType.Invoice => "INVOICE",
            DocumentType.Receipt => "RECEIPT",
            DocumentType.Quotation => "QUOTATION",
            _ => "DOCUMENT"
        };
    }

    private static byte[] DownloadImage(string url)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            return httpClient.GetByteArrayAsync(url).Result;
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }
}
