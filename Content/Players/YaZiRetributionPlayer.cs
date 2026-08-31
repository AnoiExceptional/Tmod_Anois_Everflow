using everflow.Content.Buffs;
using everflow.Content.NPCs.Bosses;
using everflow.Content.Projectiles;
using Terraria;
using Terraria.ModLoader;

namespace everflow.Content.Players
{
    public sealed class YaZiRetributionPlayer : ModPlayer
    {
        public const int MaximumStacks = 10;
        public const float DamageIncreasePerStack = 0.10f;
        private const int StackGainCooldown = 60;
        private int stackCooldown;

        public int Stacks { get; private set; }

        public override void PostUpdate()
        {
            if (stackCooldown > 0)
                stackCooldown--;
        }

        public override void UpdateDead() => ClearStacks();

        public override void OnHitNPCWithItem(
            Item item,
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            TryGainStack(target);
        }

        public override void OnHitNPCWithProj(
            Projectile proj,
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            TryGainStack(target);
        }

        public override void ModifyHitByNPC(
            NPC npc,
            ref Player.HurtModifiers modifiers)
        {
            if (Stacks > 0 && IsYaZiDamageSource(npc))
                modifiers.FinalDamage *= 1f + Stacks * DamageIncreasePerStack;
        }

        public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
        {
            if (Stacks > 0 && IsYaZiDamageSource(npc))
                ClearStacks();
        }

        public override void ModifyHitByProjectile(
            Projectile proj,
            ref Player.HurtModifiers modifiers)
        {
            if (Stacks > 0 && proj.ModProjectile is YaZiSword)
                modifiers.FinalDamage *= 1f + Stacks * DamageIncreasePerStack;
        }

        public override void OnHitByProjectile(
            Projectile proj,
            Player.HurtInfo hurtInfo)
        {
            if (Stacks > 0 && proj.ModProjectile is YaZiSword)
                ClearStacks();
        }

        public void ClearStacks()
        {
            if (Stacks == 0)
                return;

            Stacks = 0;
            Player.ClearBuff(ModContent.BuffType<YaZiDebuff>());
        }

        public static void ClearAllPlayers()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active)
                    player.GetModPlayer<YaZiRetributionPlayer>().ClearStacks();
            }
        }

        private void TryGainStack(NPC target)
        {
            if (stackCooldown > 0 || !IsYaZiDamageSource(target))
                return;

            Stacks = System.Math.Min(Stacks + 1, MaximumStacks);
            stackCooldown = StackGainCooldown;
            Player.AddBuff(ModContent.BuffType<YaZiDebuff>(), 18000);
        }

        private static bool IsYaZiDamageSource(NPC npc) =>
            npc.ModNPC is YaZi || npc.ModNPC is YaZiSegment;
    }
}
