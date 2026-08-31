using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using everflow.Common;
using everflow.Content.Players;
using everflow.Content.Projectiles;

namespace everflow.Content.Items
{
    /// <summary>
    /// Shared behavior for all six forms of Spiral Hell. Each form is a real
    /// item type so its localized name and differently-sized texture remain
    /// correct in inventories, while held, and in multiplayer.
    /// </summary>
    public abstract class SpiralHellForm : ModItem
    {
        // Stored as value fields rather than an array so Terraria's ordinary
        // item cloning cannot accidentally make two weapons share one array.
        private int rotatioPrefix;
        private int impactoPrefix;
        private int equilibrioPrefix;
        private int prosperitoPrefix;
        private int masqueradoPrefix;
        private int barricadoPrefix;

        public readonly struct FormStats
        {
            public readonly bool Unlocked;
            public readonly int Damage;
            public readonly int UseTime;
            public readonly int Mana;
            public readonly float Knockback;
            public readonly int ProjectileCount;
            public readonly float ShootSpeed;
            public readonly int Penetration;
            public readonly int ExplosionSize;
            public readonly int ChargeTime;
            public readonly int StarDamage;
            public readonly int ArmorPenetration;

            public FormStats(bool unlocked, int damage = 0, int useTime = 20,
                int mana = 0, float knockback = 1f, int projectileCount = 1,
                float shootSpeed = 0f, int penetration = 0, int explosionSize = 0,
                int chargeTime = 120, int starDamage = 0, int armorPenetration = 0)
            {
                Unlocked = unlocked;
                Damage = damage;
                UseTime = useTime;
                Mana = mana;
                Knockback = knockback;
                ProjectileCount = projectileCount;
                ShootSpeed = shootSpeed;
                Penetration = penetration;
                ExplosionSize = explosionSize;
                ChargeTime = chargeTime;
                StarDamage = starDamage;
                ArmorPenetration = armorPenetration;
            }
        }

        protected abstract int FormNumber { get; }
        protected abstract int NextFormType { get; }
        protected abstract int TextureWidth { get; }
        protected abstract int TextureHeight { get; }
        protected abstract float MuzzleDistance { get; }
        protected virtual float MuzzleHeight => -2f;
        protected virtual Vector2 GripOffset => new Vector2(-6f, 0f);
        protected FormStats CurrentStats => GetFormStats(FormNumber);
        protected virtual int LeftDamage => CurrentStats.Damage;
        protected virtual int LeftMana => CurrentStats.Mana;
        protected virtual int LeftUseTime => CurrentStats.UseTime;
        protected virtual int LeftProjectileType => ModContent.ProjectileType<SpiralHellProjectile1>();
        protected virtual float LeftShootSpeed => CurrentStats.ShootSpeed;
        protected virtual SoundStyle? LeftUseSound => SoundID.Item41 with { Volume = 0.3f };
        protected virtual bool LeftChannel => false;
        protected virtual int LeftUseStyle => ItemUseStyleID.Shoot;
        protected virtual bool LeftNoMelee => true;

        public override void SetDefaults()
        {
            Item.width = TextureWidth;
            Item.height = TextureHeight;
            Item.damage = LeftDamage;
            Item.DamageType = DamageClass.Magic;
            Item.knockBack = CurrentStats.Knockback;
            Item.useStyle = LeftUseStyle;
            Item.useTime = LeftUseTime;
            Item.useAnimation = LeftUseTime;
            Item.mana = LeftMana;
            Item.noMelee = LeftNoMelee;
            Item.autoReuse = true;
            Item.channel = LeftChannel;
            Item.UseSound = LeftUseSound;
            Item.shoot = LeftProjectileType;
            Item.shootSpeed = LeftShootSpeed;
            Item.rare = GetStageRarity();
            Item.value = Item.sellPrice(gold: 1);
        }

        // All six forms are magic weapons, including Barricado despite its
        // melee-style swing animation. Explicitly use the magic prefix pool so
        // Mythical is their shared best modifier and remains valid on switching.
        public override bool MagicPrefix() => true;

        public static int GetProgressionStage() => DragonsEdge.GetProgressionStage();

