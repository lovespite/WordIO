using System.Globalization;
using System.IO.Compression;
using System.Text;
using WordIO.Core.Models;
using WordIO.Core.Xml;

namespace WordIO.Mini;

/// <summary>
/// Reads the text content of a DOCX package and converts it into the WordIO.Core model.
/// This implementation deliberately focuses on readable text: paragraphs, runs, tables,
/// numbered lists, and bullet lists. Formatting that is not needed for text extraction
/// is intentionally omitted.
/// </summary>
public static class DocxTextReader
{
    public static Document Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    public static string ExtractText(string path) => Read(path).Text;

    public static Document Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var documentEntry = archive.GetEntry("word/document.xml")
            ?? throw new InvalidDataException("The file does not appear to be a valid DOCX package (word/document.xml was not found).");

        using var documentStream = documentEntry.Open();
        using var reader = new StreamReader(documentStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var xml = SimpleXmlParser.Parse(reader.ReadToEnd());
        var body = xml.Root?.Element("body")
            ?? throw new InvalidDataException("word/document.xml does not contain a body element.");

        var numbering = ReadNumbering(archive);
        return BuildDocument(body, numbering);
    }

    private static IReadOnlyDictionary<int, bool> ReadNumbering(ZipArchive archive)
    {
        var numberingEntry = archive.GetEntry("word/numbering.xml");
        if (numberingEntry is null)
        {
            return new Dictionary<int, bool>();
        }

        using var stream = numberingEntry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var xml = SimpleXmlParser.Parse(reader.ReadToEnd());
        return ParseNumbering(xml);
    }

    private static IReadOnlyDictionary<int, bool> ParseNumbering(XmlDocument xml)
    {
        var root = xml.Root;
        if (root is null)
        {
            return new Dictionary<int, bool>();
        }

        var orderedByAbstractId = new Dictionary<int, bool>();

        foreach (var abstractNumbering in root.Elements("abstractNum"))
        {
            if (!TryParseInt(abstractNumbering.GetAttribute("abstractNumId"), out var abstractNumberingId))
            {
                continue;
            }

            var level = abstractNumbering.Elements("lvl")
                .FirstOrDefault(static candidate => candidate.GetAttribute("ilvl") == "0")
                ?? abstractNumbering.Elements("lvl").FirstOrDefault();

            var numberFormat = level?.Element("numFmt")?.GetAttribute("val");
            orderedByAbstractId[abstractNumberingId] =
                !string.Equals(numberFormat, "bullet", StringComparison.OrdinalIgnoreCase);
        }

        var orderedByNumberingId = new Dictionary<int, bool>();

        foreach (var numbering in root.Elements("num"))
        {
            if (!TryParseInt(numbering.GetAttribute("numId"), out var numberingId) ||
                !TryParseInt(numbering.GetAttribute("abstractNumId"), out var abstractNumberingId))
            {
                continue;
            }

            if (orderedByAbstractId.TryGetValue(abstractNumberingId, out var ordered))
            {
                orderedByNumberingId[numberingId] = ordered;
            }
        }

        return orderedByNumberingId;
    }

    private static Document BuildDocument(XmlElement body, IReadOnlyDictionary<int, bool> numbering)
    {
        var document = new Document();
        var section = new Section();
        document.Sections.Add(section);

        List? activeList = null;

        foreach (var child in body.Children)
        {
            switch (child.LocalName)
            {
                case "p":
                    AddParagraph(section, ParseParagraph(child), numbering, ref activeList);
                    break;

                case "tbl":
                    activeList = null;
                    section.Blocks.Add(ParseTable(child));
                    break;

                case "sdt":
                    AddStructuredDocumentContent(child, section, numbering, ref activeList);
                    break;

                case "sectPr":
                    break;
            }
        }

        return document;
    }

    private static void AddStructuredDocumentContent(
        XmlElement structuredDocument,
        Section section,
        IReadOnlyDictionary<int, bool> numbering,
        ref List? activeList)
    {
        foreach (var content in structuredDocument.Elements("sdtContent"))
        {
            foreach (var child in content.Children)
            {
                switch (child.LocalName)
                {
                    case "p":
                        AddParagraph(section, ParseParagraph(child), numbering, ref activeList);
                        break;

                    case "tbl":
                        activeList = null;
                        section.Blocks.Add(ParseTable(child));
                        break;

                    case "sdt":
                        AddStructuredDocumentContent(child, section, numbering, ref activeList);
                        break;
                }
            }
        }
    }

    private static void AddParagraph(
        Section section,
        Paragraph paragraph,
        IReadOnlyDictionary<int, bool> numbering,
        ref List? activeList)
    {
        if (paragraph.NumberingId is not int numberingId)
        {
            activeList = null;
            section.Blocks.Add(paragraph);
            return;
        }

        var ordered = numbering.TryGetValue(numberingId, out var isOrdered) && isOrdered;

        if (activeList is null ||
            activeList.NumberingId != numberingId ||
            activeList.Level != paragraph.IndentLevel ||
            activeList.IsOrdered != ordered)
        {
            activeList = ordered
                ? new OrderedList { NumberingId = numberingId, Level = paragraph.IndentLevel }
                : new List { NumberingId = numberingId, Level = paragraph.IndentLevel };
            section.Blocks.Add(activeList);
        }

        activeList.Items.Add(paragraph);
    }

