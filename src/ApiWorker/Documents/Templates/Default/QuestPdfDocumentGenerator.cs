using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using ApiWorker.Documents.Entities;
using ApiWorker.Authentication.Entities;
using ApiWorker.Documents.ValueObjects;

namespace ApiWorker.Documents.Templates.Default;

/// <summary>
/// Generates PDF files for transactional documents using QuestPDF.
/// </summary>
public static class QuestPdfDocumentGenerator
{
    public static byte[] GenerateDocumentPdf(TransactionalDocument document, Business business, DocumentTheme theme, DocumentSignatureRender signature)
    {
        try
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.EnableDebugging = false; // Set to true for detailed debugging if needed
            return CreateDocumentDefinition(document, business, theme, signature).GeneratePdf();
        }
        catch (QuestPDF.Drawing.Exceptions.DocumentLayoutException ex)
        {
            throw new InvalidOperationException(
                $"PDF layout error for document {document.Number}. " +
                $"This may be caused by content that doesn't fit on the page. " +
                $"Line items: {document.Lines?.Count ?? 0}, " +
                $"Signature present: {signature?.ImageBytes != null && signature.ImageBytes.Length > 0}. " +
                $"Original error: {ex.Message}", ex);
        }
    }

    public static byte[] GenerateDocumentPreview(TransactionalDocument document, Business business, DocumentTheme theme, DocumentSignatureRender signature)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        return CreateDocumentDefinition(document, business, theme, signature).GenerateImages().First();
    }

    private static IDocument CreateDocumentDefinition(TransactionalDocument document, Business business, DocumentTheme theme, DocumentSignatureRender signature)
    {
        var primary = ToColor(theme.PrimaryColor, Color.FromHex("#111827"));
        var secondary = ToColor(theme.SecondaryColor, Color.FromHex("#1F2937"));
        var accent = ToColor(theme.AccentColor, Color.FromHex("#F97316"));
        var fontFamily = string.IsNullOrWhiteSpace(theme.FontFamily) ? "Poppins" : theme.FontFamily;

        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(fontFamily).FontColor(secondary));

                page.Header().Element(c => ComposeHeader(c, document, business, primary, secondary, accent));
                page.Content().Element(c => ComposeContent(c, document, business, primary, secondary, accent, signature));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ").FontColor(secondary);
                    x.CurrentPageNumber();
                });
            });
        });
    }

    private static void ComposeHeader(IContainer container, TransactionalDocument document, Business business, Color primary, Color secondary, Color accent)
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
                column.Item().Text(business.Name).FontSize(18).Bold().FontColor(primary);
                if (!string.IsNullOrEmpty(business.Phone))
                    column.Item().Text(business.Phone).FontSize(9).FontColor(secondary);
                if (!string.IsNullOrEmpty(business.Email))
                    column.Item().Text(business.Email).FontSize(9).FontColor(secondary);
            });

            row.RelativeItem().AlignRight().Column(column =>
            {
                var documentTitle = GetDocumentTitle(document.Type);
                column.Item().Text(documentTitle).FontSize(18).Bold().FontColor(accent);
                column.Item().Text($"#{document.Number}").FontSize(11).FontColor(primary);
                column.Item().Text($"Date: {document.IssuedAt:dd/MM/yyyy}").FontSize(9).FontColor(secondary);
                if (document.DueAt.HasValue)
                    column.Item().Text($"Due: {document.DueAt.Value:dd/MM/yyyy}").FontSize(9).FontColor(secondary);
            });
        });
    }

    private static void ComposeContent(IContainer container, TransactionalDocument document, Business business, Color primary, Color secondary, Color accent, DocumentSignatureRender signature)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(10);

            var customerLabel = document.Type == DocumentType.Receipt ? "Paid By:" : "Bill To:";
            column.Item().Text(customerLabel).Bold().FontColor(primary);
            column.Item().Text(document.CustomerName ?? string.Empty).FontColor(secondary);
            if (!string.IsNullOrEmpty(document.CustomerPhone))
                column.Item().Text(document.CustomerPhone).FontColor(secondary);
            if (!string.IsNullOrEmpty(document.CustomerEmail))
                column.Item().Text(document.CustomerEmail).FontColor(secondary);
            if (!string.IsNullOrEmpty(document.BillingAddressLine1))
                column.Item().Text(document.BillingAddressLine1).FontColor(secondary);
            if (!string.IsNullOrEmpty(document.BillingAddressLine2))
                column.Item().Text(document.BillingAddressLine2).FontColor(secondary);
            if (!string.IsNullOrEmpty(document.BillingCity) || !string.IsNullOrEmpty(document.BillingCountry))
                column.Item().Text($"{document.BillingCity}{(string.IsNullOrEmpty(document.BillingCity) || string.IsNullOrEmpty(document.BillingCountry) ? "" : ", ")}{document.BillingCountry}").FontColor(secondary);

            if (!string.IsNullOrEmpty(document.Reference))
            {
                column.Item().PaddingTop(10);
                column.Item().Text($"Reference: {document.Reference}").FontSize(9).Italic().FontColor(primary);
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
                    header.Cell().Element(container => HeaderCell(container, secondary, accent)).Text("Item").FontColor(Colors.White).Bold();
                    header.Cell().Element(container => HeaderCell(container, secondary, accent)).AlignRight().Text("Qty").FontColor(Colors.White).Bold();
                    header.Cell().Element(container => HeaderCell(container, secondary, accent)).AlignRight().Text("Price").FontColor(Colors.White).Bold();
                    header.Cell().Element(container => HeaderCell(container, secondary, accent)).AlignRight().Text("Total").FontColor(Colors.White).Bold();
                });

                foreach (var line in document.Lines)
                {
                    table.Cell().Element(container => BodyCell(container)).Text(line.Name);
                    table.Cell().Element(container => BodyCell(container)).AlignRight().Text(line.Quantity.ToString("N2"));
                    table.Cell().Element(container => BodyCell(container)).AlignRight().Text($"{document.Currency} {line.UnitPrice:N2}");
                    table.Cell().Element(container => BodyCell(container)).AlignRight().Text($"{document.Currency} {line.LineTotal:N2}");
                }
            });

            column.Item().PaddingTop(10);

            column.Item().AlignRight().Row(row =>
            {
                row.RelativeItem().Text("Subtotal:").Bold().FontColor(primary);
                row.RelativeItem().AlignRight().Text($"{document.Currency} {document.Subtotal:N2}").FontColor(secondary);
            });

            if (document.Tax > 0)
            {
                column.Item().AlignRight().Row(row =>
                {
                    row.RelativeItem().Text("Tax:").FontColor(primary);
                    row.RelativeItem().AlignRight().Text($"{document.Currency} {document.Tax:N2}").FontColor(secondary);
                });
            }

            column.Item().AlignRight().Row(row =>
            {
                row.RelativeItem().Text("Total:").FontSize(14).Bold().FontColor(accent);
                row.RelativeItem().AlignRight().Text($"{document.Currency} {document.Total:N2}").FontSize(14).Bold().FontColor(accent);
            });

            if (!string.IsNullOrEmpty(document.Notes))
            {
                column.Item().PaddingTop(20);
                column.Item().Text("Notes:").Bold().FontColor(primary);
                column.Item().Text(document.Notes).FontSize(9).FontColor(secondary);
            }

            // Signature section - only show if signature exists or document is signed
            if (signature != null && (signature.ImageBytes != null || !string.IsNullOrWhiteSpace(signature.SignedBy)))
            {
                column.Item().PaddingTop(20).Column(sig =>
                {
                    sig.Spacing(5);
                    sig.Item().Text("Authorized Signature").Bold().FontColor(primary);
                    
                    if (signature.ImageBytes != null && signature.ImageBytes.Length > 0)
                    {
                        // Constrain signature image to prevent layout issues
                        // Use a fixed width and height to avoid conflicting constraints
                        sig.Item()
                            .Width(120) // Fixed width to prevent overflow
                            .Height(50) // Fixed height - reduced to prevent layout issues
                            .Image(signature.ImageBytes)
                            .FitArea(); // Fit within the constrained area
                    }
                    else
                    {
                        sig.Item().Height(20).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                    }

                    if (!string.IsNullOrWhiteSpace(signature.SignedBy) || signature.SignedAt.HasValue)
                    {
                        var signedLine = $"Signed by {signature.SignedBy ?? "N/A"}";
                        if (signature.SignedAt.HasValue)
                            signedLine += $" on {signature.SignedAt:dd MMM yyyy HH:mm}";
                        sig.Item().Text(signedLine).FontSize(9).FontColor(secondary);
                    }

                    if (!string.IsNullOrWhiteSpace(signature.Notes))
                        sig.Item().Text(signature.Notes).FontSize(9).FontColor(secondary);
                });
            }
        });

        static IContainer HeaderCell(IContainer container, Color secondary, Color accent) =>
            container.Background(accent).BorderBottom(1).BorderColor(accent).PaddingVertical(5).PaddingHorizontal(2);

        static IContainer BodyCell(IContainer container) =>
            container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5);
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

    private static Color ToColor(string? value, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        try
        {
            return Color.FromHex(value);
        }
        catch
        {
            return fallback;
        }
    }
}
