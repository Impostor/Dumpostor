using System.Text.Json;
using UnityEngine;

namespace Dumpostor.Dumpers.Map;

public sealed class MushroomMixupCosmeticsDumper
{
    public string FileName => "mixup.json";

    public string Dump(FungleShipStatus shipStatus)
    {
        return JsonSerializer.Serialize(MixupCosmetics.From(shipStatus.specialSabotage), DumpostorPlugin.JsonSerializerOptions);
    }

    public sealed class MixupCosmetics
    {
        public required string[] HatIds { get; init; }
        public required string[] VisorIds { get; init; }
        public required string[] PetIds { get; init; }

        public static MixupCosmetics From(MushroomMixupSabotageSystem system)
        {
            return new MixupCosmetics
            {
                HatIds = system.hatIds,
                VisorIds = system.visorIds,
                PetIds = system.petIds,
            };
        }
    }
}
