using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Buffs
{
    public sealed class MonsterChilledDebuff : ModBuff
    {
        public override string Texture =>
            $"Terraria/Images/Buff_{BuffID.Chilled}";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            if (npc.buffImmune[BuffID.Chilled] ||
                MonsterColdControlNPC.IsFullBoss(npc))
            {
                npc.DelBuff(buffIndex);
                buffIndex--;
                return;
            }

            npc.GetGlobalNPC<MonsterColdControlNPC>().Chilled = true;
        }
    }

    public sealed class MonsterFrozenDebuff : ModBuff
    {
        public override string Texture =>
            $"Terraria/Images/Buff_{BuffID.Frozen}";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            if (npc.buffImmune[BuffID.Frozen])
            {
                npc.DelBuff(buffIndex);
                buffIndex--;
                return;
            }

            npc.GetGlobalNPC<MonsterColdControlNPC>().MarkFrozen();
        }
    }

    public sealed class MonsterColdControlNPC : GlobalNPC
    {
        private const float ChilledVelocityMultiplier = 0.5f;
        private const float BossControlDurationMultiplier = 0.33f;
        private const int FreezeRecoveryWindow = 5 * 60;
        private const float FrozenOverlayOpacity = 0.6f;
        private const float FrozenOverlayPadding = 10f;

        private static Asset<Texture2D> frozenOverlay;

        public override bool InstancePerEntity => true;

        internal bool Chilled;
        internal bool Frozen;
        private int ticksSinceFrozenEnded = FreezeRecoveryWindow;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                frozenOverlay = ModContent.Request<Texture2D>(
                    "Terraria/Images/Frozen",
                    AssetRequestMode.ImmediateLoad);
            }
        }

        public override void Unload() => frozenOverlay = null;

        public override void ResetEffects(NPC npc)
        {
            Chilled = false;
            Frozen = false;
        }

        public static bool TryApplyChilled(NPC npc, int baseDuration)
        {
            if (npc.buffImmune[BuffID.Chilled] || IsFullBoss(npc))
                return false;

            npc.AddBuff(
                ModContent.BuffType<MonsterChilledDebuff>(),
                baseDuration);
            return true;
        }

        /// <summary>
        /// 施加带五秒抗连续控制窗口的冰冻。冰冻期间不会刷新持续时间；
        /// 冰冻结束后，持续时间按已经恢复的比例从0%线性恢复至100%。
        /// </summary>
        public static bool TryApplyFrozen(NPC npc, int baseDuration)
        {
            if (npc.buffImmune[BuffID.Frozen] ||
                npc.HasBuff(ModContent.BuffType<MonsterFrozenDebuff>()))
            {
                return false;
            }

            MonsterColdControlNPC control =
                npc.GetGlobalNPC<MonsterColdControlNPC>();
            float recoveryRatio = MathHelper.Clamp(
                control.ticksSinceFrozenEnded /
                (float)FreezeRecoveryWindow,
                0f,
                1f);
            int bossAdjustedDuration =
                ScaleControlDuration(npc, baseDuration);
            int scaledDuration =
                (int)(bossAdjustedDuration * recoveryRatio);
            if (scaledDuration <= 0)
                return false;

            npc.AddBuff(
                ModContent.BuffType<MonsterFrozenDebuff>(),
                scaledDuration);
            return true;
        }

        internal void MarkFrozen()
        {
            Frozen = true;
            ticksSinceFrozenEnded = 0;
        }

        private static int ScaleControlDuration(NPC npc, int duration)
        {
            if (!IsFullBoss(npc))
                return duration;

            return System.Math.Max(
                1,
                (int)(duration * BossControlDurationMultiplier));
        }

        internal static bool IsFullBoss(NPC npc)
        {
            if (npc.boss)
                return true;

            int rootIndex = npc.realLife;
            return rootIndex >= 0 && rootIndex < Main.maxNPCs &&
                Main.npc[rootIndex].active && Main.npc[rootIndex].boss;
        }

        public override bool PreAI(NPC npc)
        {
            if (!Frozen)
                return true;

            // 完全跳过AI并清空速度，使目标硬控在当前世界坐标。
            npc.velocity = Vector2.Zero;
            return false;
        }

        public override void PostAI(NPC npc)
        {
            if (Frozen)
            {
                ticksSinceFrozenEnded = 0;
            }
            else if (ticksSinceFrozenEnded < FreezeRecoveryWindow)
            {
                ticksSinceFrozenEnded++;
            }

            if (Chilled && !Frozen)
                npc.velocity *= ChilledVelocityMultiplier;
        }

        public override void PostDraw(
            NPC npc,
            SpriteBatch spriteBatch,
            Vector2 screenPos,
            Color drawColor)
        {
            if (!Frozen || frozenOverlay?.Value == null)
                return;

            Texture2D texture = frozenOverlay.Value;
            Vector2 drawPosition = npc.Center - screenPos;
            drawPosition.Y += npc.gfxOffY;

            // 原版贴图围绕玩家设计。分别按NPC碰撞箱宽高缩放，确保从小型
            // 史莱姆到大型敌怪都由尺寸匹配的寒冰覆盖，而不是固定大小图标。
            Vector2 scale = new Vector2(
                (npc.width + FrozenOverlayPadding) / texture.Width,
                (npc.height + FrozenOverlayPadding) / texture.Height);

            spriteBatch.Draw(
                texture,
                drawPosition,
                null,
                Color.White * (FrozenOverlayOpacity * npc.Opacity),
                0f,
                texture.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0f);
        }
    }
}