        private static int GetStageRarity() => GetProgressionStage() switch
        {
            1 => ItemRarityID.Blue,
            2 => ItemRarityID.Green,
            3 => ItemRarityID.Orange,
            4 => ItemRarityID.LightRed,
            5 => ItemRarityID.Pink,
            6 => ItemRarityID.Lime,
            7 => ItemRarityID.Red,
            _ => ItemRarityID.Purple
        };

        public static FormStats GetFormStats(int form)
        {
            int stage = GetProgressionStage();
            return (stage, form) switch
            {
                (1, 1) => new(true, 20, 10, 2, 1f, 1, 30f, 0),
                (1, 2) => new(true, 40, 20, 5, 4f, 1, 10f, 0, 96),
                (2, 1) => new(true, 20, 9, 2, 1.5f, 1, 30f, 0),
                (2, 2) => new(true, 50, 20, 7, 4f, 1, 15f, 0, 96),
                (2, 3) => new(true, 100, 40, 15, 8f, 1, 10f, 0, armorPenetration: 10),
                (3, 1) => new(true, 25, 8, 2, 2f, 1, 30f, 0),
                (3, 2) => new(true, 60, 20, 9, 4f, 1, 20f, 0, 192),
                (3, 3) => new(true, 100, 36, 15, 8f, 1, 12f, 0, armorPenetration: 20),
                (4, 1) => new(true, 35, 7, 3, 2.5f, 1, 30f, 1),
                (4, 2) => new(true, 100, 20, 10, 4f, 1, 25f, 0, 192),
                (4, 3) => new(true, 125, 36, 15, 8f, 1, 14f, 0, armorPenetration: 25),
                (4, 4) => new(true, 40, 20, 15, 7f, 5, 24f, 0),
                (5, 1) => new(true, 50, 6, 4, 2.5f, 1, 30f, 1),
                (5, 2) => new(true, 150, 20, 10, 4f, 1, 30f, 0, 192),
                (5, 3) => new(true, 150, 30, 20, 9f, 1, 16f, 1, armorPenetration: 30),
                (5, 4) => new(true, 60, 20, 15, 7f, 5, 24f, 0),
                (5, 5) => new(true, 60, 10, 20, 0.1f, 1, 1f, -1, 0, 120),
                (6, 1) => new(true, 60, 5, 5, 2.5f, 1, 40f, 2),
                (6, 2) => new(true, 150, 15, 10, 4f, 1, 40f, 0, 288),
                (6, 3) => new(true, 300, 30, 20, 10f, 1, 18f, 1, armorPenetration: 50),
                (6, 4) => new(true, 70, 20, 15, 7f, 5, 24f, 0),
                (6, 5) => new(true, 75, 10, 20, 0.1f, 1, 1f, -1, 0, 120),
                (7, 1) => new(true, 75, 5, 5, 2.5f, 1, 40f, 2),
                (7, 2) => new(true, 200, 15, 10, 4f, 1, 50f, 0, 288),
                (7, 3) => new(true, 500, 30, 20, 10f, 1, 20f, 2, armorPenetration: 50),
                (7, 4) => new(true, 120, 20, 20, 9f, 7, 24f, 0),
                (7, 5) => new(true, 120, 10, 20, 0.1f, 1, 1f, -1, 0, 120),
                (7, 6) => new(true, 250, 20, 5, 1f, 1, 0f, -1, 0, 120, 150),
                (8, 1) => new(true, 150, 5, 5, 3f, 1, 50f, 2),
                (8, 2) => new(true, 250, 15, 15, 4f, 3, 50f, 0, 288),
                (8, 3) => new(true, 1000, 20, 20, 10f, 1, 20f, 3, armorPenetration: 100),
                (8, 4) => new(true, 500, 20, 30, 9f, 12, 24f, 0),
                (8, 5) => new(true, 250, 10, 20, 0.25f, 1, 1f, -1, 0, 60),
                (8, 6) => new(true, 250, 15, 5, 1f, 1, 0f, -1, 0, 120, 300),
                _ => new(false)
            };
        }

