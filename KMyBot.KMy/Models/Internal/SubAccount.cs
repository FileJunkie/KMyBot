using System.Xml.Serialization;

namespace KMyBot.KMy.Models.Internal;

[Serializable]
public class SubAccount
{
    [XmlAttribute("id")]
    public required string Id { get; set; }
}