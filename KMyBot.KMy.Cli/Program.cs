using System.IO.Compression;
using System.Text.Json;
using KMyBot.KMy.Models;

var file = File.OpenRead("../testdata/sample.kmy");
var unzippedFile = new GZipStream(file, CompressionMode.Decompress);
var parsedFile = await KMyFile.LoadAsync(unzippedFile, CancellationToken.None);

Console.WriteLine(JsonSerializer.Serialize(parsedFile, new JsonSerializerOptions
{
    WriteIndented = true,
}));
