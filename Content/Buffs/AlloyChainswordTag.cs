using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Buffs
{
    public class AlloyChainswordTag : ModBuff
    {
        public const int TagDamage = 2;

        public override void SetStaticDefaults()
        {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }

    public class AlloyChainswordTagNPC : GlobalNPC
    {
        public override void ModifyHitByProjectile(
            NPC npc,
            Projectile projectile,
            ref NPC.HitModifiers modifiers)
        {
            if (projectile.npcProj || projectile.trap || !projectile.IsMinionOrSentryRelated)
                return;

            if (npc.HasBuff<AlloyChainswordTag>())
            {
                float tagMultiplier =
                    ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type];

                modifiers.FlatBonusDamage +=
                    AlloyChainswordTag.TagDamage * tagMultiplier;
            }
        }
    }
}
