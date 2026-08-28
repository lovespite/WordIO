namespace WordIO.Core.Xml;

public abstract class XmlNode
{
    public abstract XmlNodeType NodeType { get; }

    public XmlElement? Parent { get; internal set; }

    public virtual string? Value => null;
}