        private static int GetFormItemType(int form) => form switch
        {
            1 => ModContent.ItemType<SpiralHell>(),
            2 => ModContent.ItemType<SpiralHellImpacto>(),
            3 => ModContent.ItemType<SpiralHellEquilibrio>(),
            4 => ModContent.ItemType<SpiralHellProsperito>(),
            5 => ModContent.ItemType<SpiralHellMasquerado>(),
            6 => ModContent.ItemType<SpiralHellBarricado>(),
            _ => ModContent.ItemType<SpiralHell>()
        };

        private int GetNextUnlockedFormNumber()
        {
            // Search the remainder of the six-form loop and skip every locked
            // form. Rotatio is always unlocked, so this always finds a valid
            // destination even if the currently loaded form is itself locked.
            for (int offset = 1; offset <= 6; offset++)
            {
                int candidateForm = (FormNumber - 1 + offset) % 6 + 1;
                if (GetFormStats(candidateForm).Unlocked)
                    return candidateForm;
            }

            return 1;
        }

        private int GetNextUnlockedFormType() =>
            GetFormItemType(GetNextUnlockedFormNumber());

        public override bool AltFunctionUse(Player player) => true;

        public override void UpdateInventory(Player player)
        {
            RememberCurrentFormPrefix();
            Item.rare = GetStageRarity();
            if (ReferenceEquals(player.HeldItem, Item) && player.itemAnimation > 0)
                return;
            ApplyLeftStats();
        }

        private void ApplyLeftStats()
        {
            Item.DamageType = DamageClass.Magic;
            Item.UseSound = LeftUseSound;
            Item.shoot = LeftProjectileType;
            Item.channel = LeftChannel;
            Item.useStyle = LeftUseStyle;
            Item.noMelee = LeftNoMelee;

            DynamicWeaponPrefixHelper.Apply(
                Item,
                LeftDamage,
                CurrentStats.Knockback,
                LeftUseTime,
                LeftMana,
                LeftShootSpeed);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int stage = GetProgressionStage();
            string root = "Mods.everflow.Items.SpiralHellGrowth.";
            tooltips.Add(new TooltipLine(Mod, "GrowthStage",
                Language.GetTextValue(root + "Stage", stage)));
            tooltips.Add(new TooltipLine(Mod, "FormStatus",
                Language.GetTextValue(root + (CurrentStats.Unlocked ? "Unlocked" : "Locked"))));
            if (stage < 8)
                tooltips.Add(new TooltipLine(Mod, "NextStage",
                    Language.GetTextValue(root + "Next" + stage)));

            tooltips.Add(new TooltipLine(Mod, "HellIsEmpty",
                Language.GetTextValue(root + "HellIsEmpty"))
            {
                OverrideColor = new Color(255, 24, 24)
            });
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                player.GetModPlayer<SpiralHellSwitchPlayer>().LockFacingDirection();
                ConfigureSwitchUse(Item);
            }
            else
            {
                if (!CurrentStats.Unlocked)
                    return false;
                ApplyLeftStats();
            }

            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.altFunctionUse != 2 || player.itemAnimation != player.itemAnimationMax)
                return null;

            Item heldItem = player.inventory[player.selectedItem];
            bool wasFavorited = heldItem.favorited;
            int currentForm = FormNumber;
            int nextFormNumber = GetNextUnlockedFormNumber();
            RememberCurrentFormPrefix();

            if (player.whoAmI == Main.myPlayer)
            {
                IEntitySource source = player.GetSource_ItemUse(heldItem);
                Vector2 indicatorPosition = player.MountedCenter - Vector2.UnitY * 80f;

                // The indicator remains visible after a switch. Remove the
                // previous pair before creating the next one so two target
                // colors can never overlap or appear to change abruptly.
                int ammunitionType = ModContent.ProjectileType<SpiralHellCylinderAmmunition>();
                int chamberType = ModContent.ProjectileType<SpiralHellCylinderChamber>();
                int tentacleType = ModContent.ProjectileType<SpiralHellCylinderTentacles>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile existing = Main.projectile[i];
                    if (existing.active && existing.owner == player.whoAmI &&
                        (existing.type == ammunitionType || existing.type == chamberType ||
                         existing.type == tentacleType))
                    {
                        existing.Kill();
                    }
                }

