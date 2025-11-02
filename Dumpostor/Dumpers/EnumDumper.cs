using System;
using System.Linq;
using System.Text.Json;
using Il2CppSystem.IO;

namespace Dumpostor.Dumpers;

public sealed class EnumDumper<T> : IDumper where T : Enum
{
    public string FileName => Path.Combine("enums", typeof(T).Name + ".json");

    public string Dump()
    {
        return JsonSerializer.Serialize(
            Extensions.GetValues<T>().ToDictionary(k => Enum.GetName(typeof(T), k), v => v),
            DumpostorPlugin.JsonSerializerOptions
        );
    }
}
