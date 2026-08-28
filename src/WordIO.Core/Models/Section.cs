namespace WordIO.Core.Models;

/// <summary>
/// A contiguous section of a document. Each section contains block-level content.
/// </summary>
public sealed class Section
{
    public IList<Block> Blocks { get; } = new List<Block>();

    public IList<Header> Headers { get; } = new List<Header>();

    public IList<Footer> Footers { get; } = new List<Footer>();

    public string BodyText => string.Join(Environment.NewLine, Blocks.Select(static block => block.Text));

    public string Text
    {
        get
        {
            var parts = new List<string>();
            parts.AddRange(Headers.Select(static header => header.Text));
            parts.Add(BodyText);
            parts.AddRange(Footers.Select(static footer => footer.Text));

            return string.Join(Environment.NewLine, parts.Where(static text => text.Length > 0));
        }
    }
}
