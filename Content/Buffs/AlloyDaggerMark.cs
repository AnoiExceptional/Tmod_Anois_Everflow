using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Projectiles.Minions;

namespace everflow.Content.Buffs
{
    public class AlloyDaggerMark : ModBuff
    {
        public override string Texture =>
            $"Terraria/Images/Buff_{BuffID.PirateMinion}";

        public override void SetStaticDefaults()
        {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }

    public class AlloyDaggerMarkNPC : GlobalNPC
    {
        public override void PostAI(NPC npc)
        {
            if (!npc.HasBuff<AlloyDaggerMark>())
                return;

            int guardType = ModContent.ProjectileType<AlloyGuard>();

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];

                if (player.active &&
                    !player.dead &&
                    player.ownedProjectileCounts[guardType] > 0)
                {
                    player.MinionAttackTargetNPC = npc.whoAmI;
                }
            }
        }

    }
}
