namespace WordIO.Core.Models;

/// <summary>
/// A paragraph containing one or more runs.
/// </summary>
public sealed class Paragraph : Block
{
    public IList<Run> Runs { get; } = new List<Run>();

    public string? StyleId { get; set; }

    public ParagraphAlignment Alignment { get; set; } = ParagraphAlignment.Left;

    public int? NumberingId { get; set; }

    public int IndentLevel { get; set; }

    public int? LeftIndentTwips { get; set; }

    public int? FirstLineIndentTwips { get; set; }

    public bool IsListItem => NumberingId.HasValue;

    public override string Text => string.Concat(Runs.Select(static run => run.Text));
}
