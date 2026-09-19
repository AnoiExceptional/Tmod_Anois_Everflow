using everflow.Content.Mounts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Common.Players
{
    /// <summary>
    /// Abilities shared by every war vehicle: a high-speed contact attack and
    /// an always-on camera extension when the driver has a scope accessory.
    /// </summary>
    public sealed class WarVehicleAbilityPlayer : ModPlayer
    {
        private const float UnicornRamBaseDamage = 120f;
        private const float UnicornRamKnockback = 10f;
        // Terraria's Unicorn reaches roughly 61 mph at 12 pixels per tick.
        // 25 mph therefore corresponds to about 4.9 pixels per tick.
        private const float UnicornTopSpeed = 12f;
        private const float RamMinimumSpeed = 4.9f;
        private const int RamHitCooldown = 10;
        private const int RamInvulnerabilityFrames = 6;

        private readonly int[] ramNpcCooldowns = new int[Main.maxNPCs];
        private Vector2 currentScopeOffset;

        public override void PostUpdate()
        {
            for (int i = 0; i < ramNpcCooldowns.Length; i++)
            {
                if (ramNpcCooldowns[i] > 0)
                    ramNpcCooldowns[i]--;
            }

            if (!IsWarVehicle(Player) ||
                Player.whoAmI != Main.myPlayer ||
                System.Math.Abs(Player.velocity.X) < RamMinimumSpeed)
            {
                return;
            }

            Rectangle ramHitbox = Player.Hitbox;
            int direction = Player.velocity.X >= 0f ? 1 : -1;
            float speedDamageScale = System.Math.Abs(Player.velocity.X) /
                UnicornTopSpeed;
            int damage = System.Math.Max(1,
                (int)Player.GetTotalDamage(DamageClass.Summon)
                    .ApplyTo(UnicornRamBaseDamage * speedDamageScale));
            float knockback = Player.GetTotalKnockback(DamageClass.Summon)
                .ApplyTo(UnicornRamKnockback);

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (ramNpcCooldowns[i] > 0 || !npc.active || npc.friendly ||
                    npc.dontTakeDamage || npc.lifeMax <= 5 ||
                    !ramHitbox.Intersects(npc.Hitbox))
                {
                    continue;
                }

                Player.ApplyDamageToNPC(
                    npc,
                    damage,
                    knockback,
                    direction,
                    false,
                    DamageClass.Summon,
                    false);
                ramNpcCooldowns[i] = RamHitCooldown;

                // Match the Unicorn mount's brief safety window after a ram.
                Player.immune = true;
                Player.immuneTime = System.Math.Max(
                    Player.immuneTime,
                    RamInvulnerabilityFrames);
            }
        }

        public override void ModifyScreenPosition()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            bool scopeActive = Player.active && !Player.dead &&
                HasScopeAccessoryEquipped(Player) &&
                IsWarVehicle(Player) &&
                !Main.gameMenu && !Main.mapFullscreen;

            Vector2 targetOffset = Vector2.Zero;
            if (scopeActive)
            {
                Vector2 screenCenter = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
                Vector2 clampedMouse = new(
                    MathHelper.Clamp(Main.mouseX, 0f, Main.screenWidth),
                    MathHelper.Clamp(Main.mouseY, 0f, Main.screenHeight));
                targetOffset = (clampedMouse - screenCenter) * 0.5f;
            }

            currentScopeOffset = Vector2.Lerp(
                currentScopeOffset,
                targetOffset,
                scopeActive ? 0.12f : 0.2f);

            if (currentScopeOffset.LengthSquared() < 0.01f)
                currentScopeOffset = Vector2.Zero;

            Main.screenPosition += currentScopeOffset;
        }

        private static bool HasScopeAccessoryEquipped(Player player)
        {
            // scope保留对模组瞄准镜的兼容；直接扫描3..9号功能饰品槽，
            // 避免战争载具的noItems状态令三个原版瞄准镜标记失效。
            if (player.scope)
                return true;

            int lastAccessorySlot = System.Math.Min(9, player.armor.Length - 1);
            for (int slot = 3; slot <= lastAccessorySlot; slot++)
            {
                int itemType = player.armor[slot].type;
                if (itemType == ItemID.RifleScope ||
                    itemType == ItemID.SniperScope ||
                    itemType == ItemID.ReconScope)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsWarVehicle(Player player)
        {
            if (!player.mount.Active)
                return false;

            int mountType = player.mount.Type;
            return mountType == ModContent.MountType<BMPT72>() ||
                mountType == ModContent.MountType<AlloyTank02>();
        }
    }
}
