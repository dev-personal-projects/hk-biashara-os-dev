using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using ApiWorker.Documents.Entities;
using ApiWorker.Authentication.Entities;
using ApiWorker.Documents.ValueObjects;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using W = DocumentFormat.OpenXml.Wordprocessing;
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;

namespace ApiWorker.Documents.Services;

/// <summary>
/// Generates DOCX files for transactional documents (Invoice, Receipt, Quotation).
/// Creates documents from scratch using OpenXML.
/// </summary>
public sealed class OpenXmlDocumentGenerator
{
    /// <summary>
    /// Creates a DOCX document for a transactional document and returns the file stream.
    /// </summary>
    public static MemoryStream GenerateDocument(TransactionalDocument document, Business business, DocumentTheme theme, DocumentSignatureRender signature)
    {
        var stream = new MemoryStream();

        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new WordDocument(new W.Body());
            var body = mainPart.Document.Body!;

            // Get document title based on type
            var documentTitle = GetDocumentTitle(document.Type);

            // Header: Business info and document details
            AddBusinessHeader(body, business, document, documentTitle, mainPart, theme);
            AddSpacer(body);

            // Customer info
            AddCustomerSection(body, document, theme);
            AddSpacer(body);

            // Line items table
            AddLineItemsTable(body, document, theme);
            AddSpacer(body);

            // Totals
            AddTotalsSection(body, document, theme);
            AddSpacer(body);

            // Footer: Notes and reference
            AddFooter(body, document, theme);

            // Signature block
            AddSignatureSection(body, signature, theme, mainPart);
        }

        stream.Position = 0;
        return stream;
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

    private static void AddBusinessHeader(W.Body body, Business business, TransactionalDocument document, string documentTitle, MainDocumentPart mainPart, DocumentTheme theme)
    {
        // Add logo if available
        if (!string.IsNullOrEmpty(business.LogoUrl))
        {
            try
            {
                AddLogo(body, business.LogoUrl, mainPart);
                AddSpacer(body);
            }
            catch { /* Skip logo if download fails */ }
        }

        // Business name (large, bold)
        body.AppendChild(CreateParagraph(business.Name, bold: true, fontSize: "32", color: theme.PrimaryColor));

        // Business contact info
        if (!string.IsNullOrEmpty(business.Phone))
            body.AppendChild(CreateParagraph($"Phone: {business.Phone}", color: theme.SecondaryColor));
        if (!string.IsNullOrEmpty(business.Email))
            body.AppendChild(CreateParagraph($"Email: {business.Email}", color: theme.SecondaryColor));

        AddSpacer(body);

        // Document title and number (right-aligned)
        body.AppendChild(CreateParagraph(documentTitle, bold: true, fontSize: "28", alignment: W.JustificationValues.Right, color: theme.AccentColor));
        body.AppendChild(CreateParagraph($"#{document.Number}", fontSize: "20", alignment: W.JustificationValues.Right, color: theme.PrimaryColor));
        body.AppendChild(CreateParagraph($"Date: {document.IssuedAt:dd/MM/yyyy}", alignment: W.JustificationValues.Right, color: theme.SecondaryColor));

        if (document.DueAt.HasValue)
            body.AppendChild(CreateParagraph($"Due: {document.DueAt:dd/MM/yyyy}", alignment: W.JustificationValues.Right, color: theme.SecondaryColor));
    }

