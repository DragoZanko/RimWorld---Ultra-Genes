using RimWorld;
using Verse;
using Verse.Sound;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace DragoZanko.Ultra.Genes
{
    public class CompProperties_ElectricShock : CompProperties_AbilityEffect
    {
        public HediffDef hediffDef;
        public HediffDef failHediffDef;
        public int durationTicks;
        public int maxDurationTicks;
        public int failDurationTicks;
        public int maxFailDurationTicks;
        public float failChance;

        public CompProperties_ElectricShock()
        {
            this.compClass = typeof(CompAbility_ElectricShock);
        }
    }

    public class CompAbility_ElectricShock : CompAbilityEffect
    {
        private new CompProperties_ElectricShock Props => (CompProperties_ElectricShock)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn victim = target.Pawn;
            if (victim == null) return;

            SoundDef sound = SoundDef.Named("ElectricThighs_ActivationSound");
            if (sound != null)
            {
                sound.PlayOneShot(new TargetInfo(victim.Position, victim.Map));
            }

            float resistance = victim.GetStatValue(StatDefOf.ToxicResistance);
            float multiplier = Mathf.Max(0f, 1f - resistance);

            if (Rand.Value < Props.failChance)
            {
                int finalDuration = Mathf.RoundToInt(Props.failDurationTicks * multiplier);
                ApplyEffect(victim, Props.failHediffDef, finalDuration, Props.maxFailDurationTicks, true);
            }
            else
            {
                int finalDuration = Mathf.RoundToInt(Props.durationTicks * multiplier);
                ApplyEffect(victim, Props.hediffDef, finalDuration, Props.maxDurationTicks, false);
            }
        }

        private void ApplyEffect(Pawn victim, HediffDef def, int addTicks, int maxTicks, bool isLeg)
        {
            if (addTicks <= 0) return;

            BodyPartRecord part = isLeg ? victim.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.MovingLimbCore).RandomElementWithFallback() : null;
            
            Hediff existing = victim.health.hediffSet.hediffs.FirstOrDefault(h => h.def == def && h.Part == part);

            if (existing != null)
            {
                var comp = existing.TryGetComp<HediffComp_Disappears>();
                if (comp != null)
                {
                    comp.ticksToDisappear = Mathf.Min(comp.ticksToDisappear + addTicks, maxTicks);
                }
            }
            else
            {
                Hediff newHediff = HediffMaker.MakeHediff(def, victim, part);
                victim.health.AddHediff(newHediff, part);
                var comp = newHediff.TryGetComp<HediffComp_Disappears>();
                if (comp != null) comp.ticksToDisappear = addTicks;
            }
        }
    }
}