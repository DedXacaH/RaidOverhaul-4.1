using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Routers;
using Path = System.IO.Path;

namespace RaidOverhaulMain.Helpers;

[Injectable(InjectionType.Singleton)]
public class ROQuestHelper(
    ISptLogger<ROQuestHelper> logger,
    LocaleTable localeTable,
    TemplateTable templateTable,
    ImageRouter imageRouter,
    ModHelper modHelper,
    ROJsonHelper jsonHelper
)
{
    private readonly Assembly _assembly = Assembly.GetExecutingAssembly();

    public void CreateCustomQuests(string questPath)
    {
        var modPath = modHelper.GetAbsolutePathToModFolder(_assembly);
        var questDirectory = Path.Combine(modPath, questPath);
        var questFiles = jsonHelper.LoadCombinedQuestJsons(Path.Combine(questDirectory, "quests"));
        var imageFiles = Directory.GetFiles(Path.Combine(questDirectory, "pics")).ToList();
        var questLocales = Path.Combine(questDirectory, "locales");

        LoadQuestData(questFiles, templateTable);
        LoadQuestLocales(questLocales, localeTable);
        LoadQuestImgs(imageFiles);
    }

    private void LoadQuestData(List<Dictionary<MongoId, Quest>> questFiles, TemplateTable questTable)
    {
        var questCount = 0;
        foreach (var file in questFiles)
        {
            foreach (var (key, quest) in file)
            {
                questTable.Quests[key] = quest;
                questCount++;
            }
        }

        ROLogger.LogDebug(logger, $"Successfully loaded {questCount} quests");
    }

    private void LoadQuestLocales(string localesPath, LocaleTable localesTable)
    {
        var locales = jsonHelper.LoadCombinedLocaleJsons(localesPath);
        var fallback = locales.TryGetValue("en", out var englishLocales) ? englishLocales : locales.Values.FirstOrDefault();

        if (fallback == null)
        {
            return;
        }

        foreach (var (localeCode, lazyLocale) in localesTable.Global)
        {
            lazyLocale.AddTransformer(localeData =>
            {
                if (localeData == null)
                {
                    return localeData;
                }

                var customLocale = locales.GetValueOrDefault(localeCode, fallback);

                foreach (var (key, value) in customLocale)
                {
                    localeData[key] = value;
                }

                return localeData;
            });
        }
    }

    private void LoadQuestImgs(List<string> images)
    {
        foreach (var path in images)
        {
            var image = Path.GetFileNameWithoutExtension(path);
            imageRouter.AddRoute($"/files/quest/icon/{image}", path);
        }

        ROLogger.LogDebug(logger, $"Loaded {images.Count} images");
    }
}
