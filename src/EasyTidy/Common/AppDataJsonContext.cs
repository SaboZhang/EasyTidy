using EasyTidy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EasyTidy.Common;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(CoreSettings))]
public partial class AppDataJsonContext : JsonSerializerContext
{

}
