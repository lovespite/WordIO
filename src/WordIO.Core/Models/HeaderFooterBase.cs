namespace WordIO.Core.Models;

/// <summary>
/// Shared content model for headers and footers.
/// </summary>
public abstract class HeaderFooterBase
{
    public HeaderFooterKind Kind { get; set; } = HeaderFooterKind.Default;

    public string? RelationshipId { get; set; }

    public IList<Block> Blocks { get; } = new List<Block>();

    public string Text => string.Join(Environment.NewLine, Blocks.Select(static block => block.Text));

    public override string ToString() => Text;
}
