using RimWorld;
using Verse;
using System.Collections.Generic;

namespace DragoZanko.Ultra.Genes
{
    public class CompTargetEffect_IntegrateGenes : CompUseEffect
    {
        public override void DoEffect(Pawn user)
        {
            Pawn target = user; 
            if (target.genes == null) return;

            List<Gene> xenogenes = new List<Gene>(target.genes.Xenogenes);

            if (xenogenes.Count == 0)
            {
                Messages.Message("Pawn has no xenogenes to integrate.", target, MessageTypeDefOf.RejectInput);
                return;
            }

            foreach (Gene gene in xenogenes)
            {
                target.genes.AddGene(gene.def, false);
                target.genes.RemoveGene(gene);
            }

            Messages.Message(target.LabelShort + ": Genes successfully integrated into germline ADN.", target, MessageTypeDefOf.PositiveEvent);
        }
    }
}