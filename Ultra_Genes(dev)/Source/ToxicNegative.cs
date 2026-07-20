using HarmonyLib;
using Verse;
using RimWorld;

namespace TuModResistenciaNegativa
{
    [StaticConstructorOnStartup]
    public static class ModIniciador
    {
        static ModIniciador()
        {
            var harmony = new Harmony("com.tunametag.resistencianegativa");
            harmony.PatchAll();

            if (StatDefOf.ToxicEnvironmentResistance != null)
            {
                StatDefOf.ToxicEnvironmentResistance.minValue = -2.0f;
            }
        }
    }
}