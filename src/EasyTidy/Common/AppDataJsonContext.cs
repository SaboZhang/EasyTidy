using EasyTidy.Model;
using System.Text.Json.Serialization;

namespace EasyTidy.Common;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(CoreSettings))]
[JsonSerializable(typeof(Hotkey))]
[JsonSerializable(typeof(List<Hotkey>))]
public partial class AppDataJsonContext : JsonSerializerContext
{

}
