using everflow.Content.Items.Materials;
using everflow.Content.Buffs;
using everflow.Common.DamageClasses;
using everflow.Common.Players;
using everflow.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items
{
    public sealed class WyrmBladeGreatsword : ModItem
    {
        private const int ChargeTime = 120;

        // _1 为68x68x4纵向图集，常态使用第一帧；_2 为90x90强化形态。
        // 主贴图本身已经没有旧图集的透明占位，因此直接使用原版真近战判定。
        public override string Texture =>
            "everflow/Content/Items/WyrmBladeGreatsword_1";

        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(
                Type,
                new DrawAnimationVertical(int.MaxValue, 4));
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 68;
            Item.height = 68;
            Item.damage = 100;
            Item.DamageType = DamageClass.Melee;
            Item.crit = 4;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.knockBack = 6f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Lime;
            Item.shoot = ModContent.ProjectileType<WyrmBladeGreatswordProjectile1>();
            Item.shootSpeed = 12f;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                if (player.GetModPlayer<WyrmBladeInputPlayer>().RightReleaseRequired)
                    return false;

                int chargeType = ModContent.ProjectileType<WyrmBladeCharge>();
                if (player.ownedProjectileCounts[chargeType] > 0 ||
                    !player.CheckMana(50, false))
                {
                    return false;
                }

                Item.useTime = ChargeTime;
                Item.useAnimation = ChargeTime;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.UseSound = null;
                Item.channel = true;
                Item.noMelee = true;
                Item.noUseGraphic = true;
                Item.shoot = chargeType;
                Item.shootSpeed = 0f;
            }
            else
            {
                ApplyNormalUse();
            }

            return true;
        }

        public override void HoldItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<WyrmBladeCharge>()] == 0 &&
                player.itemAnimation <= 0)
            {
                ApplyNormalUse();
            }
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (player.whoAmI == Main.myPlayer)
                player.ChangeDir(Main.MouseWorld.X >= player.Center.X ? 1 : -1);
        }

        public override void ModifyShootStats(
            Player player,
            ref Vector2 position,
            ref Vector2 velocity,
            ref int type,
            ref int damage,
            ref float knockback)
        {
            // 只削减常态左键剑气；右键变形用的蓄力射弹不受影响。
            if (type == ModContent.ProjectileType<WyrmBladeGreatswordProjectile1>())
                damage = (int)(damage * 0.5f);
        }

        private void ApplyNormalUse()
        {
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.channel = false;
            Item.noMelee = false;
            Item.noUseGraphic = false;
            Item.shoot = ModContent.ProjectileType<WyrmBladeGreatswordProjectile1>();
            Item.shootSpeed = 12f;
        }

        internal static void EnterEmpoweredForm(Player player)
        {
            Item heldItem = player.HeldItem;
            if (heldItem.type != ModContent.ItemType<WyrmBladeGreatsword>())
                return;

            bool favorited = heldItem.favorited;
            int prefix = heldItem.prefix;
            player.AddBuff(
                ModContent.BuffType<TheRuneBladeBuff>(),
                15 * 60);
            player.GetModPlayer<WyrmBladeInputPlayer>().RequireRightRelease();
            heldItem.SetDefaults(ModContent.ItemType<WyrmBladeGreatswordEmpowered>());
            if (heldItem.ModItem is WyrmBladeGreatswordEmpowered empowered)
                empowered.RequireRightRelease();
            if (prefix > 0)
                heldItem.Prefix(prefix);
            heldItem.favorited = favorited;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(
                    MessageID.SyncEquipment,
                    number: player.whoAmI,
                    number2: player.selectedItem);
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<WyrmBladeBar>(15)
                .AddIngredient(ItemID.SoulofLight, 10)
                .AddIngredient(ItemID.SoulofNight, 10)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public sealed class WyrmBladeGreatswordEmpowered : ModItem
    {
        private const int SpinUseTime = 60;
        private const int SpinChargeTime = 30;
        private const int SpinSlashTime = 15;
        private const float BladeLength = 90f * 1.41421356f;
        private const float BladeCollisionHalfWidth = 12f;
        private const float SpinBladeReach = 290f;
        private const float SpinCollisionHalfWidth = 68f;
        private const float BladeVisualAngleOffset = MathHelper.Pi * 2f / 9f;
        private bool spinAttackActive;
        private int spinFacingDirection = 1;
        private bool spinSlashSoundPlayed;
        internal void RequireRightRelease()
        {
            spinAttackActive = false;
        }

        public override string Texture =>
            "everflow/Content/Items/WyrmBladeGreatsword_2";

        public override void SetStaticDefaults()
        {
            // 强化形态是独立物品Type，不会继承普通形态的右键登记。
            // 这里只开放右键输入；实际行为由本类CanUseItem锁定为旋转斩击，
            // 不会再次调用普通形态的变形充能逻辑。
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 90;
            Item.height = 90;
            Item.damage = 100;
            Item.DamageType = ModContent.GetInstance<MagicalMeleeDamageClass>();
            Item.crit = 4;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.mana = 5;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.knockBack = 6f;
            Item.UseSound = SoundID.Item71;
            Item.rare = ItemRarityID.Lime;
            Item.shoot = ModContent.ProjectileType<WyrmBladeGreatswordSwingTrail>();
            Item.shootSpeed = 12f;
        }

        public override bool AltFunctionUse(Player player) => true;

        private float SpinAttackSpeedScale(Player player) =>
            System.MathF.Max(0.1f, player.GetTotalAttackSpeed(Item.DamageType));

        // 强化右键固定为60帧。用最终攻速反向补偿原版对物品计时的
        // 缩短，但攻速倍率仍会用于下方的剑身、剑光及命中范围。
        public override float UseAnimationMultiplier(Player player) =>
            spinAttackActive ? SpinAttackSpeedScale(player) : 1f;

        public override float UseTimeMultiplier(Player player) =>
            spinAttackActive ? SpinAttackSpeedScale(player) : 1f;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                // 普通形态刚转换为强化形态时，原先那次右键通常仍处于按住状态。
                // 必须先观察到一次松手，才允许开始新的旋斩并消耗20魔力。
                if (player.GetModPlayer<WyrmBladeInputPlayer>().RightReleaseRequired)
                    return false;

                spinAttackActive = true;
                spinSlashSoundPlayed = false;
                spinFacingDirection = player.whoAmI == Main.myPlayer
                    ? (Main.MouseWorld.X >= player.Center.X ? 1 : -1)
                    : player.direction;

                Item.damage = 200;
                Item.crit = 15;
                Item.useTime = SpinUseTime;
                Item.useAnimation = SpinUseTime;
                Item.mana = 20;
                Item.useTurn = false;
                // 蓄力阶段保持静音；进入旋斩阶段时再单独播放斩击音效。
                Item.UseSound = null;
            }
            else
            {
                ApplyLeftClickStats();
            }

            return true;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (!spinAttackActive)
            {
                if (player.whoAmI == Main.myPlayer)
                    player.ChangeDir(Main.MouseWorld.X >= player.Center.X ? 1 : -1);
                return;
            }

            player.ChangeDir(spinFacingDirection);
            int elapsed = SpinUseTime - player.itemAnimation;
            float worldAngle = -MathHelper.PiOver2;

            if (elapsed >= SpinChargeTime)
            {
                if (!spinSlashSoundPlayed)
                {
                    spinSlashSoundPlayed = true;
                    if (Main.netMode != NetmodeID.Server)
                    {
                        SoundEngine.PlaySound(
                            SoundID.Item71 with
                            {
                                Pitch = 0.25f,
                                PitchVariance = 0f,
                                Volume = SoundID.Item71.Volume * 1.5f
                            },
                            player.Center);
                    }
                }

                int slashElapsed = System.Math.Min(
                    elapsed - SpinChargeTime,
                    SpinSlashTime - 1);
                float spinProgress = MathHelper.Clamp(
                    slashElapsed / (float)(SpinSlashTime - 1),
                    0f,
                    1f);
                worldAngle += MathHelper.TwoPi * spinProgress * spinFacingDirection;
            }

            // itemRotation在朝左时采用镜像坐标；转换后两侧都会从正上方
            // 蓄力，并在后45帧完成方向一致的一整圈旋斩。
            player.itemRotation = MathHelper.WrapAngle(
                worldAngle + (spinFacingDirection == -1 ? -MathHelper.Pi : 0f));
        }

        public override bool? CanHitNPC(Player player, NPC target)
        {
            if (!spinAttackActive)
                return null;

            int elapsed = SpinUseTime - player.itemAnimation;
            bool inSlash = elapsed >= SpinChargeTime &&
                elapsed < SpinChargeTime + SpinSlashTime;
            return inSlash ? null : false;
        }

        public override bool Shoot(
            Player player,
            EntitySource_ItemUse_WithAmmo source,
            Vector2 position,
            Vector2 velocity,
            int type,
            int damage,
            float knockback)
        {
            // 一次挥舞只允许存在一道剑光。自动挥舞开始下一击时先清除
            // 上一击的视觉射弹，杜绝多道FinalFractal互相叠亮。
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active &&
                    projectile.owner == player.whoAmI &&
                    projectile.type == type)
                {
                    projectile.Kill();
                }
            }

            int projectileIndex = Projectile.NewProjectile(
                source,
                position,
                Vector2.Zero,
                type,
                0,
                0f,
                player.whoAmI,
                spinAttackActive ? 1f : 0f,
                spinAttackActive ? SpinAttackSpeedScale(player) : 1f);

            if (projectileIndex >= 0 && projectileIndex < Main.maxProjectiles)
                Main.projectile[projectileIndex].timeLeft = spinAttackActive ? SpinUseTime : 20;

            // 强化右键只生成旋斩剑光；强化左键每次挥舞额外发射一枚剑气。
            if (!spinAttackActive)
            {
                Projectile.NewProjectile(
                    source,
                    position,
                    velocity,
                    ModContent.ProjectileType<WyrmBladeGreatswordProjectile2>(),
                    (int)(damage * 0.8f),
                    knockback,
                    player.whoAmI);
            }
            return false;
        }

        public override void HoldItem(Player player)
        {
            if (player.itemAnimation <= 0)
                ApplyLeftClickStats();
        }

        private void ApplyLeftClickStats()
        {
            spinAttackActive = false;
            Item.damage = 100;
            Item.crit = 4;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.mana = 5;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item71;
        }

        public override bool ModifyItemDraw(
            ref PlayerDrawSet drawInfo,
            ref DrawData drawData,
            ref DrawData? coloredDrawData,
            ref DrawData? glowMaskDrawData)
        {
            if (IsSpinSlash(drawInfo.drawPlayer))
            {
                // 保持原版已经算好的绘制位置与握持原点不变，只放大Scale，
                // 因而2倍剑身仍以剑柄为锚点，不会从玩家手中漂移。
                drawData.scale *= 2f * SpinAttackSpeedScale(drawInfo.drawPlayer);
            }

            Texture2D glowTexture = ModContent.Request<Texture2D>(
                "everflow/Content/Projectiles/WyrmBladeGreatsword_Glow").Value;

            // 复用原版持握贴图已经算好的位置、旋转、原点、缩放与翻转，
            // 让90x90发光遮罩始终与真实剑身完全重合且不受环境光影响。
            glowMaskDrawData = new DrawData(
                glowTexture,
                drawData.position,
                drawData.sourceRect,
                Color.White,
                drawData.rotation,
                drawData.origin,
                drawData.scale,
                drawData.effect,
                0f)
            {
                shader = drawData.shader,
                ignorePlayerRotation = drawData.ignorePlayerRotation
            };

            return true;
        }

        private bool IsSpinSlash(Player player)
        {
            if (!spinAttackActive || player.itemAnimation <= 0)
                return false;

            int elapsed = SpinUseTime - player.itemAnimation;
            return elapsed >= SpinChargeTime &&
                elapsed < SpinChargeTime + SpinSlashTime;
        }

        public override void UseItemHitbox(
            Player player,
            ref Rectangle hitbox,
            ref bool noHitbox)
        {
            if (!IsSpinSlash(player))
                return;

            // 先用覆盖整圈的方框保证外缘NPC能进入原版近战候选列表，
            // 最终是否命中仍由下方的精确剑刃线段检测决定。
            Vector2 handle = player.RotatedRelativePoint(player.MountedCenter);
            float attackSpeedScale = SpinAttackSpeedScale(player);
            int radius = (int)System.MathF.Ceiling(
                (SpinBladeReach + SpinCollisionHalfWidth) * attackSpeedScale);
            hitbox = new Rectangle(
                (int)handle.X - radius,
                (int)handle.Y - radius,
                radius * 2,
                radius * 2);
            noHitbox = false;
        }

        public override bool? CanMeleeAttackCollideWithNPC(
            Rectangle meleeAttackHitbox,
            Player player,
            NPC target)
        {
            // 不再使用被放大后偏向剑尖的轴对齐矩形作为最终判定。
            // 对每个NPC分别检测“握柄 -> 剑尖”的完整剑刃线段，原版
            // ItemCheck_MeleeHitNPCs会继续遍历所有NPC，因此没有穿透上限。
            Vector2 bladeDirection = player.itemRotation.ToRotationVector2();
            bladeDirection.X *= player.direction;
            if (player.direction == -1)
                bladeDirection.Y *= -1f;

            bladeDirection = bladeDirection.RotatedBy(
                -BladeVisualAngleOffset * player.direction);

            bool isSpinSlash = IsSpinSlash(player);
            float attackSpeedScale = isSpinSlash
                ? SpinAttackSpeedScale(player)
                : 1f;
            float scale = isSpinSlash ? 2f * attackSpeedScale : 1f;
            Vector2 bladeStart = player.RotatedRelativePoint(player.MountedCenter);
            float bladeReach = isSpinSlash
                ? SpinBladeReach * attackSpeedScale
                : BladeLength * scale;
            float collisionHalfWidth = isSpinSlash
                ? SpinCollisionHalfWidth * attackSpeedScale
                : BladeCollisionHalfWidth * scale;
            Vector2 bladeEnd = bladeStart + bladeDirection * bladeReach;
            float collisionPoint = 0f;

            return Collision.CheckAABBvLineCollision(
                target.Hitbox.TopLeft(),
                target.Hitbox.Size(),
                bladeStart,
                bladeEnd,
                collisionHalfWidth,
                ref collisionPoint);
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            Color effectColor = new Color(255, 213, 65);

            if (IsSpinSlash(player))
            {
                Vector2 bladeDirection = player.itemRotation.ToRotationVector2();
                bladeDirection.X *= player.direction;
                if (player.direction == -1)
                    bladeDirection.Y *= -1f;
                bladeDirection = bladeDirection.RotatedBy(
                    -BladeVisualAngleOffset * player.direction);

                Vector2 handle =
                    player.RotatedRelativePoint(player.MountedCenter);
                float attackSpeedScale = SpinAttackSpeedScale(player);
                float scaledBladeReach = SpinBladeReach * attackSpeedScale;
                Vector2 tangent = bladeDirection.RotatedBy(
                    MathHelper.PiOver2 * spinFacingDirection);

                // 15帧旋转阶段每帧沿整条剑刃大量抛出粒子。速度沿当前
                // 旋转切线，TintableDustLighted自身的阻尼会令其逐渐减速。
                for (int i = 0; i < 10; i++)
                {
                    float radius = Main.rand.NextFloat(
                        30f * attackSpeedScale, scaledBladeReach);
                    Vector2 position = handle + bladeDirection * radius +
                        Main.rand.NextVector2Circular(10f, 10f);
                    float radiusRatio = radius / scaledBladeReach;
                    Vector2 velocity = tangent * Main.rand.NextFloat(
                        2.8f + radiusRatio * 2f,
                        5.2f + radiusRatio * 4f);
                    velocity += bladeDirection * Main.rand.NextFloat(0.2f, 1.1f);
                    velocity += Main.rand.NextVector2Circular(0.7f, 0.7f);

                    Dust dust = Dust.NewDustPerfect(
                        position,
                        DustID.TintableDustLighted,
                        velocity,
                        20,
                        effectColor,
                        Main.rand.NextFloat(0.9f, 1.45f));
                    dust.noGravity = true;
                    dust.fadeIn = Main.rand.NextFloat(0.6f, 1.1f);
                    Lighting.AddLight(
                        position,
                        effectColor.ToVector3() * 0.45f);
                }

                return;
            }

            Lighting.AddLight(
                hitbox.Center.ToVector2(),
                effectColor.ToVector3() * 1.35f);

            // 平均每两个挥舞更新产生1颗粒子，数量保持中等；粒子只取自
            // 实际近战框，跟随咒文剑刃而不是固定在玩家身上。
            if (Main.rand.NextBool())
            {
                Vector2 position = new Vector2(
                    Main.rand.NextFloat(hitbox.Left, hitbox.Right),
                    Main.rand.NextFloat(hitbox.Top, hitbox.Bottom));
                Dust dust = Dust.NewDustPerfect(
                    position,
                    DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(0.8f, 0.8f),
                    60,
                    effectColor,
                    Main.rand.NextFloat(0.7f, 1.05f));
                dust.noGravity = true;
            }
        }

        public override void UpdateInventory(Player player)
        {
            // Buff是强化形态的唯一状态来源。自然结束、手动取消，或读取到
            // 旧存档中的孤立强化物品时，都会立即恢复普通形态。
            if (player.HasBuff(ModContent.BuffType<TheRuneBladeBuff>()))
                return;

            // Buff在右键旋斩期间结束或被手动取消时，先让当前60帧攻击
            // （30帧蓄力、15帧斩击、15帧冷却）完整执行完毕。
            if (spinAttackActive && player.itemAnimation > 0)
                return;

            int inventorySlot = -1;
            for (int i = 0; i < player.inventory.Length; i++)
            {
                if (ReferenceEquals(player.inventory[i], Item))
                {
                    inventorySlot = i;
                    break;
                }
            }

            bool favorited = Item.favorited;
            int prefix = Item.prefix;
            player.GetModPlayer<WyrmBladeInputPlayer>().RequireRightRelease();
            Item.SetDefaults(ModContent.ItemType<WyrmBladeGreatsword>());
            if (prefix > 0)
                Item.Prefix(prefix);
            Item.favorited = favorited;

            if (Main.netMode == NetmodeID.MultiplayerClient && inventorySlot >= 0)
            {
                NetMessage.SendData(
                    MessageID.SyncEquipment,
                    number: player.whoAmI,
                    number2: inventorySlot);
            }
        }
    }
}
