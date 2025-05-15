using EasyTidy.Model;
using System.Text.Json.Serialization;

namespace EasyTidy.Common;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(CoreSettings))]
[JsonSerializable(typeof(Hotkey))]
[JsonSerializable(typeof(List<Hotkey>))]
[JsonSerializable(typeof(HotkeysCollection))]
[JsonSerializable(typeof(Dictionary<string, object>))]
public partial class AppDataJsonContext : JsonSerializerContext
{

}
