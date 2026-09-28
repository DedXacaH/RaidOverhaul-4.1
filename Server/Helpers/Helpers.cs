using System.Reflection;
using RaidOverhaulMain.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace RaidOverhaulMain.Helpers;

[Injectable(InjectionType.Singleton)]
public class ROHelpers(
    ISptLogger<ROHelpers> logger,
    TemplateTable templateTable,
    LocaleService localeService,
    IReadOnlyList<SptMod> sptModsList,
    PresetHelper presetHelper,
    HandbookHelper handbookHelper,
    ItemHelper itemHelper,
    RandomUtil randomUtil,
    JsonUtil jsonUtil,
    ModHelper modHelper
)
{
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();

    public bool CheckForMod(string modGuid)
    {
        return sptModsList.Any(m => m.ModMetadata.ModGuid == modGuid);
    }

    public static bool CheckFilePath(string path, string fileName)
    {
        var filePath = Path.Combine(path, fileName);
        filePath += ".json";

        return File.Exists(filePath);
    }

    public int GenRandomCount(int min, int max)
    {
        return randomUtil.RandInt(min, max);
    }

    public HandbookItem? GetItemInHandbook(string itemId)
    {
        var hbItem = templateTable.Handbook.Items.SingleOrDefault(x => x.Id == itemId);

        return hbItem;
    }

    public double? GetStackedItemPrice(MongoId itemTpl, IEnumerable<Item> items)
    {
        return itemHelper.IsOfBaseclass(itemTpl, BaseClasses.AMMO_BOX)
            ? GetAmmoBoxPrice(items) * 1.15
            : handbookHelper.GetTemplatePrice(itemTpl) * 1.15;
    }

    public double? GetAmmoBoxPrice(IEnumerable<Item> items)
    {
        var total = 0D;
        foreach (var item in items)
        {
            if (itemHelper.IsOfBaseclass(item.Template, BaseClasses.AMMO))
            {
                total += handbookHelper.GetTemplatePrice(item.Template) * (item.Upd?.StackObjectsCount ?? 1);
            }
        }

        return total;
    }

    public void AddToCases(string[] casesToAdd, MongoId itemToAdd)
    {
        var items = templateTable.Items;

        foreach (var cases in casesToAdd)
        {
            foreach (var (item, _) in items)
            {
                if (items[item].Id != cases)
                {
                    return;
                }
                if (items[item].Properties?.Grids?.First().Properties?.Filters?.First().Filter == null)
                {
                    return;
                }
                items[item].Properties?.Grids?.First().Properties?.Filters?.First().Filter?.Add(itemToAdd);
            }
        }
    }

    public void ModifyContainerSize(MongoId containerToModify, int horizontal, int vertical)
    {
        var items = templateTable.Items;

        if (!items.TryGetValue(containerToModify, out var container))
        {
            return;
        }
        var grids = container.Properties?.Grids?.First();

        if (grids == null || grids.Properties == null)
        {
            return;
        }

        grids.Properties.CellsH = horizontal;
        grids.Properties.CellsV = vertical;
    }

    public string FetchIdFromMap(string key, Dictionary<string, MongoId> map)
    {
        if (MongoId.IsValidMongoId(key))
        {
            return key;
        }

        if (map.TryGetValue(key, out var fetchedKey))
        {
            var finalKey = fetchedKey.ToString();

            return finalKey;
        }
        throw new ArgumentException($"'{key}' was not found in map.");
    }

    public T LoadConfig<T>(string dataPath, string configName)
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(_assembly);
        var finalPath = Path.Combine(pathToMod, dataPath);
        var config = modHelper.GetJsonDataFromFile<T>(finalPath, configName);

        return config;
    }

    public void WriteConfigFile<T>(T data, string dataPath, string configName)
    {
        if (data == null)
        {
            return;
        }
        var pathToMod = modHelper.GetAbsolutePathToModFolder(_assembly);
        var finalPath = Path.Combine(pathToMod, dataPath);

        if (!Directory.Exists(finalPath))
        {
            Directory.CreateDirectory(finalPath);
        }
        if (!CheckFilePath(finalPath, configName))
        {
            var filePath = Path.Combine(finalPath, configName);
            var jsonString = jsonUtil.Serialize(data, true);
            File.Create(filePath).Dispose();

            var streamWriter = new StreamWriter(filePath);
            streamWriter.Write(jsonString);
            streamWriter.Flush();
            streamWriter.Close();
        }
        else if (CheckFilePath(finalPath, configName))
        {
            var filePath = Path.Combine(finalPath, configName);
            var jsonString = jsonUtil.Serialize(data, true);
            File.Delete(filePath);
            File.Create(filePath).Dispose();

            var streamWriter = new StreamWriter(filePath);
            streamWriter.Write(jsonString);
            streamWriter.Flush();
            streamWriter.Close();
        }
    }

    public void DumpDataMaps()
    {
        var dumpedDataPath = Path.Combine("db", "devFiles", "dumpedData");
        var itemMap = new SortedDictionary<string, MongoId>();
        var presetMap = new SortedDictionary<string, Preset>();
        var items = templateTable.Items;
        var locales = localeService.GetLocaleDb();
        var defaultPresets = presetHelper.GetAllPresets();

        foreach (var (itemId, data) in items)
        {
            try
            {
                itemMap.TryAdd(
                    locales[data.Parent + " Name"].ToUpperInvariant().Replace(" ", "_").Replace(".", "").Replace("(", "").Replace(")", "")
                        + "_"
                        + locales[itemId + " Name"].ToUpperInvariant().Replace(" ", "_").Replace(".", "").Replace("(", "").Replace(")", ""),
                    itemId
                );
            }
            catch (Exception ex)
            {
                ROLogger.Log(logger, $"Error adding item {itemId} to item map => " + ex, Spectre.Console.Color.Yellow);
                continue;
            }
        }

        foreach (var defaultPreset in defaultPresets ?? [])
        {
            try
            {
                presetMap.TryAdd(defaultPreset.Name.ToUpperInvariant().Replace(" ", "_"), defaultPreset);
            }
            catch (Exception ex)
            {
                ROLogger.Log(logger, $"Error adding preset {defaultPreset.Name} to preset map => " + ex, Spectre.Console.Color.Yellow);
                continue;
            }
        }

        try
        {
            WriteConfigFile(itemMap, dumpedDataPath, "dumpedItemMap.json");
            WriteConfigFile(presetMap, dumpedDataPath, "dumpedPresetMap.json");
        }
        catch (Exception ex)
        {
            ROLogger.Log(logger, $"Error writing maps => " + ex);
        }
    }

    private int WeatherOptionCount(ConfigFile config)
    {
        return (config.AllSeasons ? 1 : 0)
            + (config.NoWinter ? 1 : 0)
            + (config.SeasonalProgression ? 1 : 0)
            + (config.WinterWonderland ? 1 : 0);
    }

    public bool IsOnlyWeatherOption(bool option, ConfigFile config)
    {
        return option && WeatherOptionCount(config) == 1;
    }

    public bool HasConflictingWeatherOptions(ConfigFile config)
    {
        return WeatherOptionCount(config) > 1;
    }
}
