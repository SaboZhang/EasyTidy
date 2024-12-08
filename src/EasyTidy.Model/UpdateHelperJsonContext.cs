using System.Text.Json.Serialization;

namespace EasyTidy.Model;

[JsonSourceGenerationOptions()]
[JsonSerializable(typeof(UpdateInfo))]
public partial class UpdateHelperJsonContext : JsonSerializerContext
{

}
