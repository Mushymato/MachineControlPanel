using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MachineControlPanel.GUI;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.Machines;

namespace MachineControlPanel;

internal static class Patches
{
    /// <summary>Indicate why a rule or input is skipped by this mod</summary>
    internal enum SkipReason
    {
        None = 0,
        Rule = 1,
        Input = 2,
        Quality = 3,
    }

    private static MethodInfo LAItemLookupProviderBuildSubject = null!;

    /// <summary>Hold skip reason, here is hoping single thread means its safe lol</summary>
    private static PerScreen<SkipReason> skipped = new();

    internal static void Patch()
    {
        Harmony harmony = new(ModEntry.ModId);
        skipped.Value = SkipReason.None;

        // important patches
        harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(MachineDataUtility), nameof(MachineDataUtility.CanApplyOutput)),
            transpiler: new HarmonyMethod(typeof(Patches), nameof(MachineDataUtility_CanApplyOutput_Transpiler))
        );
        ModEntry.Log($"Applied MachineDataUtility.CanApplyOutput Transpiler", LogLevel.Trace);
        harmony.Patch(
            original: AccessTools.Method(typeof(SObject), nameof(SObject.PlaceInMachine)),
            prefix: new HarmonyMethod(typeof(Patches), nameof(SObject_PlaceInMachine_Prefix)),
            transpiler: new HarmonyMethod(typeof(Patches), nameof(SObject_PlaceInMachine_Transpiler))
        );
        ModEntry.Log($"Applied SObject.PlaceInMachine Transpiler", LogLevel.Trace);

        // lookup anything patch
        try
        {
            var modInfo = ModEntry.help.ModRegistry.Get("Pathoschild.LookupAnything");
            if (modInfo?.GetType().GetProperty("Mod")?.GetValue(modInfo) is IMod mod)
            {
                var assembly = mod.GetType().Assembly;
                if (
                    assembly.GetType("Pathoschild.Stardew.LookupAnything.Framework.Lookups.Items.ItemLookupProvider")
                        is Type LAItemLookupProviderType
                    && AccessTools.Method(LAItemLookupProviderType, "GetSubject")
                        is MethodInfo LAItemLookupProviderGetTargets
                    && assembly.GetType("Pathoschild.Stardew.LookupAnything.Framework.Data.ObjectContext")
                        is Type LAObjectContextType
                    && (
                        LAItemLookupProviderBuildSubject = AccessTools.DeclaredMethod(
                            LAItemLookupProviderType,
                            "BuildSubject",
                            [typeof(Item), LAObjectContextType, typeof(GameLocation), typeof(bool)]
                        )
                    ) != null
                )
                {
                    ModEntry.Log(
                        $"Patching LookupAnything: {LAItemLookupProviderGetTargets}::{LAItemLookupProviderGetTargets}"
                    );
                    harmony.Patch(
                        original: LAItemLookupProviderGetTargets,
                        postfix: new HarmonyMethod(typeof(Patches), nameof(ItemLookupProvider_GetSubject_Postfix))
                    );
                }
                else
                {
                    foreach (Type type in typeof(Game1).Assembly.GetTypes())
                    {
                        ModEntry.Log(type.ToString());
                    }
                }
            }
        }
        catch (Exception err)
        {
            ModEntry.Log($"Failed to patch lookup anything ItemLookupProvider.GetSubject:\n{err}");
        }

        // non-vital patches
        harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.OutputDeconstructor)),
            transpiler: new HarmonyMethod(
                AccessTools.Method(typeof(Patches), nameof(Object_OutputDeconstructor_Transpiler)),
                priority: Priority.VeryLow
            )
        );
    }

    private static void ItemLookupProvider_GetSubject_Postfix(object __instance, ref object __result)
    {
        // this.BuildSubject(item, ObjectContext.Inventory, null);
        // Pathoschild.Stardew.LookupAnything.Framework.Lookups.Items.ItemSubject
        if (__result != null)
            return;

        if (MenuHandler.HoveredItem is Item hoveredItem)
        {
            try
            {
                var res = LAItemLookupProviderBuildSubject.Invoke(__instance, [hoveredItem, 2, null, false]);
                if (res != null)
                    __result = res;
            }
            catch (Exception err)
            {
                ModEntry.Log($"Failed in ItemLookupProvider_GetSubject_Postfix:\n{err}");
            }
        }
    }

    private static bool ShouldSkipMachineInputEntry(
        SObject machine,
        Item inputItem,
        ModSaveDataEntry msdEntry,
        RuleIdent ident
    )
    {
        // maybe better to check once in the postfix rather than in the iteration, but eh
        if (inputItem != null)
        {
            if (msdEntry.Inputs.Contains(inputItem.QualifiedItemId))
            {
                ModEntry.LogOnce($"{machine.QualifiedItemId} Input {inputItem.QualifiedItemId} disabled.");
                skipped.Value = SkipReason.Input;
                return true;
            }
            if (msdEntry.Quality[inputItem.Quality])
            {
                ModEntry.LogOnce($"{machine.QualifiedItemId} Quality {inputItem.Quality} disabled.");
                skipped.Value = SkipReason.Quality;
                return true;
            }
        }
        if (msdEntry.Rules.Contains(ident))
        {
            ModEntry.LogOnce($"{machine.QualifiedItemId} Rule {ident} disabled.");
            if (skipped.Value != SkipReason.Input)
                skipped.Value = SkipReason.Rule;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Check whether a rule or input item should be skipped
    /// </summary>
    /// <param name="trigger2"></param>
    /// <param name="machine"></param>
    /// <param name="rule"></param>
    /// <param name="inputItem"></param>
    /// <param name="idx"></param>
    /// <returns></returns>
    private static bool ShouldSkipMachineInput(
        MachineOutputTriggerRule trigger2,
        SObject machine,
        MachineOutputRule rule,
        Item inputItem
    )
    {
        if (trigger2 == null || rule == null || machine == null || machine.Location == null)
            return false;
        RuleIdent ident = new(rule.Id, trigger2.Id);
        // check local
        if (
            ModEntry.SaveData.TryGetModSaveDataEntry(
                machine.QualifiedItemId,
                machine.Location.NameOrUniqueName,
                out ModSaveDataEntry? msdEntry
            ) && ShouldSkipMachineInputEntry(machine, inputItem, msdEntry, ident)
        )
            return true;
        // check global
        if (
            ModEntry.SaveData.TryGetModSaveDataEntry(machine.QualifiedItemId, null, out msdEntry)
            && ShouldSkipMachineInputEntry(machine, inputItem, msdEntry, ident)
        )
            return true;
        return false;
    }

    /// <summary>
    /// Check should skip, for <see cref="MachineOutputTrigger.DayUpdate"/>
    /// </summary>
    /// <param name="trigger"></param>
    /// <param name="trigger2"></param>
    /// <param name="machine"></param>
    /// <param name="rule"></param>
    /// <param name="inputItem"></param>
    /// <param name="idx"></param>
    /// <returns></returns>
    private static bool ShouldSkipMachineInput_DayUpdate(
        MachineOutputTrigger trigger,
        MachineOutputTriggerRule trigger2,
        SObject machine,
        MachineOutputRule rule,
        Item inputItem
    )
    {
        if (
            trigger.HasFlag(MachineOutputTrigger.DayUpdate)
            || trigger.HasFlag(MachineOutputTrigger.MachinePutDown)
            || trigger.HasFlag(MachineOutputTrigger.OutputCollected)
        )
            return ShouldSkipMachineInput(trigger2, machine, rule, inputItem);
        return false;
    }

    /// <summary>Unset skipped reason</summary>
    private static void SObject_PlaceInMachine_Prefix()
    {
        skipped.Value = SkipReason.None;
    }

    /// <summary>
    /// Patch checks in CanApplyOutput
    /// </summary>
    /// <param name="instructions"></param>
    /// <param name="generator"></param>
    /// <returns></returns>
    private static IEnumerable<CodeInstruction> MachineDataUtility_CanApplyOutput_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        try
        {
            CodeMatcher matcher = new(instructions, generator);

            // insert just before if (trigger2.RequiredCount > inputItem.Stack)
            matcher
                .Start()
                .MatchStartForward(
                    [
                        new(OpCodes.Brfalse_S),
                        new(inst => inst.IsLdloc()),
                        new(
                            OpCodes.Callvirt,
                            AccessTools.PropertyGetter(
                                typeof(MachineOutputTriggerRule),
                                nameof(MachineOutputTriggerRule.RequiredCount)
                            )
                        ),
                        new(OpCodes.Ldarg_3),
                        new(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Item), nameof(Item.Stack))),
                    ]
                )
                .ThrowIfNotMatch("Did not find 'if (trigger2.RequiredCount > inputItem.Stack)'");

            Label lbl = (Label)matcher.Operand; // label to end of loop
            matcher.Advance(1);
            CodeInstruction ldloc = new(matcher.Opcode, matcher.Operand);

            // Check for MachineOutputTrigger.ItemPlacedInMachine
            matcher
                .Advance(1)
                .InsertAndAdvance(
                    [
                        // ldloc from match
                        new(OpCodes.Ldarg_0), // Object machine
                        new(OpCodes.Ldarg_1), // MachineOutputRule rule
                        new(OpCodes.Ldarg_3), // Item inputItem
                        // new(OpCodes.Ldloc, idx), // foreach idx
                        new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Patches), nameof(ShouldSkipMachineInput))),
                        new(OpCodes.Brtrue, lbl),
                        ldloc, // MachineOutputTriggerRule trigger2
                    ]
                );

            // Check for MachineOutputTrigger.DayUpdate
            matcher
                .MatchStartForward(
                    [
                        new(OpCodes.Ldarg_S, (byte)6),
                        new(ldloc.opcode, ldloc.operand),
                        new(OpCodes.Stind_Ref),
                        new(OpCodes.Ldarg_S, (byte)7),
                        new(OpCodes.Ldc_I4_0),
                        new(OpCodes.Stind_I1),
                    ]
                )
                .ThrowIfNotMatch("Failed 'triggerRule = trigger2; matchesExceptCount = false;");

            matcher
                .SetAndAdvance(OpCodes.Ldarg_2, null) // MachineOutputTrigger trigger
                .Insert(
                    [
                        ldloc.Clone(), // MachineOutputTriggerRule trigger2
                        new(OpCodes.Ldarg_0), // Object machine
                        new(OpCodes.Ldarg_1), // MachineOutputRule rule
                        new(OpCodes.Ldarg_3), // Item inputItem
                        // new(OpCodes.Ldloc, idx), // foreach idx
                        new(
                            OpCodes.Call,
                            AccessTools.DeclaredMethod(typeof(Patches), nameof(ShouldSkipMachineInput_DayUpdate))
                        ),
                        new(OpCodes.Brtrue, lbl),
                        new(OpCodes.Ldarg_S, (byte)6),
                    ]
                );

            return matcher.Instructions();
        }
        catch (Exception err)
        {
            ModEntry.Log($"Error in MachineDataUtility_CanApplyOutput_Transpiler:\n{err}", LogLevel.Error);
            return instructions;
        }
    }

    /// <summary>
    /// Display HUD message for rejecting an input, if <see cref="skipped"/> is set
    /// </summary>
    /// <param name="inputItem"></param>
    /// <param name="who"></param>
    private static void ShowSkippedReasonMessage(Item inputItem, Farmer who)
    {
        if (inputItem != null && who != null)
        {
            if (
                skipped.Value switch
                {
                    SkipReason.Rule => I18n.SkipReason_Rules(inputItem.DisplayName),
                    SkipReason.Input => I18n.SkipReason_Inputs(inputItem.DisplayName),
                    SkipReason.Quality => I18n.SkipReason_Quality(inputItem.DisplayName),
                    _ => null,
                }
                is string skipMsg
            )
            {
                Game1.showRedMessage(skipMsg);
                who.ignoreItemConsumptionThisFrame = true;
            }
        }
        skipped.Value = SkipReason.None;
    }

    /// <summary>
    /// Patch PlaceInMachine to show HUD messages
    /// </summary>
    /// <param name="instructions"></param>
    /// <param name="generator"></param>
    /// <returns></returns>
    private static IEnumerable<CodeInstruction> SObject_PlaceInMachine_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        try
        {
            CodeMatcher matcher = new(instructions, generator);

            matcher
                .Start()
                .MatchStartForward(
                    [
                        new(
                            OpCodes.Stfld,
                            AccessTools.Field(typeof(Farmer), nameof(Farmer.ignoreItemConsumptionThisFrame))
                        ),
                        new(OpCodes.Ldc_I4_0),
                        new(OpCodes.Ret),
                        new(OpCodes.Ldarg_3),
                    ]
                )
                .ThrowIfNotMatch("Did not find 'who.ignoreItemConsumptionThisFrame = true; } } return false;");
            matcher.Advance(1);
            // if not reached by jump, go to ret
            matcher.InsertAndAdvance([new(OpCodes.Br, matcher.Labels.Last())]);
            matcher.Insert(
                [
                    new(OpCodes.Ldarg_2),
                    new(OpCodes.Ldarg_S, (sbyte)4),
                    new(OpCodes.Call, AccessTools.Method(typeof(Patches), nameof(ShowSkippedReasonMessage))),
                ]
            );
            matcher.CreateLabel(out Label lbl);

            // change 2 prev false branches to jump to the new label

            matcher.MatchEndBackwards(
                [
                    new(
                        OpCodes.Call,
                        AccessTools.Method(
                            typeof(GameStateQuery),
                            nameof(GameStateQuery.CheckConditions),
                            [
                                typeof(string),
                                typeof(GameLocation),
                                typeof(Farmer),
                                typeof(Item),
                                typeof(Random),
                                typeof(HashSet<string>),
                            ]
                        )
                    ),
                    new(OpCodes.Brfalse_S),
                ]
            );
            matcher.Operand = lbl;

            matcher.MatchEndBackwards(
                [
                    new(OpCodes.Ldfld, AccessTools.Field(typeof(MachineData), nameof(MachineData.InvalidItemMessage))),
                    new(OpCodes.Brfalse_S),
                ]
            );
            matcher.Operand = lbl;

            return matcher.Instructions();
        }
        catch (Exception err)
        {
            ModEntry.Log($"Error in MachineDataUtility_CanApplyOutput_Transpiler:\n{err}", LogLevel.Error);
            return instructions;
        }
    }

    private static string CheckDeconstructorQualifiedItemId(string itemId)
    {
        if (ItemRegistry.IsQualifiedItemId(itemId))
        {
            if (itemId.StartsWith("(O)"))
                return itemId[3..].TrimStart();
            return Item.ErrorItemName;
        }
        return itemId;
    }

    private static IEnumerable<CodeInstruction> Object_OutputDeconstructor_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator
    )
    {
        try
        {
            CodeMatcher matcher = new(instructions, generator);
            CodeMatch matchLdloc = new(inst => inst.IsLdloc());

            matcher
                .MatchStartForward(
                    [
                        new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ArgUtility), nameof(ArgUtility.Get))),
                        matchLdloc,
                        matchLdloc,
                        new(OpCodes.Ldc_I4_1),
                        new(OpCodes.Add),
                        new(OpCodes.Ldc_I4_1),
                        new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ArgUtility), nameof(ArgUtility.GetInt))),
                    ]
                )
                .ThrowIfNotMatch("Failed to find ArgUtility.Get/ArgUtility.GetInt block")
                .Advance(1)
                .Insert(
                    [
                        new(
                            OpCodes.Call,
                            AccessTools.DeclaredMethod(typeof(Patches), nameof(CheckDeconstructorQualifiedItemId))
                        ),
                    ]
                );

            return matcher.Instructions();
        }
        catch (Exception)
        {
            ModEntry.Log($"Error in Object_OutputDeconstructor_Transpiler, give up on this non-vital patch.");
            return instructions;
        }
    }
}
