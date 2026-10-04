using everflow.Common.Items;
using everflow.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Common.Players
{
    public sealed class PhasebladeForceBlockPlayer : ModPlayer
    {
        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer || Player.dead ||
                !Player.controlUseTile ||
                Player.HasBuff(BuffID.ManaSickness) ||
                !PhasebladeForceBlockGlobalItem.TryGetShieldColorIndex(
                    Player.HeldItem.type,
                    out int colorIndex))
            {
                return;
            }

            int shieldType = ModContent.ProjectileType<PhasebladeForceShieldProjectile>();
            if (Player.ownedProjectileCounts[shieldType] > 0 || Player.statMana < 2)
                return;

            Vector2 direction = (Main.MouseWorld - Player.MountedCenter)
                .SafeNormalize(new Vector2(Player.direction, 0f));
            Projectile.NewProjectile(
                Player.GetSource_ItemUse(Player.HeldItem),
                Player.MountedCenter,
                direction,
                shieldType,
                0,
                0f,
                Player.whoAmI,
                colorIndex,
                PhasebladeForceBlockGlobalItem.GetShieldTier(Player.HeldItem.type));
        }
    }
}
