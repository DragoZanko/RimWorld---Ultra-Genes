using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DragoZanko.Ultra.Genes
{
    [DefOf]
    public static class UG_PhoenixDefOf
    {
        public static HediffDef UG_PhoenixVigilante;

    }

    public class Gene_Phoenix : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            CheckHediff();
        }

        public override void Tick()
        {
            base.Tick();
            if (pawn.IsHashIntervalTick(600)) CheckHediff();
        }

        public override void PostRemove()
        {
            base.PostRemove();
            if (UG_PhoenixDefOf.UG_PhoenixVigilante != null)
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(UG_PhoenixDefOf.UG_PhoenixVigilante);
                if (hediff != null) pawn.health.RemoveHediff(hediff);
            }
        }

        private void CheckHediff()
        {
            if (UG_PhoenixDefOf.UG_PhoenixVigilante != null && !pawn.health.hediffSet.HasHediff(UG_PhoenixDefOf.UG_PhoenixVigilante))
            {
                pawn.health.AddHediff(UG_PhoenixDefOf.UG_PhoenixVigilante, null, null, null);
            }
        }
    }

    public class Hediff_PhoenixVigilante : HediffWithComps
    {
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (pawn.DevelopmentalStage == DevelopmentalStage.Adult && pawn.Corpse != null)
            {
                Find.World.GetComponent<PhoenixRebirthManager>()?.RegisterRebirth(pawn.Corpse, pawn);
            }
        }
    }

    public class PhoenixRebirthManager : WorldComponent
    {
        private List<RebirthData> pendingRebirths = new List<RebirthData>();

        public PhoenixRebirthManager(World world) : base(world) { }

        public void RegisterRebirth(Corpse corpse, Pawn originalPawn)
        {
            if (corpse == null || originalPawn == null) return;
            if (pendingRebirths.Any(r => r.corpse == corpse)) return;

            RebirthData data = new RebirthData
            {
                corpse = corpse,
                sourcePawn = originalPawn,
                ticksUntilBirth = 22500,
                savedXenotype = originalPawn.genes?.Xenotype,
                savedFaction = originalPawn.Faction,
                savedKind = originalPawn.kindDef
            };

            if (originalPawn.genes != null)
            {
                foreach (Gene gene in originalPawn.genes.Endogenes)
                {
                    if (!data.savedGenes.Contains(gene.def)) data.savedGenes.Add(gene.def);
                }
            }

            if (originalPawn.skills != null)
            {
                foreach (SkillRecord skill in originalPawn.skills.skills)
                    data.savedPassions[skill.def] = skill.passion;
            }

            pendingRebirths.Add(data);
        }

        public override void WorldComponentTick()
        {
            if (pendingRebirths.NullOrEmpty()) return;

            for (int i = pendingRebirths.Count - 1; i >= 0; i--)
            {
                var rebirth = pendingRebirths[i];
                if (rebirth == null || rebirth.corpse == null || rebirth.corpse.Destroyed)
                {
                    pendingRebirths.RemoveAt(i);
                    continue;
                }

                rebirth.ticksUntilBirth--;
                if (rebirth.ticksUntilBirth <= 0)
                {
                    ExecuteBirth(rebirth);
                    pendingRebirths.RemoveAt(i);
                }
            }
        }

        private void ExecuteBirth(RebirthData data)
        {
            Map map = data.corpse.MapHeld;
            IntVec3 position = data.corpse.PositionHeld;
            
            if (map == null) return;

            PawnKindDef kind = data.savedKind ?? PawnKindDefOf.Colonist;

            PawnGenerationRequest request = new PawnGenerationRequest(kind, data.savedFaction)
            {
                ForceGenerateNewPawn = true,
                FixedBiologicalAge = 0f,
                FixedChronologicalAge = 0f,
                ForcedXenotype = data.savedXenotype,
                AllowedDevelopmentalStages = DevelopmentalStage.Newborn,
                AllowDowned = true,
                CanGeneratePawnRelations = false
            };

            Pawn baby = PawnGenerator.GeneratePawn(request);

            if (baby.genes != null)
            {
                List<Gene> toRemove = new List<Gene>(baby.genes.GenesListForReading);
                foreach (Gene g in toRemove) baby.genes.RemoveGene(g);
                foreach (GeneDef gd in data.savedGenes) baby.genes.AddGene(gd, false);
            }

            if (baby.skills != null && data.savedPassions != null)
            {
                foreach (var entry in data.savedPassions)
                {
                    SkillRecord skill = baby.skills.GetSkill(entry.Key);
                    if (skill != null) skill.passion = entry.Value;
                }
            }

            DamageCorpse(data.corpse);
            FilthMaker.TryMakeFilth(position, map, ThingDefOf.Filth_Blood, baby.LabelShort, 3);
            GenSpawn.Spawn(baby, position, map);
            
            if (data.sourcePawn != null)
            {
                if (!baby.relations.DirectRelationExists(PawnRelationDefOf.Parent, data.sourcePawn))
                {
                    baby.relations.AddDirectRelation(PawnRelationDefOf.Parent, data.sourcePawn);
                }
            }

            ChoiceLetter_BabyBirth letter = (ChoiceLetter_BabyBirth)LetterMaker.MakeLetter(LetterDefOf.BabyBirth);
            letter.Label = "Phoenix Rebirth: " + baby.LabelShort;
            letter.Text = "A new phoenix has risen from the remains of " + (data.sourcePawn?.LabelShort ?? "an ancient phoenix") + ".";
            letter.lookTargets = new LookTargets(baby);

            FieldInfo pawnField = typeof(ChoiceLetter_BabyBirth).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (pawnField != null) pawnField.SetValue(letter, baby);

            Find.LetterStack.ReceiveLetter(letter);
        }

        private void DamageCorpse(Corpse corpse)
        {
            List<BodyPartDef> targetDefs = new List<BodyPartDef> { BodyPartDefOf.Heart, BodyPartDefOf.Lung };
            
            string[] extraParts = { "Kidney", "Liver", "Stomach" };
            foreach (var name in extraParts)
            {
                BodyPartDef def = DefDatabase<BodyPartDef>.GetNamedSilentFail(name);
                if (def != null) targetDefs.Add(def);
            }

            var parts = corpse.InnerPawn.health.hediffSet.GetNotMissingParts()
                .Where(p => targetDefs.Contains(p.def)).ToList();

            foreach (BodyPartRecord part in parts)
            {
                corpse.InnerPawn.health.AddHediff(HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, corpse.InnerPawn, part));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pendingRebirths, "pendingRebirths", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && pendingRebirths == null)
                pendingRebirths = new List<RebirthData>();
        }
    }

    public class RebirthData : IExposable
    {
        public Corpse corpse;
        public Pawn sourcePawn;
        public int ticksUntilBirth;
        public XenotypeDef savedXenotype;
        public Faction savedFaction;
        public PawnKindDef savedKind;
        public List<GeneDef> savedGenes = new List<GeneDef>();
        public Dictionary<SkillDef, Passion> savedPassions = new Dictionary<SkillDef, Passion>();

        public void ExposeData()
        {
            Scribe_References.Look(ref corpse, "corpse");
            Scribe_References.Look(ref sourcePawn, "sourcePawn", true); 
            Scribe_Values.Look(ref ticksUntilBirth, "ticksUntilBirth");
            Scribe_Defs.Look(ref savedXenotype, "savedXenotype");
            Scribe_References.Look(ref savedFaction, "savedFaction");
            Scribe_Defs.Look(ref savedKind, "savedKind");
            Scribe_Collections.Look(ref savedGenes, "savedGenes", LookMode.Def);
            
            if (Scribe.mode == LoadSaveMode.LoadingVars && savedPassions == null)
                savedPassions = new Dictionary<SkillDef, Passion>();

            Scribe_Collections.Look(ref savedPassions, "savedPassions", LookMode.Def, LookMode.Value);
        }
    }
}