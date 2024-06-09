using System.Xml.Serialization;

namespace KMyBot.KMy.Models.Internal;

[Serializable]
[XmlRoot("KMYMONEY-FILE")]
public class KMyMoneyFile
{
    [XmlArray("ACCOUNTS")]
    [XmlArrayItem("ACCOUNT", typeof(Account))]
    public Account[] Accounts { get; set; } = [];
}