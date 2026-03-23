using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System;

namespace DragoZanko.Ultra.Genes
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("DragoZanko.Ultra.Genes.CorePatch");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(MassUtility), "Capacity")]
    public static class Patch_MassCapacity
    {
        public static void Postfix(Pawn p, ref float __result)
        {
            if (p?.genes == null) return;

            if (p.genes.HasActiveGene(DefDatabase<GeneDef>.GetNamed("UG_DigitigradeGait", false)))
            {
                __result *= 0.8f;
            }
            else if (p.genes.HasActiveGene(DefDatabase<GeneDef>.GetNamed("UG_UnguligradeGait", false)))
            {
                __result *= 1.2f;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "TicksPerMove")]
    public static class Patch_CaravanSpeed
    {
        public static void Postfix(Pawn __instance, ref float __result)
        {
            if (__instance.IsCaravanMember() && __instance.genes != null)
            {
                if (__instance.genes.HasActiveGene(DefDatabase<GeneDef>.GetNamed("UG_UnguligradeGait", false)))
                {
                    __result *= 0.77f;
                }
            }
        }
    }
}