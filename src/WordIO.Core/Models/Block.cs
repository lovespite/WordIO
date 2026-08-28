namespace WordIO.Core.Models;

/// <summary>
/// A block-level piece of document content, such as a paragraph, table, or list.
/// </summary>
public abstract class Block
{
    public abstract string Text { get; }

    public override string ToString() => Text;
}
