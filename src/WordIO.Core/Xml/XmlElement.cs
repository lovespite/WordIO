namespace WordIO.Core.Xml;

public sealed class XmlElement : XmlNode
{
    private readonly List<XmlNode> _children = [];

    public XmlElement(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        var separatorIndex = name.IndexOf(':');
        if (separatorIndex >= 0)
        {
            Prefix = name[..separatorIndex];
            LocalName = name[(separatorIndex + 1)..];
        }
        else
        {
            Prefix = null;
            LocalName = name;
        }
    }

    public override XmlNodeType NodeType => XmlNodeType.Element;

    public string Name { get; }

    public string? Prefix { get; }

    public string LocalName { get; }

    public IDictionary<string, string> Attributes { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

    public IReadOnlyList<XmlNode> ChildNodes => _children;

    public IEnumerable<XmlElement> ChildElements => _children.OfType<XmlElement>();

    public IEnumerable<XmlElement> Children => ChildElements;

    public string DirectText => string.Concat(_children.OfType<XmlText>().Select(static text => text.Value));

    public string InnerText => string.Concat(DescendantNodes().OfType<XmlText>().Select(static text => text.Value));

    public string? GetAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (Attributes.TryGetValue(name, out var value))
        {
            return value;
        }

        foreach (var attribute in Attributes)
        {
            var localName = attribute.Key.IndexOf(':') is int index and >= 0
                ? attribute.Key[(index + 1)..]
                : attribute.Key;

            if (string.Equals(localName, name, StringComparison.Ordinal))
            {
                return attribute.Value;
            }
        }

        return null;
    }

    public IEnumerable<XmlElement> Elements(string? localName = null)
    {
        foreach (var child in _children)
        {
            if (child is XmlElement element &&
                (localName is null || string.Equals(element.LocalName, localName, StringComparison.Ordinal)))
            {
                yield return element;
            }
        }
    }

    public XmlElement? Element(string? localName = null) => Elements(localName).FirstOrDefault();

    public IEnumerable<XmlNode> DescendantNodes()
    {
        foreach (var child in _children)
        {
            yield return child;

            if (child is XmlElement element)
            {
                foreach (var descendant in element.DescendantNodes())
                {
                    yield return descendant;
                }
            }
        }
    }

    public IEnumerable<XmlElement> Descendants(string? localName = null)
    {
        foreach (var node in DescendantNodes())
        {
            if (node is XmlElement element &&
                (localName is null || string.Equals(element.LocalName, localName, StringComparison.Ordinal)))
            {
                yield return element;
            }
        }
    }

    internal void AddChild(XmlNode child)
    {
        ArgumentNullException.ThrowIfNull(child);
        child.Parent = this;
        _children.Add(child);
    }

    public override string ToString() => $"<{Name}>";
}
