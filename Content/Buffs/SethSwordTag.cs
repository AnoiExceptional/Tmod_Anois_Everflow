using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Buffs
{
    public sealed class SethSwordTag : ModBuff
    {
        public const int TagDamage = 25;
        public const float BondedCritChance = 0.15f;

        public override string Texture =>
            "everflow/Content/Buffs/AlloyChainswordTag";

        public override void SetStaticDefaults()
        {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }

    public sealed class SethSwordTagNPC : GlobalNPC
    {
        public override void ModifyHitByProjectile(
            NPC npc,
            Projectile projectile,
            ref NPC.HitModifiers modifiers)
        {
            if (projectile.npcProj || projectile.trap ||
                !projectile.IsMinionOrSentryRelated ||
                !npc.HasBuff<SethSwordTag>())
            {
                return;
            }

            float tagMultiplier =
                ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type];
            modifiers.FlatBonusDamage += SethSwordTag.TagDamage * tagMultiplier;

            if (Main.rand.NextFloat() < SethSwordTag.BondedCritChance)
                modifiers.SetCrit();
        }
    }
}
