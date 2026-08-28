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

    public IEnumerable<Header> Headers => Sections.SelectMany(static section => section.Headers);

    public IEnumerable<Footer> Footers => Sections.SelectMany(static section => section.Footers);

    public string BodyText => string.Join(Environment.NewLine, Blocks.Select(static block => block.Text));

    public string Text => string.Join(Environment.NewLine, Sections.Select(static section => section.Text));
}
