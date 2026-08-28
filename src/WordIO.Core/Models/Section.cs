namespace WordIO.Core.Models;

/// <summary>
/// A contiguous section of a document. Each section contains block-level content.
/// </summary>
public sealed class Section
{
    public IList<Block> Blocks { get; } = new List<Block>();

    public string Text => string.Join(Environment.NewLine, Blocks.Select(static block => block.Text));
}
