using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ApiWorker.Documents.Entities;
using ApiWorker.Authentication.Entities;

namespace ApiWorker.Documents.Templates.Default;

public static class QuestPdfInvoiceGenerator
{
    public static byte[] GenerateInvoicePdf(Invoice invoice, Business business)
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
                        column.Item().Text("INVOICE").FontSize(18).Bold();
                        column.Item().Text($"#{invoice.Number}").FontSize(11);
                        column.Item().Text($"Date: {invoice.IssuedAt:dd/MM/yyyy}").FontSize(9);
                        if (invoice.DueAt.HasValue)
                            column.Item().Text($"Due: {invoice.DueAt.Value:dd/MM/yyyy}").FontSize(9);
                    });
                });
            }

            void ComposeContent(IContainer container)
            {
                container.PaddingVertical(20).Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text("Bill To:").Bold();
                    column.Item().Text(invoice.CustomerName ?? "");
                    if (!string.IsNullOrEmpty(invoice.CustomerPhone))
                        column.Item().Text(invoice.CustomerPhone);
                    if (!string.IsNullOrEmpty(invoice.CustomerEmail))
                        column.Item().Text(invoice.CustomerEmail);
                    if (!string.IsNullOrEmpty(invoice.BillingAddressLine1))
                        column.Item().Text(invoice.BillingAddressLine1);
                    if (!string.IsNullOrEmpty(invoice.BillingAddressLine2))
                        column.Item().Text(invoice.BillingAddressLine2);
                    if (!string.IsNullOrEmpty(invoice.BillingCity) || !string.IsNullOrEmpty(invoice.BillingCountry))
                        column.Item().Text($"{invoice.BillingCity}{(string.IsNullOrEmpty(invoice.BillingCity) || string.IsNullOrEmpty(invoice.BillingCountry) ? "" : ", ")}{invoice.BillingCountry}");

                    if (!string.IsNullOrEmpty(invoice.Reference))
                    {
                        column.Item().PaddingTop(10);
                        column.Item().Text($"Reference: {invoice.Reference}").FontSize(9).Italic();
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

                        foreach (var line in invoice.Lines)
                        {
                            table.Cell().Element(CellStyle).Text(line.Name);
                            table.Cell().Element(CellStyle).AlignRight().Text(line.Quantity.ToString("N2"));
                            table.Cell().Element(CellStyle).AlignRight().Text($"{invoice.Currency} {line.UnitPrice:N2}");
                            table.Cell().Element(CellStyle).AlignRight().Text($"{invoice.Currency} {line.LineTotal:N2}");

                            static IContainer CellStyle(IContainer container) =>
                                container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5);
                        }
                    });

                    column.Item().PaddingTop(10);

                    column.Item().AlignRight().Row(row =>
                    {
                        row.RelativeItem().Text("Subtotal:").Bold();
                        row.RelativeItem().AlignRight().Text($"{invoice.Currency} {invoice.Subtotal:N2}");
                    });

                    if (invoice.Tax > 0)
                    {
                        column.Item().AlignRight().Row(row =>
                        {
                            row.RelativeItem().Text("Tax:");
                            row.RelativeItem().AlignRight().Text($"{invoice.Currency} {invoice.Tax:N2}");
                        });
                    }

                    column.Item().AlignRight().Row(row =>
                    {
                        row.RelativeItem().Text("Total:").FontSize(14).Bold();
                        row.RelativeItem().AlignRight().Text($"{invoice.Currency} {invoice.Total:N2}").FontSize(14).Bold();
                    });

                    if (!string.IsNullOrEmpty(invoice.Notes))
                    {
                        column.Item().PaddingTop(20);
                        column.Item().Text("Notes:").Bold();
                        column.Item().Text(invoice.Notes).FontSize(9);
                    }
                });
            }
        }).GeneratePdf();
    }

    public static byte[] GenerateInvoicePreview(Invoice invoice, Business business)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Content().Element(c => ComposePreview(c, invoice, business));
            });
        }).GenerateImages().First();
    }

    private static void ComposePreview(IContainer container, Invoice invoice, Business business)
    {
        container.Column(column =>
        {
            column.Item().Text(business.Name).FontSize(20).Bold();
            column.Item().Text($"Invoice #{invoice.Number}").FontSize(16);
            column.Item().Text($"Total: {invoice.Currency} {invoice.Total:N2}").FontSize(14).Bold();
        });
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
