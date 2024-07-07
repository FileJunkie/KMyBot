using System.Xml.Serialization;

namespace KMyBot.KMy.Models.Internal;

[Serializable]
public class Account
{
    [XmlAttribute("id")]
    public string Id { get; set; } = null!;

    [XmlAttribute("name")]
    public string Name { get; set; } = null!;

    [XmlAttribute("currency")]
    public string Currency { get; set; } = null!;

    [XmlAttribute("parentaccount")]
    public string? ParentAccount { get; set; }

    [XmlArray("SUBACCOUNTS")]
    [XmlArrayItem("SUBACCOUNT", typeof(SubAccount))]
    public SubAccount[] SubAccounts { get; set; } = [];
}