using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;

namespace MachineControlPanel;

internal static class Quirks
{
    internal static string DefaultThingId = "(O)0";
    internal static Item? defaultThing = null;
    internal static Item DefaultThing
    {
        get
        {
            defaultThing ??= ItemRegistry.Create(DefaultThingId);
            return defaultThing;
        }
    }

    /// <summary>
    /// abuse of functional programming
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="modelList"></param>
    /// <param name="getId"></param>
    /// <param name="setIdSeq"></param>
    /// <param name="process"></param>
    private static void EnsureUniqueId<T>(
        string debugText,
        List<T>? modelList,
        Func<T, string> getId,
        Action<T, int> setIdSeq,
        Action<T>? process = null
    )
    {
        if (modelList == null)
            return;
        List<T> nullIdModels = [];
        Dictionary<string, List<T>> modelById = [];
        foreach (T model in modelList)
        {
            string id = getId(model);
            if (id == null)
                nullIdModels.Add(model);
            else
            {
                if (!modelById.ContainsKey(id))
                    modelById[id] = [];
                modelById[id].Add(model);
                process?.Invoke(model);
            }
        }

        int i;
        foreach ((string key, List<T> models) in modelById)
        {
            if (models.Count > 1)
            {
                // edit second duplicate and on
                i = 1;
                foreach (T model in models.Skip(1))
                    setIdSeq(model, i++);
            }
        }
        // unsure when model could have null id, log for future detection
        if (nullIdModels.Any())
            ModEntry.LogOnce($"{debugText} has null Ids");
        i = 0;
        foreach (T model in nullIdModels)
        {
            setIdSeq(model, i++);
        }
    }

    /// <summary>
    /// Check if every MachineOutputRule and every has a unique Id field within their lists.
    /// For entries that does not fulfill this requirement, add a suffix to the second duplicate entry and on.
    /// </summary>
    /// <param name="asset"></param>
    internal static void EnsureUniqueMachineOutputRuleId(IAssetData asset)
    {
        IDictionary<string, MachineData> data = asset.AsDictionary<string, MachineData>().Data;
        foreach ((string qItemId, MachineData machine) in data)
        {
            EnsureUniqueId(
                qItemId,
                machine.OutputRules,
                (rule) => rule.Id,
                (rule, seq) => rule.Id = rule.Id == null ? $"null-rule-{seq}" : $"{rule.Id}-rule-{seq}",
                (rule) =>
                {
                    EnsureUniqueId(
                        $"{qItemId}-{rule.Id}",
                        rule.Triggers,
                        (trigger) => trigger.Id,
                        (trigger, seq) =>
                            trigger.Id = trigger.Id == null ? $"null-trigger-{seq}" : $"{trigger.Id}-trigger-{seq}"
                    );
                }
            );
        }
    }

    /// <summary>Add the placeholder item for use in displaying flavored items</summary>
    /// <param name="asset"></param>
    internal static void AddDefaultItemNamedSomethingOtherThanWeedses(IAssetData asset)
    {
        DefaultThingId = $"{ModEntry.ModId}_DefaultItem";
        IDictionary<string, ObjectData> data = asset.AsDictionary<string, ObjectData>().Data;
        data[DefaultThingId] = new()
        {
            Name = DefaultThingId,
            DisplayName = I18n.Object_Thing_DisplayName(),
            Description =
                "Where did you get this? Put it back where you found it (this is a placeholder item from Machine Control Panel)",
            Type = "Basic",
            Category = -20,
            SpriteIndex = 923,
            Edibility = 0,
        };
    }
}
