using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Events;
using Terraria.GameContent.Personalities;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using everflow.Content.Items;
using everflow.Content.Players;
using everflow.Content.Projectiles;
using everflow.Content.Systems;

namespace everflow.Content.NPCs
{
    public sealed class AncientSmith : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Guide];
            NPCID.Sets.ExtraFramesCount[Type] = NPCID.Sets.ExtraFramesCount[NPCID.Guide];
            NPCID.Sets.AttackFrameCount[Type] = NPCID.Sets.AttackFrameCount[NPCID.Guide];
            NPCID.Sets.DangerDetectRange[Type] = 700;
            NPCID.Sets.AttackType[Type] = 1;
            NPCID.Sets.AttackTime[Type] = 25;
            NPCID.Sets.AttackAverageChance[Type] = 10;

            NPC.Happiness
                .SetBiomeAffection<OceanBiome>(AffectionLevel.Love)
                .SetBiomeAffection<ForestBiome>(AffectionLevel.Love)
                .SetBiomeAffection<DesertBiome>(AffectionLevel.Like)
                .SetBiomeAffection<UndergroundBiome>(AffectionLevel.Hate)
                .SetNPCAffection(NPCID.PartyGirl, AffectionLevel.Love)
                .SetNPCAffection(NPCID.Princess, AffectionLevel.Like)
                .SetNPCAffection(NPCID.Pirate, AffectionLevel.Like)
                .SetNPCAffection(NPCID.Truffle, AffectionLevel.Hate)
                .SetNPCAffection(NPCID.GoblinTinkerer, AffectionLevel.Hate);
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = NPCAIStyleID.Passive;
            NPC.damage = 10;
            NPC.defense = 15;
            NPC.lifeMax = 250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;
            AnimationType = NPCID.Guide;
        }

        public override bool CanTownNPCSpawn(int numTownNPCs) =>
            !NPC.AnyNPCs(Type);

        public override List<string> SetNPCNameList() => new()
        {
            "Ajax", "Alexios", "Arion", "Albus", "Caius",
            "Diogenes", "Hector", "Horace", "Jason", "Julius",
            "Leonidas", "Linus", "Magnus", "Marcell", "Nikandros",
            "Orion", "Otho", "Philon", "Pylos", "Quintus",
            "Theocritus", "Timoleon", "Vitus", "Xanthos", "Xenophon"
        };

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                new FlavorTextBestiaryInfoElement(
                    "Mods.everflow.Bestiary.AncientSmith")
            });
        }

        public override string GetChat()
        {
            Player player = Main.LocalPlayer;
            AncientSmithPlayer smithPlayer = player.GetModPlayer<AncientSmithPlayer>();
            if (!smithPlayer.HasMetAncientSmith)
            {
                smithPlayer.HasMetAncientSmith = true;
                return "如你所见，我来自一个过去的文明。但人总要往前看。";
            }

            List<string> chat = new()
            {
                "如果你了解金属冶炼的法则，你就会爱上这门艺术。",
                "我在思考令金属更强的配比。请讲。"
            };

            if (WorldGen.SavedOreTiers.Iron == TileID.Lead)
                chat.Add("这个世界的矿石构成不合常理。但好在有铅能令我的美酒香甜。");
            else if (WorldGen.SavedOreTiers.Iron == TileID.Iron)
                chat.Add("这个世界的矿石构成不合常理。但好在有铁铸就我的可靠武器。");

            AddResidentChat(chat, NPCID.Pirate,
                name => $"空闲的时候，我喜欢喊上{name}一起去海上兜个风。");
            AddResidentChat(chat, NPCID.PartyGirl,
                name => $"请帮我询问{name}，下一场盛宴将会是在何时？");
            AddResidentChat(chat, NPCID.Princess,
                _ => "那位女孩的气质令我想到曾经侍奉过的君主的女儿。");

            if (BirthdayParty.PartyIsUp)
                chat.Add("狂欢吧！畅饮吧！愿这场宴席永不结束！");
            if (Main.hardMode)
                chat.Add("有种似曾相识的感觉…希望这个文明不要重蹈覆辙。");
            if (AncientSmithWorldFlags.DownedAncientArmy)
            {
                chat.Add("你已经见过埃洛伊的遗民了？…我知道了。");
                chat.Add("埃洛伊是战斗、冶炼与艺术的国度。我们擅长使用各种武器。");
            }
            if (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3)
                chat.Add("那些钢铁巨兽的金属外壳材料我很感兴趣。");

            return chat[Main.rand.Next(chat.Count)];
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("LegacyInterface.28");
            button2 = "武器";
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
            {
                shopName = "Shop";
                return;
            }

            Player player = Main.LocalPlayer;
            SmithWeaponKind weapon = FindFirstIdentifiableWeapon(player);
            if (weapon == SmithWeaponKind.BurgerKing)
            {
                Main.npcChatText = "…我知道这把武器。来自狄俄尼索斯的神迹，永恒的圣餐，吞食者将获得无上伟力，同时也永远肩负诅咒：更深的饥渴。拥有者往往死于对于神明血肉的贪念。";
                if (!AncientSmithWorldFlags.BurgerCookerClaimed)
                {
                    AncientSmithWorldFlags.BurgerCookerClaimed = true;
                    player.QuickSpawnItem(
                        player.GetSource_Misc("AncientSmithBurgerKnowledge"),
                        ModContent.ItemType<BurgerCooker>(),
                        1);
                }
                return;
            }

            if (weapon == SmithWeaponKind.SpiralHell)
            {
                Main.npcChatText = "我不理解这把武器的技术。它完全不合常理，就像是有生命的金属一般。但有一点可以肯定：它的弹仓有六发，意味着一共有六种变形模式。";
                return;
            }

            if (weapon == SmithWeaponKind.DragonsEdge)
            {
                Main.npcChatText = "这把武器来自遥远的东方文明。传说那是天空之土，龙之国度，但就我看来其锻造工艺与我们不相上下。倒是那种可以召唤神龙的神秘铭文技术更有意思。";
                return;
            }

            Main.npcChatText = "把值得研究的武器带在身上，我会告诉你它的来历。";
        }

        public override void AddShops()
        {
            NPCShop shop = new NPCShop(Type, "Shop")
                .Add(ShopItem(ItemID.CopperBar, 2))
                .Add(ShopItem(ItemID.TinBar, 2))
                .Add(ShopItem(ItemID.IronBar, 5))
                .Add(ShopItem(ItemID.LeadBar, 5))
                .Add(ShopItem(ItemID.SilverBar, 10))
                .Add(ShopItem(ItemID.TungstenBar, 10))
                .Add(ShopItem(ItemID.GoldBar, 20))
                .Add(ShopItem(ItemID.PlatinumBar, 20));
            shop.Register();
        }

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = NPC.downedMoonlord ? 75
                : NPC.downedPlantBoss ? 30
                : Main.hardMode ? 50
                : 25;
            knockback = 5f;
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 25;
            randExtraCooldown = 20;
        }

        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            projType = NPC.downedPlantBoss
                ? ModContent.ProjectileType<AncientSmithMagicProjectile>()
                : ModContent.ProjectileType<AncientSmithSpearProjectile>();
            attackDelay = 1;
        }

        public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
            multiplier = NPC.downedPlantBoss ? 10f : 8f;
            gravityCorrection = 0f;
            randomOffset = 0.08f;
        }

        private static Item ShopItem(int type, int silverPrice)
        {
            Item item = new(type) { shopCustomPrice = Item.buyPrice(silver: silverPrice) };
            return item;
        }

        private static SmithWeaponKind FindFirstIdentifiableWeapon(Player player)
        {
            for (int i = 0; i < 58; i++)
            {
                int type = player.inventory[i].type;
                if (type == ModContent.ItemType<DragonsEdge>())
                    return SmithWeaponKind.DragonsEdge;
                if (type == ModContent.ItemType<BurgerKing>())
                    return SmithWeaponKind.BurgerKing;
                if (IsSpiralHellForm(type))
                    return SmithWeaponKind.SpiralHell;
            }

            return SmithWeaponKind.None;
        }

        private static bool IsSpiralHellForm(int type)
        {
            return type == ModContent.ItemType<SpiralHell>() ||
                type == ModContent.ItemType<SpiralHellImpacto>() ||
                type == ModContent.ItemType<SpiralHellEquilibrio>() ||
                type == ModContent.ItemType<SpiralHellProsperito>() ||
                type == ModContent.ItemType<SpiralHellMasquerado>() ||
                type == ModContent.ItemType<SpiralHellBarricado>();
        }

        private enum SmithWeaponKind
        {
            None,
            DragonsEdge,
            SpiralHell,
            BurgerKing
        }

        private static void AddResidentChat(List<string> chat, int npcType, System.Func<string, string> factory)
        {
            int index = NPC.FindFirstNPC(npcType);
            if (index >= 0)
                chat.Add(factory(Main.npc[index].GivenName));
        }
    }
}
