using EasyTidy.Model;
using System.Text.Json.Serialization;

namespace EasyTidy.Common;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(CoreSettings))]
public partial class AppDataJsonContext : JsonSerializerContext
{

}
