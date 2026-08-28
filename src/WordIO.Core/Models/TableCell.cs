namespace WordIO.Core.Models;

public sealed class TableCell
{
    public IList<Paragraph> Paragraphs { get; } = new List<Paragraph>();

    public string Text => string.Join(Environment.NewLine, Paragraphs.Select(static paragraph => paragraph.Text));

    public override string ToString() => Text;
}
