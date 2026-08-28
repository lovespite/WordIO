namespace WordIO.Core.Models;

/// <summary>
/// A table containing rows and cells.
/// </summary>
public sealed class Table : Block
{
    public IList<TableRow> Rows { get; } = new List<TableRow>();

    public override string Text => string.Join(Environment.NewLine, Rows.Select(static row => row.Text));
}
