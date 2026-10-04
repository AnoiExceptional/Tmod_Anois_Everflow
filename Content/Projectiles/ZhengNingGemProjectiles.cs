using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class ZhengNingFallingGemProjectile : ModProjectile
    {
        public const int Diamond = 0;
        public const int Ruby = 1;
        public const int Emerald = 2;
        public const int Sapphire = 3;
        public const int Topaz = 4;
        public const int Amethyst = 5;
        public const int Agate = 6;
        public const int GemTypeCount = 7;

        private bool impactTriggered;
        private int GemType => (int)Projectile.ai[0];
        private int ParentIndex => (int)Projectile.ai[1];
        private bool IsSapphireFragment => Projectile.ai[2] == 1f;
        private bool IsLandingBurst => Projectile.ai[2] == 2f;

        public override string Texture =>
            "Terraria/Images/Projectile_0";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
        }

        public override void AI()
        {
            Projectile.rotation += Projectile.velocity.X * 0.025f + 0.05f;
            Projectile.scale = IsSapphireFragment ? 0.62f : 1f;

            if (IsSapphireFragment)
            {
                Projectile.velocity.Y = System.Math.Min(
                    15f,
                    Projectile.velocity.Y + 0.32f);
            }

            if (IsLandingBurst && GemType != Diamond &&
                !IsSapphireFragment)
            {
                float gravity = 0.22f;
                if (GemType == Agate)
                {
                    gravity *= System.MathF.Pow(
                        1.5f,
                        Projectile.localAI[0]);
                }
                Projectile.velocity.Y = System.Math.Min(
                    18f,
                    Projectile.velocity.Y + gravity);
            }
            else if (GemType == Agate && Projectile.localAI[0] > 0f)
            {
                float gravity = 0.10f * System.MathF.Pow(
                    1.5f,
                    Projectile.localAI[0] - 1f);
                Projectile.velocity.Y = System.Math.Min(
                    18f,
                    Projectile.velocity.Y + gravity);
            }

            if (GemType == Diamond)
            {
                Projectile.tileCollide = false;
                Lighting.AddLight(Projectile.Center, 2.3f, 2.3f, 2.05f);
            }

            if (GemType == Ruby)
                CreateRubyTrail();

            if (GemType == Emerald)
                CreateEmeraldFlicker();

            if (GemType == Emerald)
                TryHealParent();
        }

        private void CreateRubyTrail()
        {
            Lighting.AddLight(Projectile.Center, 0.65f, 0.12f, 0.025f);
            if (Main.dedServ)
                return;

            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity *
                        Main.rand.NextFloat(0.15f, 0.55f) +
                        Main.rand.NextVector2Circular(3f, 3f),
                    DustID.Torch,
                    -Projectile.velocity * Main.rand.NextFloat(0.02f, 0.07f),
                    80,
                    new Color(255, 70, 20),
                    Main.rand.NextFloat(0.9f, 1.35f));
                dust.noGravity = true;
            }
        }

        private void CreateEmeraldFlicker()
        {
            float flicker = 0.65f +
                0.35f * System.MathF.Sin(
                    Main.GameUpdateCount * 0.38f + Projectile.whoAmI);
            Lighting.AddLight(
                Projectile.Center,
                new Vector3(0.08f, 1.15f, 0.28f) * flicker);
            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                    DustID.GemEmerald,
                    Main.rand.NextVector2Circular(0.35f, 0.35f),
                    80,
                    default,
                    Main.rand.NextFloat(0.8f, 1.2f) * flicker);
                dust.noGravity = true;
            }
        }

        private void TryHealParent()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient ||
                ParentIndex < 0 || ParentIndex >= Main.maxNPCs)
            {
                return;
            }

            NPC parent = Main.npc[ParentIndex];
            if (!parent.active ||
                parent.type != ModContent.NPCType<
                    global::everflow.Content.NPCs.Bosses.ZhengNing>() ||
                parent.dontTakeDamage ||
                !Projectile.Hitbox.Intersects(parent.Hitbox))
            {
                return;
            }

            int healing = System.Math.Min(100, parent.lifeMax - parent.life);
            if (healing > 0)
            {
                parent.life += healing;
                parent.HealEffect(healing, true);
                parent.netUpdate = true;
            }
            Projectile.Kill();
        }

        public override void OnHitPlayer(
            Player target,
            Player.HurtInfo info)
        {
            if (GemType == Ruby)
                target.AddBuff(BuffID.OnFire, 5 * 60);

            TriggerImpactEffects();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (GemType == Agate)
            {
                Projectile.localAI[0]++;
                if (Projectile.localAI[0] < 3f)
                {
                    if (Projectile.velocity.X != oldVelocity.X)
                        Projectile.velocity.X = -oldVelocity.X * 0.95f;
                    if (Projectile.velocity.Y != oldVelocity.Y)
                        Projectile.velocity.Y = -oldVelocity.Y * 0.95f;
                    SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
                    Projectile.netUpdate = true;
                    return false;
                }
            }

            TriggerImpactEffects();
            return true;
        }

        private void TriggerImpactEffects()
        {
            if (impactTriggered || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            impactTriggered = true;
            if (GemType == Sapphire && !IsSapphireFragment)
                SpawnSapphireFragments();
            else if (GemType == Topaz)
                SpawnTopazExplosion();
        }

        private void SpawnSapphireFragments()
        {
            for (int i = -1; i <= 1; i++)
            {
                Vector2 velocity = new(
                    i * 3.4f + Main.rand.NextFloat(-0.6f, 0.6f),
                    Main.rand.NextFloat(-6.2f, -4.4f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity,
                    Type,
                    System.Math.Max(1, Projectile.damage / 2),
                    Projectile.knockBack * 0.5f,
                    Main.myPlayer,
                    Sapphire,
                    ParentIndex,
                    1f);
            }
        }

        private void SpawnTopazExplosion()
        {
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<ZhengNingTopazExplosionProjectile>(),
                Projectile.damage,
                Projectile.knockBack,
                Main.myPlayer);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Shatter, Projectile.Center);
            int dustType = GetDustType();
            for (int i = 0; i < (IsSapphireFragment ? 5 : 11); i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    dustType,
                    Main.rand.NextVector2Circular(3.2f, 3.2f),
                    80,
                    default,
                    Main.rand.NextFloat(0.8f, 1.25f) * Projectile.scale);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int itemType = GetGemItemType();
            Main.instance.LoadItem(itemType);
            Texture2D texture = TextureAssets.Item[itemType].Value;
            Vector2 origin = texture.Size() * 0.5f;

            if (GemType == Diamond)
            {
                for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
                {
                    float strength = 1f - i / (float)Projectile.oldPos.Length;
                    Vector2 position = Projectile.oldPos[i] +
                        Projectile.Size * 0.5f - Main.screenPosition;
                    Main.EntitySpriteDraw(
                        texture,
                        position,
                        null,
                        new Color(255, 255, 225, 0) * (0.8f * strength),
                        Projectile.oldRot[i],
                        origin,
                        Projectile.scale * MathHelper.Lerp(0.8f, 1.45f, strength),
                        SpriteEffects.None);
                    Main.EntitySpriteDraw(
                        texture,
                        position,
                        null,
                        Color.White * strength,
                        Projectile.oldRot[i],
                        origin,
                        Projectile.scale * MathHelper.Lerp(0.5f, 1f, strength),
                        SpriteEffects.None);
                }
                lightColor = Color.White;
            }
            else if (GemType == Agate)
            {
                for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
                {
                    float strength = 1f - i / (float)Projectile.oldPos.Length;
                    Vector2 position = Projectile.oldPos[i] +
                        Projectile.Size * 0.5f - Main.screenPosition;
                    Main.EntitySpriteDraw(
                        texture,
                        position,
                        null,
                        new Color(255, 165, 45, 0) * (0.58f * strength),
                        Projectile.oldRot[i],
                        origin,
                        Projectile.scale * MathHelper.Lerp(0.45f, 0.95f, strength),
                        SpriteEffects.None);
                }
            }

            if (GemType == Emerald)
            {
                float flicker = 0.45f + 0.35f * System.MathF.Sin(
                    Main.GameUpdateCount * 0.38f + Projectile.whoAmI);
                for (int i = 0; i < 4; i++)
                {
                    Vector2 offset = (MathHelper.PiOver2 * i)
                        .ToRotationVector2() * 2f;
                    Main.EntitySpriteDraw(
                        texture,
                        Projectile.Center + offset - Main.screenPosition,
                        null,
                        new Color(70, 255, 115, 0) * flicker,
                        Projectile.rotation,
                        origin,
                        Projectile.scale * 1.08f,
                        SpriteEffects.None);
                }
            }

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None);
            return false;
        }

        private int GetGemItemType() => GemType switch
        {
            Diamond => ItemID.Diamond,
            Ruby => ItemID.Ruby,
            Emerald => ItemID.Emerald,
            Sapphire => ItemID.Sapphire,
            Topaz => ItemID.Topaz,
            Amethyst => ItemID.Amethyst,
            // 原版第七种宝石使用琥珀贴图，对应本攻击中的“玛瑙”。
            _ => ItemID.Amber
        };

        private int GetDustType() => GemType switch
        {
            Diamond => DustID.GemDiamond,
            Ruby => DustID.GemRuby,
            Emerald => DustID.GemEmerald,
            Sapphire => DustID.GemSapphire,
            Topaz => DustID.GemTopaz,
            Amethyst => DustID.GemAmethyst,
            _ => DustID.GemAmber
        };
    }

    public sealed class ZhengNingTopazExplosionProjectile : ModProjectile
    {
        public override string Texture =>
            "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 160;
            Projectile.height = 160;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 18;
        }

        public override bool? CanDamage() =>
            Projectile.timeLeft >= 16 ? null : false;

        public override void AI()
        {
            float strength = Projectile.timeLeft / 18f;
            Lighting.AddLight(
                Projectile.Center,
                0.42f * strength,
                0.30f * strength,
                0.035f * strength);

            int age = 18 - Projectile.timeLeft;
            if (age % 2 != 0)
                return;

            float progress = age / 17f;
            const int particlesPerRing = 20;
            for (int ring = 0; ring < 3; ring++)
            {
                float ringProgress = MathHelper.Clamp(
                    progress - ring * 0.10f,
                    0f,
                    1f);
                float radius = MathHelper.Lerp(
                    12f + ring * 6f,
                    80f - ring * 6f,
                    ringProgress);
                float rotationOffset = age * 0.025f + ring * 0.17f;

                for (int i = 0; i < particlesPerRing; i++)
                {
                    float angle = MathHelper.TwoPi * i /
                        particlesPerRing + rotationOffset;
                    Vector2 direction = angle.ToRotationVector2();
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center + direction * radius,
                        DustID.GemTopaz,
                        direction * 0.18f,
                        95,
                        new Color(255, 210, 45),
                        0.8f - ring * 0.08f);
                    dust.noGravity = true;
                    dust.fadeIn = 0.9f;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 圆环完全由环形粒子构成，不绘制任何线段或中心射线。
            return false;
        }
    }
}
