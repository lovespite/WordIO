namespace WordIO.Core.Xml;

public sealed class XmlParseException : Exception
{
    public XmlParseException(string message, int position)
        : base(message)
    {
        Position = position;
    }

    public int Position { get; }
}
