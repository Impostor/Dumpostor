using System;
using System.Text.Json;
using System.Collections.Generic;

namespace Dumpostor.Dumpers;

public sealed class TranslationsDumper : IDumper
{
    public string FileName => "translations.json";

    public string Dump()
    {
        var supportedLangs = Enum.GetValues(typeof(SupportedLangs));
        
        var languages = new Dictionary<SupportedLangs, Dictionary<string, string>>();

        foreach (SupportedLangs lang in supportedLangs) {
            TranslatedImageSet languageImageSet = TranslationController.Instance.Languages[lang];
            LanguageUnit languageUnit = new(languageImageSet);

            Dictionary<string, string> AllStrings = new();

            foreach (var entry in languageUnit.AllStrings) {
                AllStrings.Add(entry.Key, entry.Value);
            }

            languages.Add(lang, AllStrings);
        }

        return JsonSerializer.Serialize(
            languages,
            DumpostorPlugin.JsonSerializerOptions
        );
    }
}
