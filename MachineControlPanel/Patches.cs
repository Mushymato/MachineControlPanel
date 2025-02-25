using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace MachineControlPanel;

internal static class Patches
{
    internal static void Patch()
    {
        Harmony harmony = new(ModEntry.ModId);
        try
        {
            harmony.Patch(
                original: AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.OutputDeconstructor)),
                transpiler: new HarmonyMethod(
                    AccessTools.Method(typeof(Patches), nameof(Object_OutputDeconstructor_Transpiler)),
                    priority: Priority.VeryLow
                )
            );
        }
        catch (Exception err)
        {
            ModEntry.Log($"Failed to patch MachineControlPanel:\n{err}", LogLevel.Error);
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

            CodeMatch matchLdloc = new(OpCodes.Ldloc);
            matchLdloc.opcodes.Add(OpCodes.Ldloc_0);
            matchLdloc.opcodes.Add(OpCodes.Ldloc_1);
            matchLdloc.opcodes.Add(OpCodes.Ldloc_2);
            matchLdloc.opcodes.Add(OpCodes.Ldloc_3);
            matchLdloc.opcodes.Add(OpCodes.Ldloc_S);

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

            // IL_0089: ldc.i4.1
            // IL_008a: call string StardewValley.ArgUtility::Get(string[], int32, string, bool)
            // IL_008f: ldloc.3
            // IL_0090: ldloc.s 4
            // IL_0092: ldc.i4.1
            // IL_0093: add
            // IL_0094: ldc.i4.1
            // IL_0095: call int32 StardewValley.ArgUtility::GetInt(string[], int32, int32)
            // IL_009a: stloc.s 5
            // IL_009c: ldloc.s 5
            // IL_009e: ldc.i4.0
            // IL_009f: ldc.i4.m1
            // IL_00a0: ldc.i4.0
            // IL_00a1: newobj instance void StardewValley.Object::.ctor(string, int32, bool, int32, int32)

            return matcher.Instructions();
        }
        catch (Exception)
        {
            ModEntry.Log($"Error in Object_OutputDeconstructor_Transpiler, give up on this non-vital patch.");
            return instructions;
        }
    }
}
