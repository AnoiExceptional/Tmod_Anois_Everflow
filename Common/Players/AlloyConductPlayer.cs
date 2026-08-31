using Terraria;
using Terraria.ModLoader;
using everflow.Content.Buffs;

namespace everflow.Common.Players
{
    public class AlloyConductPlayer : ModPlayer
    {
        public int ReservedMinionSlots { get; set; }

        public override void PostUpdateBuffs()
        {
            if (!Player.HasBuff<AlloyConduct>())
                ReservedMinionSlots = 0;
        }
    }
}