    private static void AddCustomerSection(W.Body body, TransactionalDocument document, DocumentTheme theme)
    {
        var customerLabel = document.Type == DocumentType.Receipt ? "PAID BY:" : "BILL TO:";
        body.AppendChild(CreateParagraph(customerLabel, bold: true, color: theme.PrimaryColor));
        body.AppendChild(CreateParagraph(document.CustomerName ?? "", color: theme.SecondaryColor));

        if (!string.IsNullOrEmpty(document.CustomerPhone))
            body.AppendChild(CreateParagraph(document.CustomerPhone, color: theme.SecondaryColor));
        if (!string.IsNullOrEmpty(document.CustomerEmail))
            body.AppendChild(CreateParagraph(document.CustomerEmail, color: theme.SecondaryColor));
        if (!string.IsNullOrEmpty(document.BillingAddressLine1))
            body.AppendChild(CreateParagraph(document.BillingAddressLine1, color: theme.SecondaryColor));
        if (!string.IsNullOrEmpty(document.BillingAddressLine2))
            body.AppendChild(CreateParagraph(document.BillingAddressLine2, color: theme.SecondaryColor));
        if (!string.IsNullOrEmpty(document.BillingCity) || !string.IsNullOrEmpty(document.BillingCountry))
            body.AppendChild(CreateParagraph($"{document.BillingCity}{(string.IsNullOrEmpty(document.BillingCity) || string.IsNullOrEmpty(document.BillingCountry) ? "" : ", ")}{document.BillingCountry}", color: theme.SecondaryColor));
        
        if (!string.IsNullOrEmpty(document.Reference))
        {
            AddSpacer(body);
            body.AppendChild(CreateParagraph($"Reference: {document.Reference}", fontSize: "20", color: theme.PrimaryColor));
        }
    }

    private static void AddLineItemsTable(W.Body body, TransactionalDocument document, DocumentTheme theme)
    {
        var table = new W.Table();

        // Table borders
        var tblProp = new W.TableProperties(
            new W.TableBorders(
                new W.TopBorder { Val = W.BorderValues.Single, Size = 4 },
                new W.BottomBorder { Val = W.BorderValues.Single, Size = 4 },
                new W.LeftBorder { Val = W.BorderValues.Single, Size = 4 },
                new W.RightBorder { Val = W.BorderValues.Single, Size = 4 },
                new W.InsideHorizontalBorder { Val = W.BorderValues.Single, Size = 4 },
                new W.InsideVerticalBorder { Val = W.BorderValues.Single, Size = 4 }
            )
        );
        table.AppendChild(tblProp);

        // Header row
        var headerRow = new W.TableRow();
        headerRow.Append(
            CreateTableCell("Item", bold: true, backgroundColor: theme.SecondaryColor, textColor: "#FFFFFF"),
            CreateTableCell("Qty", bold: true, backgroundColor: theme.SecondaryColor, textColor: "#FFFFFF"),
            CreateTableCell("Price", bold: true, backgroundColor: theme.SecondaryColor, textColor: "#FFFFFF"),
            CreateTableCell("Tax", bold: true, backgroundColor: theme.SecondaryColor, textColor: "#FFFFFF"),
            CreateTableCell("Total", bold: true, backgroundColor: theme.SecondaryColor, textColor: "#FFFFFF")
        );
        table.AppendChild(headerRow);

        // Data rows
        foreach (var line in document.Lines)
        {
            var row = new W.TableRow();
            row.Append(
                CreateTableCell(line.Name),
                CreateTableCell(line.Quantity.ToString("N2")),
                CreateTableCell($"{document.Currency} {line.UnitPrice:N2}"),
                CreateTableCell($"{line.TaxRate * 100:N0}%"),
                CreateTableCell($"{document.Currency} {line.LineTotal:N2}")
            );
            table.AppendChild(row);
        }

        body.AppendChild(table);
    }

    private static void AddTotalsSection(W.Body body, TransactionalDocument document, DocumentTheme theme)
    {
        body.AppendChild(CreateParagraph($"Subtotal: {document.Currency} {document.Subtotal:N2}", alignment: W.JustificationValues.Right, color: theme.SecondaryColor));
        body.AppendChild(CreateParagraph($"Tax: {document.Currency} {document.Tax:N2}", alignment: W.JustificationValues.Right, color: theme.SecondaryColor));
        body.AppendChild(CreateParagraph($"TOTAL: {document.Currency} {document.Total:N2}", bold: true, fontSize: "24", alignment: W.JustificationValues.Right, color: theme.AccentColor));
    }

    private static void AddFooter(W.Body body, TransactionalDocument document, DocumentTheme theme)
    {
        if (!string.IsNullOrEmpty(document.Notes))
        {
            body.AppendChild(CreateParagraph("Notes:", bold: true, color: theme.PrimaryColor));
            body.AppendChild(CreateParagraph(document.Notes, color: theme.SecondaryColor));
        }

        if (!string.IsNullOrEmpty(document.Reference))
            body.AppendChild(CreateParagraph($"Reference: {document.Reference}", color: theme.SecondaryColor));
    }

