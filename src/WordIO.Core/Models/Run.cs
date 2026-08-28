namespace WordIO.Core.Models;

/// <summary>
/// The smallest text-bearing unit in the document model.
/// </summary>
public sealed class Run
{
    public string Text { get; set; } = string.Empty;

    public string? StyleId { get; set; }

    public bool IsBold { get; set; }

    public bool IsItalic { get; set; }

    public bool IsUnderline { get; set; }

    public bool IsStrikeThrough { get; set; }

    public string? FontName { get; set; }

    public double? FontSizePoints { get; set; }

    public string? Color { get; set; }

    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Baseline;

    public override string ToString() => Text;
}
