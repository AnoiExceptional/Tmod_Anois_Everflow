using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Buffs
{
    public sealed class EnergyCutDebuff : ModBuff
    {
        public const int MaximumStacks = 30;
        public const int Duration = 20 * 60;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            EnergyCutGlobalNPC energyCut =
                npc.GetGlobalNPC<EnergyCutGlobalNPC>();
            if (energyCut.Stacks <= 0)
                energyCut.Stacks = 1;
        }

        internal static void Apply(NPC target)
        {
            int buffType = ModContent.BuffType<EnergyCutDebuff>();
            bool alreadyActive = target.HasBuff(buffType);
            EnergyCutGlobalNPC energyCut =
                target.GetGlobalNPC<EnergyCutGlobalNPC>();

            if (!alreadyActive)
                energyCut.Stacks = 1;
            else if (energyCut.Stacks < MaximumStacks)
                energyCut.Stacks++;

            // AddBuff会把低于20秒的现有计时重新提高到完整1200帧；
            // 满30层后不再增加层数，但仍执行这里来刷新持续时间。
            target.AddBuff(buffType, Duration);
            target.netUpdate = true;
        }
    }

    public sealed class EnergyCutGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        internal int Stacks;

        public override void ResetEffects(NPC npc)
        {
            if (!npc.HasBuff<EnergyCutDebuff>())
                Stacks = 0;
        }

        public override void ModifyIncomingHit(
            NPC npc,
            ref NPC.HitModifiers modifiers)
        {
            if (Stacks > 0 && npc.HasBuff<EnergyCutDebuff>())
                modifiers.ArmorPenetration += Stacks;
        }
    }
}
