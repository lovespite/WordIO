namespace WordIO.Core.Xml;

public sealed class XmlText : XmlNode
{
    public XmlText(string value)
    {
        Value = value;
    }

    public override XmlNodeType NodeType => XmlNodeType.Text;

    public override string Value { get; }

    public override string ToString() => Value;
}
