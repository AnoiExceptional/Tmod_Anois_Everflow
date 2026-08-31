using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class AquaVirtueFallingStar : ModProjectile
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.StarCloakStar}";

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.StarCloakStar);
            AIType = ProjectileID.StarCloakStar;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Use the vanilla Mana Cloak pickup so the landed star restores 50 mana.
                int itemIndex = Item.NewItem(Projectile.GetSource_Death(), Projectile.Hitbox, ItemID.ManaCloakStar);
                Main.item[itemIndex].noGrabDelay = 0;
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncItem, number: itemIndex);
            }

            Projectile.Kill();
            return false;
        }
    }
}
