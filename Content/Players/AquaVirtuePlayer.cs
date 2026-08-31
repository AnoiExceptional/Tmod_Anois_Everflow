using everflow.Content.Buffs;
using everflow.Content.Projectiles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Players
{
    public sealed class AquaVirtuePlayer : ModPlayer
    {
        private const ulong VastWindowTicks = 300;
        private readonly Queue<ManaSpendEvent> recentManaSpending = new();
        private readonly Queue<ManaRestoreEvent> recentManaRestoration = new();
        private int recentManaSpentTotal;
        private int recentManaRestoredTotal;
        private int manaAtStartOfTick;
        private int accountedManaSpent;
        private int accountedManaRestored;
        private bool hasManaSnapshot;
        private int manaAfterLastFinalization;
        private bool hasFinalizedManaSnapshot;
        private bool wasEquippedAtLastFinalization;
        private int pendingManaStarRestoration;
        private ulong lastConfirmedEquippedTick;
        private bool pendingAutomaticManaPotion;
        private int manaBeforeAutomaticPotion;
        private int manaSpentDuringAutomaticPotion;
        private int effectAnimationTimer;
        private Vector2 effectWorldPosition;
        private bool effectPositionInitialized;

        public bool Equipped;
        public bool EffectVisible;
        internal bool ActiveForPickup => Equipped ||
            Main.GameUpdateCount - lastConfirmedEquippedTick <= 1UL;

        internal void MarkEquipped()
        {
            Equipped = true;
            lastConfirmedEquippedTick = Main.GameUpdateCount;
        }

        public int EffectAnimationFrame => effectAnimationTimer <= 0
            ? 0
            : Math.Min(4, (effectAnimationTimer - 1) / 6);

        public Vector2 EffectWorldPosition => effectWorldPosition;

        private float EffectChargeRatio => MathHelper.Clamp(
            (AquaVirtueVastStacks + AquaVirtueSurgingStacks) / 60f,
            0f,
            1f);

        public float EffectOpacity => MathHelper.Lerp(0.40f, 1f, EffectChargeRatio);

        private float EffectLightStrength => MathHelper.Lerp(0.55f * 0.80f, 0.55f * 2f, EffectChargeRatio);

        public int AquaVirtueVastStacks
        {
            get
            {
                RemoveExpiredManaSpending();
                float manaPerStack = Math.Max(1f, Player.statManaMax2 * 0.05f);
                return (int)(recentManaSpentTotal / manaPerStack);
            }
        }

        public int AquaVirtueSurgingStacks
        {
            get
            {
                RemoveExpiredManaRestoration();
                float manaPerStack = Math.Max(1f, Player.statManaMax2 * 0.05f);
                return (int)(recentManaRestoredTotal / manaPerStack);
            }
        }

        public override void ResetEffects()
        {
            Equipped = false;
            EffectVisible = false;
        }

        public override void PostUpdateMiscEffects()
        {
            if (!Equipped)
            {
                pendingManaStarRestoration = 0;
                effectAnimationTimer = 0;
                effectPositionInitialized = false;
                return;
            }

            if (pendingManaStarRestoration > 0)
            {
                RecordManaRestoration(pendingManaStarRestoration);
                pendingManaStarRestoration = 0;
            }

            if (effectAnimationTimer > 0 && ++effectAnimationTimer > 30)
                effectAnimationTimer = 0;

            if (!Player.HasBuff(ModContent.BuffType<AquaVirtueBuff_2>()))
                return;

            int stacks = AquaVirtueSurgingStacks;
            if (stacks <= 0)
                return;

            float multiplier = 1f + stacks * 0.01f;
            Player.moveSpeed *= multiplier;
            Player.statDefense += (int)MathF.Round(Player.statDefense * stacks * 0.01f);
        }

        public override void PostUpdate()
        {
            if (!Equipped)
                return;

            if (!EffectVisible)
            {
                effectPositionInitialized = false;
                return;
            }

            if (Player.whoAmI == Main.myPlayer &&
                Player.ownedProjectileCounts[ModContent.ProjectileType<AquaVirtueEffectProjectile>()] <= 0)
            {
                Projectile.NewProjectile(
                    Player.GetSource_Misc("AquaVirtue"),
                    Player.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<AquaVirtueEffectProjectile>(),
                    0,
                    0f,
                    Player.whoAmI);
            }

            Vector2 target = Player.MountedCenter + new Vector2(-Player.direction * 32f, -16f);
            if (!effectPositionInitialized || Vector2.DistanceSquared(effectWorldPosition, target) > 1200f * 1200f)
            {
                effectWorldPosition = target;
                effectPositionInitialized = true;
            }
            else
            {
                // 纯指数阻尼：始终从当前位置向目标收敛，不积累速度，因此不会越位回弹。
                // 较低的跟随比例保留明显的移动滞后。
                effectWorldPosition = Vector2.Lerp(effectWorldPosition, target, 0.06f);
            }

            Lighting.AddLight(effectWorldPosition, new Vector3(0.816f, 1f, 0.918f) * EffectLightStrength);
        }

        public override void ModifyManaCost(Item item, ref float reduce, ref float mult)
        {
            if (Equipped)
                mult *= 2f;
        }

        public override void PreUpdate()
        {
            // Item pickups are processed after PostUpdateEverything.  Compare against the
            // preceding final snapshot before beginning this tick so mana stars cannot fall
            // into the gap between two same-tick snapshots.
            if (hasFinalizedManaSnapshot && wasEquippedAtLastFinalization)
            {
                int lateManaChange = Player.statMana - manaAfterLastFinalization;
                if (lateManaChange > 0)
                {
                    // A pickup queued after the previous finalization will be committed once
                    // UpdateAccessory has reliably confirmed Aqua Virtue this tick.
                    int unaccountedLateRestoration = Math.Max(0, lateManaChange - pendingManaStarRestoration);
                    if (unaccountedLateRestoration > 0)
                        RecordManaRestoration(unaccountedLateRestoration);
                }
                else if (lateManaChange < 0)
                {
                    int spent = -lateManaChange;
                    RecordManaSpending(spent);
                    RestoreLife(spent / 5);
                }
            }

            manaAtStartOfTick = Player.statMana;
            accountedManaSpent = 0;
            accountedManaRestored = 0;
            hasManaSnapshot = true;
        }

        public override void OnConsumeMana(Item item, int manaConsumed)
        {
            if (!Equipped || manaConsumed <= 0)
                return;

            accountedManaSpent += manaConsumed;
            if (pendingAutomaticManaPotion)
                manaSpentDuringAutomaticPotion += manaConsumed;
            RecordManaSpending(manaConsumed);
            RestoreLife(manaConsumed / 5);
        }

        public override void OnMissingMana(Item item, int neededMana)
        {
            if (!Equipped)
                return;

            // manaFlower/QuickMana 会在此回调之后饮用药水。先保存饮用前的魔力，
            // 等本次物品使用完成后再把同一过程中扣除的施法魔力加回，从而得到实际回魔量。
            pendingAutomaticManaPotion = true;
            manaBeforeAutomaticPotion = Player.statMana;
            manaSpentDuringAutomaticPotion = 0;
        }

        public override void PostItemCheck()
        {
            if (!pendingAutomaticManaPotion)
                return;

            int restoredByPotion = Player.statMana - manaBeforeAutomaticPotion + manaSpentDuringAutomaticPotion;
            if (restoredByPotion > 0)
            {
                RecordManaRestoration(restoredByPotion);
                accountedManaRestored += restoredByPotion;
            }

            pendingAutomaticManaPotion = false;
            manaSpentDuringAutomaticPotion = 0;
            pendingManaStarRestoration = 0;
        }

        internal void FinalizeManaChangesForTick()
        {
            if (!hasManaSnapshot)
                return;

            if (Equipped)
            {
                int netChange = Player.statMana - manaAtStartOfTick;

            // 净变化 = 总恢复 - 总消耗。把已知的施法耗魔量加回去后，即使喝药或
            // 拾取魔力星与施法发生在同一帧，也能统计到完整恢复量，而非仅统计净增长。
                int unaccountedRestored = Math.Max(0, netChange + accountedManaSpent - accountedManaRestored);
                if (unaccountedRestored > 0)
                    RecordManaRestoration(unaccountedRestored);

                int unaccountedSpent = Math.Max(0, accountedManaRestored - netChange - accountedManaSpent);
                if (unaccountedSpent > 0)
                {
                    RecordManaSpending(unaccountedSpent);
                    RestoreLife(unaccountedSpent / 5);
                }
            }

            manaAfterLastFinalization = Player.statMana;
            wasEquippedAtLastFinalization = Equipped;
            hasFinalizedManaSnapshot = true;
            hasManaSnapshot = false;
        }

        public override void UpdateDead()
        {
            recentManaSpending.Clear();
            recentManaRestoration.Clear();
            recentManaSpentTotal = 0;
            recentManaRestoredTotal = 0;
            hasManaSnapshot = false;
            hasFinalizedManaSnapshot = false;
            wasEquippedAtLastFinalization = false;
            pendingAutomaticManaPotion = false;
            manaSpentDuringAutomaticPotion = 0;
        }

        private void RecordManaSpending(int amount)
        {
            if (amount <= 0)
                return;

            RemoveExpiredManaSpending();
            recentManaSpending.Enqueue(new ManaSpendEvent(Main.GameUpdateCount, amount));
            recentManaSpentTotal += amount;
            Player.AddBuff(ModContent.BuffType<AquaVirtueBuff_1>(), (int)VastWindowTicks);
            TriggerEffectAnimation();
        }

        private void RemoveExpiredManaSpending()
        {
            while (recentManaSpending.Count > 0 && Main.GameUpdateCount - recentManaSpending.Peek().Tick >= VastWindowTicks)
                recentManaSpentTotal -= recentManaSpending.Dequeue().Amount;
        }

        private void RecordManaRestoration(int amount)
        {
            if (amount <= 0)
                return;

            RemoveExpiredManaRestoration();
            recentManaRestoration.Enqueue(new ManaRestoreEvent(Main.GameUpdateCount, amount));
            recentManaRestoredTotal += amount;
            Player.AddBuff(ModContent.BuffType<AquaVirtueBuff_2>(), (int)VastWindowTicks);
            TriggerEffectAnimation();
        }

        internal void QueueManaStarPickup(int nominalRestoration)
        {
            int offeredRestoration = Math.Max(0, nominalRestoration);
            if (offeredRestoration <= 0)
                return;

            int actualRestoration = Math.Min(offeredRestoration,
                Math.Max(0, Player.statManaMax2 - Player.statMana));

            // “潮”按恢复来源提供的完整数值记录，包含因魔力已满而溢出的部分。
            pendingManaStarRestoration += offeredRestoration;
            // If pickup happens before this tick's final snapshot, prevent the generic net
            // change monitor from recording it a second time.
            accountedManaRestored += actualRestoration;
        }

        private void TriggerEffectAnimation()
        {
            // 已在播放时不从头打断，确保每次启动都能完整走完五帧。
            if (effectAnimationTimer <= 0)
                effectAnimationTimer = 1;
        }

        private void RemoveExpiredManaRestoration()
        {
            while (recentManaRestoration.Count > 0 && Main.GameUpdateCount - recentManaRestoration.Peek().Tick >= VastWindowTicks)
                recentManaRestoredTotal -= recentManaRestoration.Dequeue().Amount;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (!Equipped || Player.whoAmI != Main.myPlayer)
                return;

            RestoreMana(info.Damage / 5);

            for (int i = 0; i < 3; i++)
            {
                float spawnX = Player.position.X + Main.rand.Next(-400, 400);
                float spawnY = Player.position.Y - Main.rand.Next(500, 800);
                Vector2 spawnPosition = new(spawnX, spawnY);
                Vector2 velocity = Player.Center - spawnPosition;
                velocity.X += Main.rand.Next(-100, 101);
                velocity = velocity.SafeNormalize(Vector2.UnitY) * 23f;

                int damage = 75;
                if (Main.masterMode)
                    damage *= 3;
                else if (Main.expertMode)
                    damage *= 2;

                Projectile.NewProjectile(Player.GetSource_OnHurt(info.DamageSource), spawnPosition, velocity,
                    ModContent.ProjectileType<AquaVirtueFallingStar>(), damage, 5f, Player.whoAmI,
                    ai1: Player.position.Y);
            }
        }

        private void RestoreLife(int amount)
        {
            if (amount <= 0 || Player.statLife >= Player.statLifeMax2 || Player.whoAmI != Main.myPlayer)
                return;

            Player.Heal(Math.Min(amount, Player.statLifeMax2 - Player.statLife));
        }

        private void RestoreMana(int amount)
        {
            int restored = Math.Min(Math.Max(0, amount), Player.statManaMax2 - Player.statMana);
            if (restored <= 0)
                return;

            Player.statMana += restored;
            accountedManaRestored += restored;
            Player.ManaEffect(restored);
            RecordManaRestoration(restored);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.PlayerMana, number: Player.whoAmI);
        }

        private readonly record struct ManaSpendEvent(ulong Tick, int Amount);
        private readonly record struct ManaRestoreEvent(ulong Tick, int Amount);
    }
}
