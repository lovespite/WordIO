namespace WordIO.Core.Models;

/// <summary>
/// A numbered (ordered) list.
/// </summary>
public sealed class OrderedList : List
{
    public override bool IsOrdered => true;
}
