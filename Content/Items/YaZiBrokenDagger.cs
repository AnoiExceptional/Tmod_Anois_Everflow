using everflow.Content.NPCs.Bosses;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items
{
    public sealed class YaZiBrokenDagger : ModItem
    {
        private const int TotalAnimationFrames = 60;
        private const int ChargeFrames = 45;
        private bool summonTriggered;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 20;
            // 在下刺命中地面的动画节点手动消耗，避免按键瞬间就失去物品。
            Item.consumable = false;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = TotalAnimationFrames;
            Item.useAnimation = TotalAnimationFrames;
            Item.UseSound = null;
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.sellPrice(gold: 1);
        }

        public override bool CanUseItem(Player player)
        {
            bool canUse =
                !NPC.AnyNPCs(ModContent.NPCType<YaZi>()) &&
                IsStandingOnSolidTile(player);
            if (canUse)
                summonTriggered = false;

            return canUse;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            int elapsed = player.itemAnimationMax - player.itemAnimation;
            Vector2 raisedPosition = player.Center + new Vector2(
                player.direction * 5f,
                -42f);
            Vector2 impactPosition = new(
                player.Center.X + player.direction * 5f,
                player.Bottom.Y - 5f);

            if (elapsed < ChargeFrames)
            {
                // 前20帧举高，此后短暂保持蓄力姿势。
                float raiseProgress = MathHelper.Clamp(elapsed / 20f, 0f, 1f);
                raiseProgress = raiseProgress * raiseProgress * (3f - 2f * raiseProgress);
                Vector2 startingPosition = player.Center + new Vector2(
                    player.direction * 5f,
                    -8f);
                player.itemLocation = Vector2.Lerp(
                    startingPosition,
                    raisedPosition,
                    raiseProgress);
            }
            else
            {
                // 最后15帧快速向脚下刺落，后段进一步加速。
                float thrustProgress = MathHelper.Clamp(
                    (elapsed - ChargeFrames) /
                    (float)(TotalAnimationFrames - ChargeFrames),
                    0f,
                    1f);
                thrustProgress *= thrustProgress;
                player.itemLocation = Vector2.Lerp(
                    raisedPosition,
                    impactPosition,
                    thrustProgress);
            }

            // 贴图原本剑尖朝右上；按角色方向旋转后始终保持剑尖垂直向下。
            player.itemRotation = MathHelper.Pi * 0.75f * player.direction;

            if (!summonTriggered && player.itemAnimation <= 1)
            {
                summonTriggered = true;
                TryCompleteSummon(player);
            }
        }

        private void TryCompleteSummon(Player player)
        {
            if (!IsStandingOnSolidTile(player) ||
                NPC.AnyNPCs(ModContent.NPCType<YaZi>()))
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Dig, player.Bottom);
            SoundEngine.PlaySound(SoundID.Roar, player.Center);

            if (player.whoAmI != Main.myPlayer)
                return;

            Item.stack--;
            if (Item.stack <= 0)
                Item.TurnToAir();

            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<YaZi>());
        }

        private static bool IsStandingOnSolidTile(Player player)
        {
            Rectangle feetProbe = new(
                player.Hitbox.Left + 2,
                player.Hitbox.Bottom,
                System.Math.Max(1, player.Hitbox.Width - 4),
                4);
            return Collision.SolidCollision(
                feetProbe.Location.ToVector2(),
                feetProbe.Width,
                feetProbe.Height);
        }
    }
}
