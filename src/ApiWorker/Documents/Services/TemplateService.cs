using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using ApiWorker.Documents.Entities;
using ApiWorker.Authentication.Entities;
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
    public static MemoryStream GenerateDocument(TransactionalDocument document, Business business)
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
            AddBusinessHeader(body, business, document, documentTitle, mainPart);
            AddSpacer(body);

            // Customer info
            AddCustomerSection(body, document);
            AddSpacer(body);

            // Line items table
            AddLineItemsTable(body, document);
            AddSpacer(body);

            // Totals
            AddTotalsSection(body, document);
            AddSpacer(body);

            // Footer: Notes and reference
            AddFooter(body, document);
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

    private static void AddBusinessHeader(W.Body body, Business business, TransactionalDocument document, string documentTitle, MainDocumentPart mainPart)
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
        body.AppendChild(CreateParagraph(business.Name, bold: true, fontSize: "32"));

        // Business contact info
        if (!string.IsNullOrEmpty(business.Phone))
            body.AppendChild(CreateParagraph($"Phone: {business.Phone}"));
        if (!string.IsNullOrEmpty(business.Email))
            body.AppendChild(CreateParagraph($"Email: {business.Email}"));

        AddSpacer(body);

        // Document title and number (right-aligned)
        body.AppendChild(CreateParagraph(documentTitle, bold: true, fontSize: "28", alignment: W.JustificationValues.Right));
        body.AppendChild(CreateParagraph($"#{document.Number}", fontSize: "20", alignment: W.JustificationValues.Right));
        body.AppendChild(CreateParagraph($"Date: {document.IssuedAt:dd/MM/yyyy}", alignment: W.JustificationValues.Right));

        if (document.DueAt.HasValue)
            body.AppendChild(CreateParagraph($"Due: {document.DueAt:dd/MM/yyyy}", alignment: W.JustificationValues.Right));
    }

    private static void AddCustomerSection(W.Body body, TransactionalDocument document)
    {
        var customerLabel = document.Type == DocumentType.Receipt ? "PAID BY:" : "BILL TO:";
        body.AppendChild(CreateParagraph(customerLabel, bold: true));
        body.AppendChild(CreateParagraph(document.CustomerName ?? ""));

        if (!string.IsNullOrEmpty(document.CustomerPhone))
            body.AppendChild(CreateParagraph(document.CustomerPhone));
        if (!string.IsNullOrEmpty(document.CustomerEmail))
            body.AppendChild(CreateParagraph(document.CustomerEmail));
        if (!string.IsNullOrEmpty(document.BillingAddressLine1))
            body.AppendChild(CreateParagraph(document.BillingAddressLine1));
        if (!string.IsNullOrEmpty(document.BillingAddressLine2))
            body.AppendChild(CreateParagraph(document.BillingAddressLine2));
        if (!string.IsNullOrEmpty(document.BillingCity) || !string.IsNullOrEmpty(document.BillingCountry))
            body.AppendChild(CreateParagraph($"{document.BillingCity}{(string.IsNullOrEmpty(document.BillingCity) || string.IsNullOrEmpty(document.BillingCountry) ? "" : ", ")}{document.BillingCountry}"));
        
        if (!string.IsNullOrEmpty(document.Reference))
        {
            AddSpacer(body);
            body.AppendChild(CreateParagraph($"Reference: {document.Reference}", fontSize: "20"));
        }
    }

    private static void AddLineItemsTable(W.Body body, TransactionalDocument document)
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
            CreateTableCell("Item", bold: true),
            CreateTableCell("Qty", bold: true),
            CreateTableCell("Price", bold: true),
            CreateTableCell("Tax", bold: true),
            CreateTableCell("Total", bold: true)
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

    private static void AddTotalsSection(W.Body body, TransactionalDocument document)
    {
        body.AppendChild(CreateParagraph($"Subtotal: {document.Currency} {document.Subtotal:N2}", alignment: W.JustificationValues.Right));
        body.AppendChild(CreateParagraph($"Tax: {document.Currency} {document.Tax:N2}", alignment: W.JustificationValues.Right));
        body.AppendChild(CreateParagraph($"TOTAL: {document.Currency} {document.Total:N2}", bold: true, fontSize: "24", alignment: W.JustificationValues.Right));
    }

    private static void AddFooter(W.Body body, TransactionalDocument document)
    {
        if (!string.IsNullOrEmpty(document.Notes))
        {
            body.AppendChild(CreateParagraph("Notes:", bold: true));
            body.AppendChild(CreateParagraph(document.Notes));
        }

        if (!string.IsNullOrEmpty(document.Reference))
            body.AppendChild(CreateParagraph($"Reference: {document.Reference}"));
    }

    // Helper methods
    private static W.Paragraph CreateParagraph(string text, bool bold = false, string fontSize = "22", W.JustificationValues? alignment = null)
    {
        var para = new W.Paragraph();

        if (alignment.HasValue)
            para.AppendChild(new W.ParagraphProperties(new W.Justification { Val = alignment.Value }));

        var run = new W.Run();
        var runProps = new W.RunProperties();

        if (bold)
            runProps.AppendChild(new W.Bold());

        runProps.AppendChild(new W.FontSize { Val = fontSize });
        run.AppendChild(runProps);
        run.AppendChild(new W.Text(text));

        para.AppendChild(run);
        return para;
    }

    private static W.TableCell CreateTableCell(string text, bool bold = false)
    {
        var cell = new W.TableCell();
        var para = new W.Paragraph();
        var run = new W.Run();

        if (bold)
            run.AppendChild(new W.RunProperties(new W.Bold()));

        run.AppendChild(new W.Text(text));
        para.AppendChild(run);
        cell.AppendChild(para);

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

        var drawing = new W.Drawing(
            new DW.Inline(
                new DW.Extent { Cx = 914400L, Cy = 914400L }, // 1 inch = 914400 EMUs
                new DW.DocProperties { Id = 1, Name = "Logo" },
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = 0, Name = "Logo" },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip { Embed = mainPart.GetIdOfPart(imagePart) },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0, Y = 0 },
                                    new A.Extents { Cx = 914400L, Cy = 914400L }),
                                new A.PresetGeometry { Preset = A.ShapeTypeValues.Rectangle })))
                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })));

        run.AppendChild(drawing);
        para.AppendChild(run);
        body.AppendChild(para);
    }
}
