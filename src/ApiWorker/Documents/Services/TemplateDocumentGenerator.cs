using ApiWorker.Documents.Entities;
using ApiWorker.Documents.Settings;
using ApiWorker.Documents.ValueObjects;
using ApiWorker.Authentication.Entities;
using ApiWorker.Storage;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using W = DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using DocumentEntityType = ApiWorker.Documents.Entities.DocumentType;

namespace ApiWorker.Documents.Services;

/// <summary>
/// Merges document data into DOCX templates by replacing placeholders.
/// </summary>
public sealed class TemplateDocumentGenerator
{
    private readonly IBlobStorageService _blobStorage;
    private readonly TemplateStorageSettings _settings;
    private readonly ILogger<TemplateDocumentGenerator> _logger;

    public TemplateDocumentGenerator(
        IBlobStorageService blobStorage,
        IOptions<TemplateStorageSettings> settings,
        ILogger<TemplateDocumentGenerator> logger)
    {
        _blobStorage = blobStorage;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Merges document data into a DOCX template by replacing placeholders.
    /// </summary>
    public async Task<MemoryStream> MergeTemplateAsync(
        string templateBlobPath,
        TransactionalDocument document,
        Business business,
        DocumentTheme theme,
        DocumentSignatureRender signature,
        CancellationToken ct = default)
    {
        try
        {
            // Download template from blob storage
            using var templateStream = await _blobStorage.DownloadAsync(
                templateBlobPath,
                _settings.TemplatesContainer,
                ct);

            // Create output stream
            var outputStream = new MemoryStream();
            templateStream.CopyTo(outputStream);
            outputStream.Position = 0;

            // Open and modify the document
            using (var wordDoc = WordprocessingDocument.Open(outputStream, true))
            {
                var mainPart = wordDoc.MainDocumentPart;
                if (mainPart == null)
                    throw new InvalidOperationException("Template document is invalid: missing main document part");

                var body = mainPart.Document?.Body;
                if (body == null)
                    throw new InvalidOperationException("Template document is invalid: missing body");

                // Replace all placeholders in the document
                ReplacePlaceholders(body, document, business, theme, signature, mainPart);
            }

            outputStream.Position = 0;
            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to merge template {TemplatePath}", templateBlobPath);
            throw new InvalidOperationException($"Failed to merge template: {ex.Message}", ex);
        }
    }

    private void ReplacePlaceholders(
        W.Body body,
        TransactionalDocument document,
        Business business,
        DocumentTheme theme,
        DocumentSignatureRender signature,
        MainDocumentPart mainPart)
    {
        // Get all text in the document
        var allTextElements = body.Descendants<W.Text>().ToList();

        foreach (var textElement in allTextElements)
        {
            var originalText = textElement.Text;
            if (string.IsNullOrWhiteSpace(originalText))
                continue;

            var replacedText = ReplacePlaceholderText(originalText, document, business, theme, signature);
            
            if (replacedText != originalText)
            {
                textElement.Text = replacedText;
            }
        }

        // Handle special placeholders that need table replacement
        ReplaceLineItemsPlaceholder(body, document, theme, mainPart);
        ReplaceSignaturePlaceholder(body, signature, theme, mainPart);
    }

    private string ReplacePlaceholderText(
        string text,
        TransactionalDocument document,
        Business business,
        DocumentTheme theme,
        DocumentSignatureRender signature)
    {
        var result = text;

        // Business placeholders
        result = result.Replace("{BusinessName}", business.Name ?? "");
        result = result.Replace("{BusinessPhone}", business.Phone ?? "");
        result = result.Replace("{BusinessEmail}", business.Email ?? "");
        result = result.Replace("{BusinessAddress}", BuildBusinessAddress(business));

        // Document placeholders
        result = result.Replace("{DocumentType}", GetDocumentTypeName(document.Type));
        result = result.Replace("{DocumentNumber}", document.Number ?? "");
        result = result.Replace("{DocumentDate}", document.IssuedAt.ToString("dd/MM/yyyy"));
        result = result.Replace("{DueDate}", document.DueAt?.ToString("dd/MM/yyyy") ?? "");

        // Customer placeholders
        result = result.Replace("{CustomerName}", document.CustomerName ?? "");
        result = result.Replace("{CustomerPhone}", document.CustomerPhone ?? "");
        result = result.Replace("{CustomerEmail}", document.CustomerEmail ?? "");
        result = result.Replace("{CustomerAddress}", BuildCustomerAddress(document));

        // Totals placeholders
        result = result.Replace("{Subtotal}", $"{document.Currency} {document.Subtotal:N2}");
        result = result.Replace("{Tax}", $"{document.Currency} {document.Tax:N2}");
        result = result.Replace("{Total}", $"{document.Currency} {document.Total:N2}");
        result = result.Replace("{Currency}", document.Currency);

        // Other placeholders
        result = result.Replace("{Notes}", document.Notes ?? "");
        result = result.Replace("{Reference}", document.Reference ?? "");

        return result;
    }

    private void ReplaceLineItemsPlaceholder(W.Body body, TransactionalDocument document, DocumentTheme theme, MainDocumentPart mainPart)
    {
        // Find paragraphs containing {LineItems} placeholder
        var paragraphs = body.Descendants<W.Paragraph>().ToList();
        var paragraphsToReplace = new List<W.Paragraph>();
        
        foreach (var para in paragraphs)
        {
            var text = para.InnerText;
            if (text.Contains("{LineItems}"))
            {
                paragraphsToReplace.Add(para);
            }
        }

        // Replace each paragraph with table
        foreach (var para in paragraphsToReplace)
        {
            var table = CreateLineItemsTable(document, theme, mainPart);
            body.ReplaceChild(table, para);
        }
    }

    private W.Table CreateLineItemsTable(TransactionalDocument document, DocumentTheme theme, MainDocumentPart mainPart)
    {
        var table = new W.Table();

        // Table properties
        var tableProps = new W.TableProperties(
            new W.TableBorders(
                new W.TopBorder { Val = W.BorderValues.Single, Size = 4 },
                new W.BottomBorder { Val = W.BorderValues.Single, Size = 4 },
                new W.LeftBorder { Val = W.BorderValues.Single, Size = 4 },
                new W.RightBorder { Val = W.BorderValues.Single, Size = 4 },
                new W.InsideHorizontalBorder { Val = W.BorderValues.Single, Size = 4 },
                new W.InsideVerticalBorder { Val = W.BorderValues.Single, Size = 4 }
            )
        );
        table.AppendChild(tableProps);

        // Header row
        var headerRow = new W.TableRow();
        headerRow.Append(
            CreateTableCell("Item", true, theme.SecondaryColor, "#FFFFFF"),
            CreateTableCell("Qty", true, theme.SecondaryColor, "#FFFFFF"),
            CreateTableCell("Price", true, theme.SecondaryColor, "#FFFFFF"),
            CreateTableCell("Tax", true, theme.SecondaryColor, "#FFFFFF"),
            CreateTableCell("Total", true, theme.SecondaryColor, "#FFFFFF")
        );
        table.AppendChild(headerRow);

        // Data rows
        foreach (var line in document.Lines)
        {
            var row = new W.TableRow();
            row.Append(
                CreateTableCell(line.Name ?? ""),
                CreateTableCell(line.Quantity.ToString("N2")),
                CreateTableCell($"{document.Currency} {line.UnitPrice:N2}"),
                CreateTableCell($"{line.TaxRate * 100:N0}%"),
                CreateTableCell($"{document.Currency} {line.LineTotal:N2}")
            );
            table.AppendChild(row);
        }

        return table;
    }

    private W.TableCell CreateTableCell(string text, bool bold = false, string? backgroundColor = null, string? textColor = null)
    {
        var cell = new W.TableCell();
        var para = new W.Paragraph();
        var run = new W.Run();

        var runProps = new W.RunProperties();
        if (bold)
            runProps.AppendChild(new W.Bold());
        if (!string.IsNullOrWhiteSpace(textColor))
            runProps.AppendChild(new W.Color { Val = NormalizeColor(textColor) });
        if (runProps.ChildElements.Count > 0)
            run.AppendChild(runProps);

        run.AppendChild(new W.Text(text));
        para.AppendChild(run);
        cell.AppendChild(para);

        if (!string.IsNullOrWhiteSpace(backgroundColor))
        {
            var cellProps = new W.TableCellProperties();
            cellProps.AppendChild(new W.Shading
            {
                Fill = NormalizeColor(backgroundColor),
                Val = W.ShadingPatternValues.Clear
            });
            cell.AppendChild(cellProps);
        }

        return cell;
    }

    private void ReplaceSignaturePlaceholder(W.Body body, DocumentSignatureRender signature, DocumentTheme theme, MainDocumentPart mainPart)
    {
        var paragraphs = body.Descendants<W.Paragraph>().ToList();

        foreach (var para in paragraphs)
        {
            var text = para.InnerText;
            if (!text.Contains("{Signature}"))
                continue;

            // Replace placeholder text
            var runs = para.Descendants<W.Run>().ToList();
            foreach (var run in runs)
            {
                var runText = run.Descendants<W.Text>().FirstOrDefault();
                if (runText != null && runText.Text.Contains("{Signature}"))
                {
                    runText.Text = runText.Text.Replace("{Signature}", "");
                }
            }

            // Add signature image if available
            if (signature.ImageBytes != null && signature.ImageBytes.Length > 0)
            {
                try
                {
                    var imagePara = new W.Paragraph();
                    var imageRun = new W.Run();
                    var drawing = CreateImageDrawing(mainPart, signature.ImageBytes, 1828800L, 609600L);
                    imageRun.AppendChild(drawing);
                    imagePara.AppendChild(imageRun);
                    body.InsertAfter(imagePara, para);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to embed signature image in template");
                }
            }
        }
    }

    private W.Drawing CreateImageDrawing(MainDocumentPart mainPart, byte[] imageBytes, long width, long height)
    {
        using var imageStream = new MemoryStream(imageBytes);
        var imagePart = mainPart.AddImagePart(DocumentFormat.OpenXml.Packaging.ImagePartType.Png);
        imagePart.FeedData(imageStream);

        return new W.Drawing(
            new DW.Inline(
                new DW.Extent { Cx = width, Cy = height },
                new DW.DocProperties { Id = 1, Name = "Signature" },
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = 0, Name = "Signature" },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip { Embed = mainPart.GetIdOfPart(imagePart) },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0, Y = 0 },
                                    new A.Extents { Cx = width, Cy = height }),
                                new A.PresetGeometry { Preset = A.ShapeTypeValues.Rectangle })))
                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })));
    }

    private string BuildBusinessAddress(Business business)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(business.Town))
            parts.Add(business.Town);
        if (!string.IsNullOrWhiteSpace(business.County))
            parts.Add(business.County);
        return string.Join(", ", parts);
    }

    private string BuildCustomerAddress(TransactionalDocument document)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(document.BillingAddressLine1))
            parts.Add(document.BillingAddressLine1);
        if (!string.IsNullOrWhiteSpace(document.BillingAddressLine2))
            parts.Add(document.BillingAddressLine2);
        if (!string.IsNullOrWhiteSpace(document.BillingCity))
            parts.Add(document.BillingCity);
        if (!string.IsNullOrWhiteSpace(document.BillingCountry))
            parts.Add(document.BillingCountry);
        return string.Join(", ", parts);
    }

    private string GetDocumentTypeName(DocumentEntityType type)
    {
        return type switch
        {
            DocumentEntityType.Invoice => "INVOICE",
            DocumentEntityType.Receipt => "RECEIPT",
            DocumentEntityType.Quotation => "QUOTATION",
            _ => "DOCUMENT"
        };
    }

    private string NormalizeColor(string? color, string fallback = "FFFFFF")
    {
        if (string.IsNullOrWhiteSpace(color))
            return fallback;

        var hex = color.Trim();
        if (hex.StartsWith("#"))
            hex = hex[1..];

        return hex.Length == 6 ? hex.ToUpperInvariant() : fallback;
    }
}

