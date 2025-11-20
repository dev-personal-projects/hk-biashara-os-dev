using ApiWorker.Authentication.Entities;
using ApiWorker.Documents.Entities;
using ApiWorker.Documents.ValueObjects;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentEntityType = ApiWorker.Documents.Entities.DocumentType;
using WordprocessingDocumentRoot = DocumentFormat.OpenXml.Wordprocessing.Document;

namespace ApiWorker.Documents.Templates.Default;

/// <summary>
/// Lightweight DOCX generator used when no custom template is supplied.
/// Mirrors the structure of the QuestPDF output so both formats stay consistent.
/// </summary>
public static class OpenXmlDocumentGenerator
{
    public static MemoryStream GenerateDocument(
        TransactionalDocument document,
        Business business,
        DocumentTheme theme,
        DocumentSignatureRender signature)
    {
        var stream = new MemoryStream();

        using (var wordDoc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new WordprocessingDocumentRoot();
            var body = new Body();
            mainPart.Document.Append(body);

            AppendHeader(body, document, business);
            AppendCustomerSection(body, document);
            AppendReferenceAndDates(body, document);
            AppendLineItemsTable(body, document);
            AppendTotals(body, document);
            AppendNotes(body, document);
            AppendSignature(body, signature);
        }

        stream.Position = 0;
        return stream;
    }

    private static void AppendHeader(Body body, TransactionalDocument document, Business business)
    {
        body.AppendChild(CreateParagraph(business.Name, bold: true, fontSize: "32"));

        if (!string.IsNullOrWhiteSpace(business.Phone))
            body.AppendChild(CreateParagraph(business.Phone));
        if (!string.IsNullOrWhiteSpace(business.Email))
            body.AppendChild(CreateParagraph(business.Email));

        body.AppendChild(CreateParagraph(string.Empty));

        body.AppendChild(CreateParagraph(GetDocumentTitle(document.Type), bold: true, fontSize: "30"));
        body.AppendChild(CreateParagraph($"Number: {document.Number}"));
    }

    private static void AppendCustomerSection(Body body, TransactionalDocument document)
    {
        var label = document.Type == DocumentEntityType.Receipt ? "Paid By:" : "Bill To:";
        body.AppendChild(CreateParagraph(label, bold: true));
        body.AppendChild(CreateParagraph(document.CustomerName ?? string.Empty));
        if (!string.IsNullOrWhiteSpace(document.CustomerPhone))
            body.AppendChild(CreateParagraph(document.CustomerPhone));
        if (!string.IsNullOrWhiteSpace(document.CustomerEmail))
            body.AppendChild(CreateParagraph(document.CustomerEmail));

        var addressLines = new[]
        {
            document.BillingAddressLine1,
            document.BillingAddressLine2,
            string.Join(", ", new[] { document.BillingCity, document.BillingCountry }.Where(x => !string.IsNullOrWhiteSpace(x)))
        }.Where(x => !string.IsNullOrWhiteSpace(x));

        foreach (var line in addressLines)
            body.AppendChild(CreateParagraph(line!));

        body.AppendChild(CreateParagraph(string.Empty));
    }

    private static void AppendReferenceAndDates(Body body, TransactionalDocument document)
    {
        body.AppendChild(CreateParagraph($"Issued: {document.IssuedAt:dd MMM yyyy}"));
        if (document.DueAt.HasValue)
            body.AppendChild(CreateParagraph($"Due: {document.DueAt:dd MMM yyyy}"));
        if (!string.IsNullOrWhiteSpace(document.Reference))
            body.AppendChild(CreateParagraph($"Reference: {document.Reference}"));

        body.AppendChild(CreateParagraph(string.Empty));
    }

    private static void AppendLineItemsTable(Body body, TransactionalDocument document)
    {
        var table = new Table();

        table.AppendChild(new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })));

        var headerRow = new TableRow();
        headerRow.Append(
            CreateTableCell("Item", bold: true),
            CreateTableCell("Qty", bold: true, alignment: JustificationValues.Right),
            CreateTableCell("Price", bold: true, alignment: JustificationValues.Right),
            CreateTableCell("Total", bold: true, alignment: JustificationValues.Right));
        table.AppendChild(headerRow);

        foreach (var line in document.Lines)
        {
            var row = new TableRow();
            row.Append(
                CreateTableCell(line.Name),
                CreateTableCell(line.Quantity.ToString("N2"), alignment: JustificationValues.Right),
                CreateTableCell($"{document.Currency} {line.UnitPrice:N2}", alignment: JustificationValues.Right),
                CreateTableCell($"{document.Currency} {line.LineTotal:N2}", alignment: JustificationValues.Right));
            table.AppendChild(row);
        }

        body.AppendChild(table);
        body.AppendChild(CreateParagraph(string.Empty));
    }

    private static void AppendTotals(Body body, TransactionalDocument document)
    {
        body.AppendChild(CreateParagraph($"Subtotal: {document.Currency} {document.Subtotal:N2}", bold: true, alignment: JustificationValues.Right));

        if (document.Tax > 0)
            body.AppendChild(CreateParagraph($"Tax: {document.Currency} {document.Tax:N2}", alignment: JustificationValues.Right));

        body.AppendChild(CreateParagraph($"Total: {document.Currency} {document.Total:N2}", bold: true, fontSize: "28", alignment: JustificationValues.Right));
    }

    private static void AppendNotes(Body body, TransactionalDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.Notes))
            return;

        body.AppendChild(CreateParagraph(string.Empty));
        body.AppendChild(CreateParagraph("Notes", bold: true));
        body.AppendChild(CreateParagraph(document.Notes));
    }

    private static void AppendSignature(Body body, DocumentSignatureRender signature)
    {
        body.AppendChild(CreateParagraph(string.Empty));
        body.AppendChild(CreateParagraph("Authorized Signature", bold: true));

        if (signature.HasSignature)
        {
            var signedLine = $"Signed by {signature.SignedBy ?? "N/A"}";
            if (signature.SignedAt.HasValue)
                signedLine += $" on {signature.SignedAt:dd MMM yyyy HH:mm}";
            body.AppendChild(CreateParagraph(signedLine));
        }
        else
        {
            body.AppendChild(CreateParagraph("Signature pending"));
        }

        if (!string.IsNullOrWhiteSpace(signature.Notes))
            body.AppendChild(CreateParagraph(signature.Notes));
    }

    private static Paragraph CreateParagraph(
        string text,
        bool bold = false,
        string? fontSize = null,
        JustificationValues? alignment = null)
    {
        var runProperties = new RunProperties();
        if (bold) runProperties.AppendChild(new Bold());
        if (!string.IsNullOrWhiteSpace(fontSize)) runProperties.AppendChild(new FontSize { Val = fontSize });

        var run = new Run(runProperties, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        var paragraph = new Paragraph(run);
        var effectiveAlignment = alignment ?? JustificationValues.Left;
        paragraph.ParagraphProperties = new ParagraphProperties(new Justification { Val = effectiveAlignment });
        return paragraph;
    }

    private static TableCell CreateTableCell(string text, bool bold = false, JustificationValues? alignment = null)
    {
        var paragraph = CreateParagraph(text, bold: bold, alignment: alignment);
        return new TableCell(paragraph);
    }

    private static string GetDocumentTitle(DocumentEntityType type) =>
        type switch
        {
            DocumentEntityType.Invoice => "INVOICE",
            DocumentEntityType.Receipt => "RECEIPT",
            DocumentEntityType.Quotation => "QUOTATION",
            _ => "DOCUMENT"
        };
}

