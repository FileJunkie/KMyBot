using System.IO.Compression;
using System.Text.Json;
using KMyBot.KMy.Loader;

var file = File.OpenRead("../testdata/sample.kmy");
var unzippedFile = new GZipStream(file, CompressionMode.Decompress);
var parsedFile = KMyLoader.Load(unzippedFile);

Console.WriteLine(JsonSerializer.Serialize(parsedFile, new JsonSerializerOptions
{
    WriteIndented = true,
}));
