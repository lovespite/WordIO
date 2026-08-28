namespace WordIO.Core.Models;

/// <summary>
/// Root object for a Word document. A document is modelled as one or more sections.
/// </summary>
public sealed class Document
{
    public IList<Section> Sections { get; } = new List<Section>();

    public IEnumerable<Block> Blocks => Sections.SelectMany(static section => section.Blocks);

    public IEnumerable<Paragraph> Paragraphs => Blocks.OfType<Paragraph>();

    public IEnumerable<Table> Tables => Blocks.OfType<Table>();

    public IEnumerable<List> Lists => Blocks.OfType<List>();

    public string Text => string.Join(Environment.NewLine, Blocks.Select(static block => block.Text));
}
