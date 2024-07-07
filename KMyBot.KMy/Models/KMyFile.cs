using System.Xml.Linq;

namespace KMyBot.KMy.Models;

public class KMyFile
{
    public IEnumerable<Account> Accounts { get; set; } = [];

    public static async Task<KMyFile> LoadAsync(Stream stream, CancellationToken cancellationToken)
    {
        var document = await XElement.LoadAsync(
            stream,
            LoadOptions.PreserveWhitespace,
            cancellationToken);

        return new()
        {
            Accounts = document
                .Element("ACCOUNTS")!
                .Descendants("ACCOUNT")
                .Select(i => new Account
                {
                    Id = i.Attribute("id")!.Value,
                    Name = i.Attribute("name")!.Value,
                    Currency = i.Attribute("currency")!.Value
                }),
        };
    }
}