                // Spawn in strict back-to-front order: visual tentacles,
                // rotating ammunition, then the fixed chamber mask.
                Projectile.NewProjectile(source, indicatorPosition, Vector2.Zero,
                    tentacleType, 0, 0f, player.whoAmI, currentForm,
                    nextFormNumber, SwitchUseTime);
                Projectile.NewProjectile(source, indicatorPosition, Vector2.Zero,
                    ammunitionType,
                    0, 0f, player.whoAmI, currentForm, nextFormNumber,
                    SwitchUseTime);
                Projectile.NewProjectile(source, indicatorPosition, Vector2.Zero,
                    chamberType,
                    0, 0f, player.whoAmI, currentForm, nextFormNumber,
                    SwitchUseTime);
            }

            heldItem.SetDefaults(GetFormItemType(nextFormNumber));

            if (heldItem.ModItem is SpiralHellForm nextForm)
            {
                CopyPrefixMemoryTo(nextForm);
                int nextPrefix = nextForm.GetRememberedPrefix(nextForm.FormNumber);
                if (nextPrefix > 0)
                    heldItem.Prefix(nextPrefix);
            }

            heldItem.favorited = wasFavorited;

            // SetDefaults loads the next form's left-click use time. Keep the
            // newly transformed item in switch mode until this right-click
            // action has fully ended, otherwise slow forms (notably
            // Equilibrio's 36 ticks) make form cycling feel stuck.
            ConfigureSwitchUse(heldItem);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SyncEquipment, number: player.whoAmI, number2: player.selectedItem);

            return true;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (player.altFunctionUse != 2)
                return;

            // Switching has no aiming function. Keep every differently sized
            // form pointed horizontally along the player's facing direction
            // instead of inheriting the mouse-driven Shoot-style rotation.
            int facingDirection = player.GetModPlayer<SpiralHellSwitchPlayer>()
                .LockedFacingDirection;
            if (facingDirection == 0)
                facingDirection = player.direction;

            player.ChangeDir(facingDirection);
            // Terraria's held-item renderer mirrors the sprite automatically
            // when player.direction is -1. Adding Pi here double-flips the
            // weapon, making it point right and appear upside down.
            player.itemRotation = 0f;
        }

        private int GetRememberedPrefix(int form) => form switch
        {
            1 => rotatioPrefix,
            2 => impactoPrefix,
            3 => equilibrioPrefix,
            4 => prosperitoPrefix,
            5 => masqueradoPrefix,
            6 => barricadoPrefix,
            _ => 0
        };

        private void SetRememberedPrefix(int form, int prefix)
        {
            switch (form)
            {
                case 1: rotatioPrefix = prefix; break;
                case 2: impactoPrefix = prefix; break;
                case 3: equilibrioPrefix = prefix; break;
                case 4: prosperitoPrefix = prefix; break;
                case 5: masqueradoPrefix = prefix; break;
                case 6: barricadoPrefix = prefix; break;
            }
        }

        private void RememberCurrentFormPrefix()
        {
            SetRememberedPrefix(FormNumber, Item.prefix);
        }

        private void CopyPrefixMemoryTo(SpiralHellForm destination)
        {
            destination.rotatioPrefix = rotatioPrefix;
            destination.impactoPrefix = impactoPrefix;
            destination.equilibrioPrefix = equilibrioPrefix;
            destination.prosperitoPrefix = prosperitoPrefix;
            destination.masqueradoPrefix = masqueradoPrefix;
            destination.barricadoPrefix = barricadoPrefix;
        }

        public override void SaveData(TagCompound tag)
        {
            RememberCurrentFormPrefix();
            tag["RotatioPrefix"] = rotatioPrefix;
            tag["ImpactoPrefix"] = impactoPrefix;
            tag["EquilibrioPrefix"] = equilibrioPrefix;
            tag["ProsperitoPrefix"] = prosperitoPrefix;
            tag["MasqueradoPrefix"] = masqueradoPrefix;
            tag["BarricadoPrefix"] = barricadoPrefix;
        }

        public override void LoadData(TagCompound tag)
        {
            rotatioPrefix = tag.GetInt("RotatioPrefix");
            impactoPrefix = tag.GetInt("ImpactoPrefix");
            equilibrioPrefix = tag.GetInt("EquilibrioPrefix");
            prosperitoPrefix = tag.GetInt("ProsperitoPrefix");
            masqueradoPrefix = tag.GetInt("MasqueradoPrefix");
            barricadoPrefix = tag.GetInt("BarricadoPrefix");

            // Compatibility with weapons saved before per-form prefixes existed.
            if (GetRememberedPrefix(FormNumber) == 0 && Item.prefix > 0)
                SetRememberedPrefix(FormNumber, Item.prefix);
        }

        public override void NetSend(BinaryWriter writer)
        {
            RememberCurrentFormPrefix();
            writer.Write(rotatioPrefix);
            writer.Write(impactoPrefix);
            writer.Write(equilibrioPrefix);
            writer.Write(prosperitoPrefix);
            writer.Write(masqueradoPrefix);
            writer.Write(barricadoPrefix);
        }

        public override void NetReceive(BinaryReader reader)
        {
            rotatioPrefix = reader.ReadInt32();
            impactoPrefix = reader.ReadInt32();
            equilibrioPrefix = reader.ReadInt32();
            prosperitoPrefix = reader.ReadInt32();
            masqueradoPrefix = reader.ReadInt32();
            barricadoPrefix = reader.ReadInt32();
        }

        private const int SwitchUseTime = 10;

        private static void ConfigureSwitchUse(Item item)
        {
            item.useTime = SwitchUseTime;
            item.useAnimation = SwitchUseTime;
            item.mana = 0;
            item.channel = false;
            item.useStyle = ItemUseStyleID.Shoot;
            item.noMelee = true;
            item.UseSound = SoundID.Unlock with
            {
                Volume = 0.7f,
                Pitch = -0.8f,
                PitchVariance = 0.04f
            };
            item.shoot = ProjectileID.None;
        }

        public override void ModifyShootStats(
            Player player,
            ref Vector2 position,
            ref Vector2 velocity,
            ref int type,
            ref int damage,
            ref float knockback)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            int facingDirection = direction.X == 0f
                ? player.direction
                : System.Math.Sign(direction.X);
            Vector2 perpendicular = direction.RotatedBy(
                MathHelper.PiOver2 * facingDirection);
            position = player.RotatedRelativePoint(player.MountedCenter)
                + direction * MuzzleDistance
                + perpendicular * MuzzleHeight;
            type = LeftProjectileType;
        }

        // All sprites place their grip at the same relative point near the
        // rear handle. Individual muzzle positions are configured above.
        public override Vector2? HoldoutOffset() => GripOffset;
    }

    public sealed class SpiralHell : SpiralHellForm
    {
        protected override int FormNumber => 1;
        public override string Texture => "everflow/Content/Items/SpiralHell_1";
        protected override int NextFormType => ModContent.ItemType<SpiralHellImpacto>();
        protected override int TextureWidth => 60;
        protected override int TextureHeight => 26;
        protected override float MuzzleDistance => 43f;
    }

    public sealed class SpiralHellImpacto : SpiralHellForm
    {
        protected override int FormNumber => 2;
        public override string Texture => "everflow/Content/Items/SpiralHell_2";
        protected override int NextFormType => ModContent.ItemType<SpiralHellEquilibrio>();
        protected override int TextureWidth => 72;
        protected override int TextureHeight => 36;
        // Measured directly from SpiralHell_2.png: grip center (12, 23),
        // muzzle center (68, 17). Keeping both drawing and shooting on this
        // same axis prevents the steep downward-aim kink.
        protected override float MuzzleDistance => 56f;
        protected override float MuzzleHeight => -6f;
        protected override Vector2 GripOffset => Vector2.Zero;
        protected override int LeftProjectileType => ModContent.ProjectileType<SpiralHellProjectile2>();
        protected override SoundStyle? LeftUseSound => SoundID.Item61;

        public override Vector2? HoldoutOrigin() => new Vector2(12f, 23f);

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (player.altFunctionUse == 2 || player.whoAmI != Main.myPlayer)
                return;

            Vector2 pivot = player.RotatedRelativePoint(player.MountedCenter);
            Vector2 aimDirection = (Main.MouseWorld - pivot)
                .SafeNormalize(Vector2.UnitX * player.direction);
            player.ChangeDir(aimDirection.X >= 0f ? 1 : -1);
            player.itemRotation = aimDirection.ToRotation();
            if (player.direction < 0)
                player.itemRotation -= MathHelper.Pi;
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
            if (player.altFunctionUse == 2)
                return false;
            if (player.whoAmI != Main.myPlayer)
                return false;

            // Do not use the velocity supplied by the vanilla Shoot use style:
            // around the lower-left/lower-right direction-change sectors it can
            // be transformed separately from the held-item rotation. Derive
            // both muzzle and velocity from the same world-space mouse vector.
            Vector2 pivot = player.RotatedRelativePoint(player.MountedCenter);
            Vector2 direction = (Main.MouseWorld - pivot)
                .SafeNormalize(Vector2.UnitX * player.direction);
            int facingDirection = direction.X == 0f
                ? player.direction
                : System.Math.Sign(direction.X);
            Vector2 perpendicular = direction.RotatedBy(
                MathHelper.PiOver2 * facingDirection);
            Vector2 muzzle = pivot
                + direction * MuzzleDistance
                + perpendicular * MuzzleHeight;
            Vector2 shotVelocityBase = direction * CurrentStats.ShootSpeed;

            int count = CurrentStats.ProjectileCount;
            for (int i = 0; i < count; i++)
            {
                // The post-Moon Lord three-shell volley uses a compact fan;
                // all earlier stages have count == 1 and remain perfectly straight.
                float spread = count > 1
                    ? MathHelper.Lerp(-0.08f, 0.08f, i / (float)(count - 1))
                    : 0f;
                Projectile.NewProjectile(
                    source,
                    muzzle,
                    shotVelocityBase.RotatedBy(spread),
                    type,
                    damage,
                    knockback,
                    player.whoAmI);
            }

            return false;
        }
    }

    public sealed class SpiralHellEquilibrio : SpiralHellForm
    {
        protected override int FormNumber => 3;
        public override string Texture => "everflow/Content/Items/SpiralHell_3";
        protected override int NextFormType => ModContent.ItemType<SpiralHellProsperito>();
        protected override int TextureWidth => 172;
        protected override int TextureHeight => 46;
        protected override float MuzzleDistance => 111f;
        protected override float MuzzleHeight => -2f;
        protected override Vector2 GripOffset => new Vector2(-46f, 0f);
        protected override int LeftProjectileType => ModContent.ProjectileType<SpiralHellProjectile3>();
        protected override SoundStyle? LeftUseSound => SoundID.Item40;

        public override void ModifyShootStats(
            Player player,
            ref Vector2 position,
            ref Vector2 velocity,
            ref int type,
            ref int damage,
            ref float knockback)
        {
            base.ModifyShootStats(player, ref position, ref velocity,
                ref type, ref damage, ref knockback);

            // Move only the projectile muzzle half a tile downward in world
            // space. The held weapon texture and grip remain untouched, and
            // the offset stays downward when aiming to either side.
            position.Y += 8f;
        }
    }

    public sealed class SpiralHellProsperito : SpiralHellForm
    {
        protected override int FormNumber => 4;
        public override string Texture => "everflow/Content/Items/SpiralHell_4";
        protected override int NextFormType => ModContent.ItemType<SpiralHellMasquerado>();
        protected override int TextureWidth => 92;
        protected override int TextureHeight => 26;
        protected override float MuzzleDistance => 76f;
        protected override int LeftProjectileType => ModContent.ProjectileType<SpiralHellProjectile4>();
        protected override SoundStyle? LeftUseSound => SoundID.Item36 with { Volume = 0.5f };

        public override bool Shoot(
            Player player,
            EntitySource_ItemUse_WithAmmo source,
            Vector2 position,
            Vector2 velocity,
            int type,
            int damage,
            float knockback)
        {
            if (player.altFunctionUse == 2)
                return false;

            for (int i = 0; i < CurrentStats.ProjectileCount; i++)
            {
                float spreadDegrees = (
                    Main.rand.NextFloat(-30f, 30f)
                    + Main.rand.NextFloat(-30f, 30f)) * 0.5f;
                Vector2 shotVelocity = velocity.RotatedBy(MathHelper.ToRadians(spreadDegrees));

                Projectile.NewProjectile(
                    source,
                    position,
                    shotVelocity,
                    type,
                    damage,
                    knockback,
                    player.whoAmI,
                    Main.rand.Next(5));
            }

            return false;
        }
    }

    public sealed class SpiralHellMasquerado : SpiralHellForm
    {
        protected override int FormNumber => 5;
        public override string Texture => "everflow/Content/Items/SpiralHell_5";
        protected override int NextFormType => ModContent.ItemType<SpiralHellBarricado>();
        protected override int TextureWidth => 92;
        protected override int TextureHeight => 32;
        protected override float MuzzleDistance => 76f;
        protected override float MuzzleHeight => -1f;
        protected override int LeftProjectileType => ModContent.ProjectileType<SpiralHellPrismBeam>();
        protected override SoundStyle? LeftUseSound => null;
        protected override bool LeftChannel => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
                return base.CanUseItem(player);

            int holdoutType = ModContent.ProjectileType<SpiralHellPrismBeam>();
            return player.ownedProjectileCounts[holdoutType] <= 0
                && base.CanUseItem(player);
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
            if (player.altFunctionUse == 2)
                return false;

            if (player.ownedProjectileCounts[type] <= 0)
            {
                Projectile.NewProjectile(
                    source,
                    player.MountedCenter,
                    velocity.SafeNormalize(Vector2.UnitX * player.direction),
                    type,
                    damage,
                    knockback,
                    player.whoAmI);
            }

            return false;
        }
    }

    public sealed class SpiralHellBarricado : SpiralHellForm
    {
        protected override int FormNumber => 6;
        public override string Texture => "everflow/Content/Items/SpiralHell_6";
        protected override int NextFormType => ModContent.ItemType<SpiralHell>();
        protected override int TextureWidth => 114;
        protected override int TextureHeight => 30;
        protected override float MuzzleDistance => 98f;
        protected override int LeftProjectileType => ModContent.ProjectileType<SpiralHellSlash6D>();
        protected override SoundStyle? LeftUseSound => SoundID.Item1;
        protected override int LeftUseStyle => ItemUseStyleID.Swing;
        protected override bool LeftNoMelee => true;

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            base.UseStyle(player, heldItemFrame);
            if (player.altFunctionUse == 2)
                return;

            // Swing-style items ignore HoldoutOffset/HoldoutOrigin. Translate the
            // rendered sprite around Terraria's live swing angle instead, so the
            // point 20 px right and 4 px above the old grip stays on the hand.
            Vector2 gripDelta = new Vector2(
                20f * player.direction,
                -4f * player.gravDir);
            player.itemLocation -= gripDelta.RotatedBy(
                player.itemRotation + player.fullRotation);
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
            if (player.altFunctionUse == 2)
                return false;

            float adjustedItemScale = player.GetAdjustedItemScale(Item);
            Projectile.NewProjectile(
                source,
                player.MountedCenter,
                new Vector2(player.direction, 0f),
                type,
                damage,
                knockback,
                player.whoAmI,
                player.direction * player.gravDir,
                player.itemAnimationMax,
                adjustedItemScale);
            NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, player.whoAmI);

            int starDamage = (int)player.GetDamage(DamageClass.Magic)
                .ApplyTo(CurrentStats.StarDamage);
            SpawnStar<SpiralHellStar6A>(player, source, starDamage);
            SpawnStar<SpiralHellStar6B>(player, source, starDamage);
            SpawnStar<SpiralHellStar6C>(player, source, starDamage);
            return false;
        }

        private static void SpawnStar<T>(
            Player player,
            EntitySource_ItemUse_WithAmmo source,
            int damage)
            where T : ModProjectile
        {
            float radius = MathF.Sqrt(Main.rand.NextFloat()) * 160f;
            Vector2 offset = Main.rand.NextVector2Unit() * radius;
            Projectile.NewProjectile(
                source,
                player.Center + offset,
                Vector2.Zero,
                ModContent.ProjectileType<T>(),
                damage,
                0f,
                player.whoAmI);
        }
    }
}
