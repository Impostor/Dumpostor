global using static Reactor.Utilities.Logger<Dumpostor.DumpostorPlugin>;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using Dumpostor.Dumpers;
using Dumpostor.Dumpers.Map;
using HarmonyLib;
using InnerNet;
using Reactor;
using Reactor.Utilities;
using Reactor.Utilities.Extensions;
using UnityEngine;

namespace Dumpostor;

[BepInAutoPlugin]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
public sealed partial class DumpostorPlugin : BasePlugin
{
    internal static JsonSerializerOptions JsonSerializerOptions { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(),
            new Vector2Converter(),
        },
    };

    internal static LanguageUnit EnglishLanguageUnit { get; set; }

    public Harmony Harmony { get; } = new(Id);

    public ConfigEntry<string> DumpPath { get; private set; }

    public override void Load()
    {
        DumpPath = Config.Bind("Settings", "DumpPath", "dump");

        if (Environment.GetEnvironmentVariable("DUMPOSTOR_DISABLE") != null)
        {
            Warning("Dumpostor is disabled with an environment variable");
            return;
        }

        Harmony.PatchAll();

        var dumpPath = DumpPath.Value;

        if (string.IsNullOrEmpty(dumpPath) || !Directory.Exists(dumpPath))
        {
            Error("DumpedPath is empty or is not a directory");
            Application.Quit();
            return;
        }

        EntrypointPatch.Initialized += () =>
        {
            Coroutines.Start(Dump(dumpPath));
        };
    }

    private IEnumerator Dump(string dumpPath)
    {
        Debug("Dumping to " + dumpPath);

        DummyMapLoadHacks.Initialize();

        var spawnableObjects = new Dictionary<uint, InnerNetObject>();
        yield return LoadSpawnableObjects(spawnableObjects);

        var dumpers = new IDumper[]
        {
            new InfoDumper(),
            new SpawnableObjectsDumper(spawnableObjects),
            // new TranslationsDumper(),

            new ColorDumper(),
            new EnumDumper<DisconnectReasons>(),
            new EnumDumper<SanctionReasons>(),
            new EnumDumper<GameKeywords>(),
            new EnumDumper<GameOverReason>(),
            new EnumDumper<Platforms>(),
            new EnumDumper<RoleTypes>(),
            new EnumDumper<StringNames>(),
            new EnumDumper<SystemTypes>(),
            new EnumDumper<SupportedLangs>(),
            new EnumDumper<TaskTypes>(),
            new EnumDumper<RpcCalls>(),
        };

        foreach (var dumper in dumpers)
        {
            Info("Dumping " + dumper);

            var outputPath = Path.Combine(dumpPath, dumper.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, dumper.Dump());
        }

        var mapsDirectory = Path.Combine(dumpPath, "maps");
        Directory.CreateDirectory(mapsDirectory);

        var mapDumpers = new IMapDumper[]
        {
            new SpawnDumper(),
            new SystemsDumper(),
            new TaskDumper(),
            new VentDumper(),
            new DoorsDumper(),
        };

        foreach (var spawnableObject in spawnableObjects.Values)
        {
            var shipStatusPrefab = spawnableObject.GetComponent<ShipStatus>();
            if (shipStatusPrefab == null) continue;

            var name = shipStatusPrefab.name.RemoveSuffix("Ship");

            var mapDirectory = Path.Combine(mapsDirectory, name);
            Directory.CreateDirectory(mapDirectory);

            Info("Dumping map " + name);

            var shipStatus = UnityEngine.Object.Instantiate(shipStatusPrefab);

            foreach (var dumper in mapDumpers)
            {
                Info("Dumping " + dumper);

                var outputPath = Path.Combine(mapDirectory, dumper.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                File.WriteAllText(outputPath, dumper.Dump(shipStatus));
            }

            shipStatus.DestroyImmediate();
        }

        Info("Dumped");
        Application.Quit();
    }

    private static IEnumerator LoadSpawnableObjects(Dictionary<uint, InnerNetObject> spawnableObjects)
    {
        foreach (var spawnableObject in AmongUsClient.Instance.NonAddressableSpawnableObjects)
        {
            spawnableObjects.Add(spawnableObject.SpawnId, spawnableObject);
        }

        for (var spawnId = 0; spawnId < AmongUsClient.Instance.SpawnableObjects.Count; spawnId++)
        {
            var assetReference = AmongUsClient.Instance.SpawnableObjects[spawnId];
            if (assetReference == null) continue;

            var handle = assetReference.LoadAssetAsync<GameObject>();
            while (!handle.IsDone) yield return null;

            if (handle.Result == null) continue;

            spawnableObjects.Add((uint)spawnId, handle.Result.GetComponent<InnerNetObject>());
        }
    }
}
