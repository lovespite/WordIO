namespace WordIO.Core.Models;

/// <summary>
/// An unordered (bulleted) list. Use <see cref="OrderedList"/> for numbered lists.
/// </summary>
public class List : Block
{
    public int? NumberingId { get; set; }

    public int Level { get; set; }

    public IList<Paragraph> Items { get; } = new List<Paragraph>();

    public virtual bool IsOrdered => false;

    public override string Text => string.Join(Environment.NewLine, Items.Select(static item => item.Text));
}
