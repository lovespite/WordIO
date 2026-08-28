namespace WordIO.Core.Models;

public sealed class TableRow
{
    public IList<TableCell> Cells { get; } = new List<TableCell>();

    public string Text => string.Join('\t', Cells.Select(static cell => cell.Text));

    public override string ToString() => Text;
}
