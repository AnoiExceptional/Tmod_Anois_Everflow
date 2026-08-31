using everflow.Content.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    /// <summary>
    /// 与星尘守卫相同，作为独立常驻投射物存在，不属于玩家绘制层。
    /// </summary>
    public sealed class AquaVirtueEffectProjectile : ModProjectile
    {
        private const int FrameSize = 96;

        public override string Texture => "everflow/Content/Projectiles/AquaVirtue_EFX";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 400;
        }

        public override void SetDefaults()
        {
            Projectile.width = FrameSize;
            Projectile.height = FrameSize;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.netImportant = true;
            Projectile.hide = true;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            AquaVirtuePlayer virtuePlayer = player.GetModPlayer<AquaVirtuePlayer>();
            if (!player.active || player.dead || !virtuePlayer.Equipped || !virtuePlayer.EffectVisible)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.Center = virtuePlayer.EffectWorldPosition;
            Projectile.frame = virtuePlayer.EffectAnimationFrame;
            Projectile.netUpdate = Projectile.owner == Main.myPlayer && Main.GameUpdateCount % 15 == 0;
        }

        public override void DrawBehind(
            int index,
            List<int> behindNPCsAndTiles,
            List<int> behindNPCs,
            List<int> behindProjectiles,
            List<int> overPlayers,
            List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player player = Main.player[Projectile.owner];
            AquaVirtuePlayer virtuePlayer = player.GetModPlayer<AquaVirtuePlayer>();
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Rectangle source = new(0, Projectile.frame * FrameSize, FrameSize, FrameSize);
            Vector2 position = Projectile.Center
                + new Vector2(0f, player.gfxOffY)
                - Main.screenPosition;

            // 照明宠物槽使用的 cPet 次级着色器需要在 Immediate 批次中应用。
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise,
                null,
                Main.GameViewMatrix.TransformationMatrix);
            GameShaders.Armor.ApplySecondary(player.cPet, player, null);
            Main.EntitySpriteDraw(
                texture,
                position,
                source,
                new Color(208, 255, 234) * virtuePlayer.EffectOpacity,
                0f,
                new Vector2(FrameSize * 0.5f),
                2f,
                SpriteEffects.None);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise,
                null,
                Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