    private static Paragraph ParseParagraph(XmlElement element)
    {
        var paragraph = new Paragraph();

        foreach (var child in element.Children)
        {
            if (child.LocalName == "pPr")
            {
                ParseParagraphProperties(child, paragraph);
            }
            else
            {
                CollectRunOrContainer(child, paragraph);
            }
        }

        return paragraph;
    }

    private static void CollectRunOrContainer(XmlElement element, Paragraph paragraph)
    {
        if (element.LocalName == "r")
        {
            paragraph.Runs.Add(ParseRun(element));
            return;
        }

        CollectRuns(element, paragraph);
    }

    private static void CollectRuns(XmlElement container, Paragraph paragraph)
    {
        foreach (var child in container.Children)
        {
            CollectRunOrContainer(child, paragraph);
        }
    }

    private static void ParseParagraphProperties(XmlElement properties, Paragraph paragraph)
    {
        foreach (var child in properties.Children)
        {
            switch (child.LocalName)
            {
                case "pStyle":
                    paragraph.StyleId = child.GetAttribute("val");
                    break;

                case "jc":
                    paragraph.Alignment = ParseAlignment(child.GetAttribute("val"));
                    break;

                case "ind":
                    paragraph.LeftIndentTwips = TryParseInt(child.GetAttribute("left"), out var left) ? left : null;
                    paragraph.FirstLineIndentTwips = TryParseInt(child.GetAttribute("firstLine"), out var firstLine) ? firstLine : null;
                    break;

                case "numPr":
                    if (child.Element("ilvl") is XmlElement levelElement &&
                        TryParseInt(levelElement.GetAttribute("val"), out var level))
                    {
                        paragraph.IndentLevel = level;
                    }

                    if (child.Element("numId") is XmlElement numberingIdElement &&
                        TryParseInt(numberingIdElement.GetAttribute("val"), out var numberingId))
                    {
                        paragraph.NumberingId = numberingId;
                    }

                    break;
            }
        }
    }

    private static Run ParseRun(XmlElement element)
    {
        var run = new Run();
        var text = new StringBuilder();

        foreach (var child in element.Children)
        {
            switch (child.LocalName)
            {
                case "rPr":
                    ParseRunProperties(child, run);
                    break;

                case "t":
                    text.Append(child.DirectText);
                    break;

                case "tab":
                    text.Append('\t');
                    break;

                case "br":
                case "cr":
                    text.Append('\n');
                    break;

                case "noBreakHyphen":
                    text.Append('-');
                    break;

                case "softHyphen":
                    text.Append('\u00AD');
                    break;

                case "sym":
                    var symbol = child.GetAttribute("char");
                    if (!string.IsNullOrEmpty(symbol))
                    {
                        text.Append(symbol);
                    }

                    break;
            }
        }

        run.Text = text.ToString();
        return run;
    }

    private static void ParseRunProperties(XmlElement properties, Run run)
    {
        foreach (var child in properties.Children)
        {
            switch (child.LocalName)
            {
                case "rStyle":
                    run.StyleId = child.GetAttribute("val");
                    break;

                case "b":
                    run.IsBold = IsTruthy(child.GetAttribute("val"));
                    break;

                case "i":
                    run.IsItalic = IsTruthy(child.GetAttribute("val"));
                    break;

                case "u":
                    run.IsUnderline = IsTruthy(child.GetAttribute("val"));
                    break;

                case "strike":
                    run.IsStrikeThrough = IsTruthy(child.GetAttribute("val"));
                    break;

                case "color":
                    run.Color = child.GetAttribute("val");
                    break;

                case "sz":
                    if (TryParseDouble(child.GetAttribute("val"), out var halfPoints))
                    {
                        run.FontSizePoints = halfPoints / 2.0;
                    }

                    break;

                case "rFonts":
                    run.FontName =
                        child.GetAttribute("ascii")
                        ?? child.GetAttribute("hAnsi")
                        ?? child.GetAttribute("eastAsia");
                    break;

                case "vertAlign":
                    run.VerticalAlignment = ParseVerticalAlignment(child.GetAttribute("val"));
                    break;
            }
        }
    }

    private static Table ParseTable(XmlElement element)
    {
        var table = new Table();

        foreach (var rowElement in element.Elements("tr"))
        {
            var row = new TableRow();

            foreach (var cellElement in rowElement.Elements("tc"))
            {
                var cell = new TableCell();

                foreach (var paragraphElement in cellElement.Elements("p"))
                {
                    cell.Paragraphs.Add(ParseParagraph(paragraphElement));
                }

                row.Cells.Add(cell);
            }

            table.Rows.Add(row);
        }

        return table;
    }

    private static ParagraphAlignment ParseAlignment(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "center" => ParagraphAlignment.Center,
            "right" => ParagraphAlignment.Right,
            "both" => ParagraphAlignment.Both,
            "distribute" => ParagraphAlignment.Distributed,
            "start" => ParagraphAlignment.Start,
            "end" => ParagraphAlignment.End,
            _ => ParagraphAlignment.Left
        };

    private static VerticalAlignment ParseVerticalAlignment(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "superscript" => VerticalAlignment.Superscript,
            "subscript" => VerticalAlignment.Subscript,
            _ => VerticalAlignment.Baseline
        };

    private static bool IsTruthy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized is not ("0" or "false" or "none" or "off");
    }

    private static bool TryParseInt(string? value, out int result) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

    private static bool TryParseDouble(string? value, out double result) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
}
