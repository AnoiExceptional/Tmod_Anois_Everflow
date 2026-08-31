using Terraria;
using Terraria.ModLoader;
using everflow.Content.Projectiles.Minions;

namespace everflow.Content.Buffs.Minions
{
    public class AlloyBallBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<AlloyBall>()] > 0)
                player.buffTime[buffIndex] = 18000;
            else
                player.DelBuff(buffIndex--);
        }
    }
}