    private static void AddSignatureSection(W.Body body, DocumentSignatureRender signature, DocumentTheme theme, MainDocumentPart mainPart)
    {
        if (!signature.HasSignature)
            return;

        body.AppendChild(CreateParagraph("Authorized Signature", bold: true, fontSize: "24", color: theme.PrimaryColor));

        if (signature.ImageBytes != null && signature.ImageBytes.Length > 0)
        {
            var para = new W.Paragraph();
            var run = new W.Run();
            var drawing = CreateImageDrawing(mainPart, signature.ImageBytes, 1828800L, 609600L); // 2in x 0.67in
            run.AppendChild(drawing);
            para.AppendChild(run);
            body.AppendChild(para);
        }

        if (!string.IsNullOrWhiteSpace(signature.SignedBy) || signature.SignedAt.HasValue)
        {
            var signedLine = $"Signed by {signature.SignedBy} on {signature.SignedAt?.ToString("dd MMM yyyy HH:mm")}";
            body.AppendChild(CreateParagraph(signedLine, color: theme.SecondaryColor));
        }

        if (!string.IsNullOrWhiteSpace(signature.Notes))
        {
            body.AppendChild(CreateParagraph(signature.Notes, color: theme.SecondaryColor));
        }
    }

    // Helper methods
    private static W.Paragraph CreateParagraph(string text, bool bold = false, string fontSize = "22", W.JustificationValues? alignment = null, string? color = null)
    {
        var para = new W.Paragraph();

        if (alignment.HasValue)
            para.AppendChild(new W.ParagraphProperties(new W.Justification { Val = alignment.Value }));

        var run = new W.Run();
        var runProps = new W.RunProperties();

        if (bold)
            runProps.AppendChild(new W.Bold());

        runProps.AppendChild(new W.FontSize { Val = fontSize });
        if (!string.IsNullOrWhiteSpace(color))
            runProps.AppendChild(new W.Color { Val = NormalizeColor(color) });

        run.AppendChild(runProps);
        run.AppendChild(new W.Text(text));

        para.AppendChild(run);
        return para;
    }

    private static W.TableCell CreateTableCell(string text, bool bold = false, string? backgroundColor = null, string? textColor = null)
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
            var cellProps = cell.GetFirstChild<W.TableCellProperties>() ?? new W.TableCellProperties();
            cellProps.AppendChild(new W.Shading
            {
                Fill = NormalizeColor(backgroundColor),
                Val = W.ShadingPatternValues.Clear
            });
            cell.AppendChild(cellProps);
        }

        return cell;
    }

    private static void AddSpacer(W.Body body)
    {
        body.AppendChild(new W.Paragraph());
    }

    private static void AddLogo(W.Body body, string logoUrl, MainDocumentPart mainPart)
    {
        using var httpClient = new HttpClient();
        var imageBytes = httpClient.GetByteArrayAsync(logoUrl).Result;
        using var imageStream = new MemoryStream(imageBytes);

        var imagePart = mainPart.AddImagePart(ImagePartType.Png);
        imagePart.FeedData(imageStream);

        var para = new W.Paragraph();
        var run = new W.Run();

        var drawing = CreateImageDrawing(mainPart, imageStream.ToArray(), 914400L, 914400L);

        run.AppendChild(drawing);
        para.AppendChild(run);
        body.AppendChild(para);
    }

    private static W.Drawing CreateImageDrawing(MainDocumentPart mainPart, byte[] imageBytes, long width, long height)
    {
        using var imageStream = new MemoryStream(imageBytes);
        var imagePart = mainPart.AddImagePart(ImagePartType.Png);
        imagePart.FeedData(imageStream);

        return new W.Drawing(
            new DW.Inline(
                new DW.Extent { Cx = width, Cy = height },
                new DW.DocProperties { Id = 1, Name = "Image" },
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = 0, Name = "Image" },
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

    private static string NormalizeColor(string? color, string fallback = "111827")
    {
        if (string.IsNullOrWhiteSpace(color))
            return fallback;

        var hex = color.Trim();
        if (hex.StartsWith("#"))
            hex = hex[1..];

        return hex.Length == 6 ? hex.ToUpperInvariant() : fallback;
    }
}
