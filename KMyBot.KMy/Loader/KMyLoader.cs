using System.Xml;
using System.Xml.Serialization;
using KMyBot.KMy.Models.Internal;

namespace KMyBot.KMy.Loader;

public static class KMyLoader
{
    public static object Load(Stream stream)
    {
        var serializer = new XmlSerializer(typeof(KMyMoneyFile));
        var xmlReader = XmlReader.Create(stream, new()
        {
            DtdProcessing = DtdProcessing.Parse,
        });

        return serializer.Deserialize(xmlReader) ?? throw new Exception("Can't load file");
    }
}