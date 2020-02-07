﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using kRPG.Enums;
using kRPG.GameObjects.Items.Glyphs;
using kRPG.GameObjects.Items.Procedural;
using kRPG.GameObjects.Items.Weapons.Ranged;
using kRPG.GameObjects.NPCs;
using kRPG.GameObjects.Players;
using kRPG.GameObjects.SFX;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Achievements;
using Terraria.GameInput;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using Terraria.UI.Gamepad;

namespace kRPG
{
    public static class API
    {
        //    public const double Phi = 1.61803398874989484820458683436;

        private const byte c = 0;
        private const byte g = 2;
        private const byte p = 3;
        private const byte s = 1;
        public static double Tau => Math.PI * 2;

        private static readonly FieldInfo InventoryGlowHue = typeof(ItemSlot).GetField("inventoryGlowHue", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo InventoryGlowTime = typeof(ItemSlot).GetField("inventoryGlowTime", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo GlobalItemsProperty = typeof(Item).GetField("globalItems", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo InventoryGlowTimeChest = typeof(ItemSlot).GetField("inventoryGlowTimeChest", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo IsModItem = typeof(ItemLoader).GetMethod("IsModItem", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly PropertyInfo ModItem = typeof(Item).GetProperty("modItem", BindingFlags.Public | BindingFlags.Instance);




        public static void ApiCreate(this Recipe recipe)
        {
            for (int i = 0; i < Recipe.maxRequirements; i++)
            {
                Item requiredItem = recipe.requiredItem[i];
                if (requiredItem.type == 0)
                    break;
                int requiredAmount = requiredItem.stack;
                if (recipe is ModRecipe modRecipe)
                    requiredAmount = modRecipe.ConsumeItem(requiredItem.type, requiredItem.stack);
                if (recipe.alchemy && Main.player[Main.myPlayer].alchemyTable)
                {
                    if (requiredAmount > 1)
                    {
                        int num2 = 0;
                        for (int j = 0; j < requiredAmount; j++)
                            if (Main.rand.Next(3) == 0)
                                num2++;
                        requiredAmount -= num2;
                    }
                    else if (Main.rand.Next(3) == 0)
                    {
                        requiredAmount = 0;
                    }
                }

                if (requiredAmount <= 0)
                    continue;
                {
                    Item[] array = Main.player[Main.myPlayer].inventory;
                    InvLogic(recipe, array, requiredItem, requiredAmount);
                    PlayerCharacter character = Main.LocalPlayer.GetModPlayer<PlayerCharacter>();
                    for (int j = 0; j < character.Inventories.Length; j += 1)
                        if (character.ActiveInvPage != j)
                        {
                            array = character.Inventories[j];
                            InvLogic(recipe, array, requiredItem, requiredAmount);
                        }

                    if (Main.player[Main.myPlayer].chest == -1)
                        continue;
                    if (Main.player[Main.myPlayer].chest > -1)
                        array = Main.chest[Main.player[Main.myPlayer].chest].item;
                    else
                        switch (Main.player[Main.myPlayer].chest)
                        {
                            case -2:
                                array = Main.player[Main.myPlayer].bank.item;
                                break;
                            case -3:
                                array = Main.player[Main.myPlayer].bank2.item;
                                break;
                            case -4:
                                array = Main.player[Main.myPlayer].bank3.item;
                                break;
                        }

                    for (int l = 0; l < 40; l++)
                    {
                        Item item3 = array[l];
                        if (requiredAmount <= 0)
                            break;

                        if (!item3.IsTheSameAs(requiredItem) && !recipe.useWood(item3.type, requiredItem.type) &&
                            !recipe.useSand(item3.type, requiredItem.type) && !recipe.useIronBar(item3.type, requiredItem.type) &&
                            !recipe.usePressurePlate(item3.type, requiredItem.type) && !recipe.useFragment(item3.type, requiredItem.type) &&
                            !recipe.AcceptedByItemGroups(item3.type, requiredItem.type))
                            continue;
                        if (item3.stack > requiredAmount)
                        {
                            item3.stack -= requiredAmount;
                            if (Main.netMode == 1 && Main.player[Main.myPlayer].chest >= 0)
                                NetMessage.SendData(32, -1, -1, null, Main.player[Main.myPlayer].chest, l);
                            requiredAmount = 0;
                        }
                        else
                        {
                            requiredAmount -= item3.stack;
                            array[l] = new Item();
                            if (Main.netMode == 1 && Main.player[Main.myPlayer].chest >= 0)
                                NetMessage.SendData(32, -1, -1, null, Main.player[Main.myPlayer].chest, l);
                        }
                    }
                }
            }

            AchievementsHelper.NotifyItemCraft(recipe);
            AchievementsHelper.NotifyItemPickup(Main.player[Main.myPlayer], recipe.createItem);
            FindRecipes();
        }

        public static void ApiQuickBuff(this Player player)
        {
            if (player.noItems)
                return;
            PlayerCharacter character = player.GetModPlayer<PlayerCharacter>();

            if (player.CountBuffs() == 22)
                return;

            for (int i = 0; i < 58; i += 1)
            {
                Item item = player.inventory[i];
                APIQuickBuff_TryItem(player, item);
            }

            for (int i = 0; i < character.Inventories.Length; i += 1)
            {
                if (character.ActiveInvPage == i)
                    continue;
                for (int j = 0; j < character.Inventories[i].Length; j += 1)
                {
                    Item item = character.Inventories[i][j];
                    APIQuickBuff_TryItem(player, item);
                }
            }
        }

        private static void APIQuickBuff_TryItem(Player player, Item item)
        {
            LegacySoundStyle legacySoundStyle = null;

            if (item.stack > 0 && item.type > 0 && item.buffType > 0 && !item.summon && item.buffType != 90)
            {
                int num = item.buffType;
                bool flag = ItemLoader.CanUseItem(item, player);
                for (int b = 0; b < 22; b++)
                {
                    if (num == 27 && (player.buffType[b] == num || player.buffType[b] == 101 || player.buffType[b] == 102))
                    {
                        flag = false;
                        break;
                    }

                    if (player.buffType[b] == num)
                    {
                        flag = false;
                        break;
                    }

                    if (!Main.meleeBuff[num] || !Main.meleeBuff[player.buffType[b]])
                        continue;
                    flag = false;
                    break;
                }

                if (Main.lightPet[item.buffType] || Main.vanityPet[item.buffType])
                    for (int k = 0; k < 22; k++)
                    {
                        if (Main.lightPet[player.buffType[k]] && Main.lightPet[item.buffType])
                            flag = false;
                        if (Main.vanityPet[player.buffType[k]] && Main.vanityPet[item.buffType])
                            flag = false;
                    }

                if ((item.mana > 0) & flag)
                {
                    if (player.statMana >= (int)(item.mana * player.manaCost))
                    {
                        player.manaRegenDelay = (int)player.maxRegenDelay;
                        player.statMana -= (int)(item.mana * player.manaCost);
                    }
                    else
                    {
                        flag = false;
                    }
                }

                if (player.whoAmI == Main.myPlayer && item.type == 603 && !Main.cEd)
                    flag = false;
                if (num == 27)
                {
                    num = Main.rand.Next(3);
                    if (num == 0)
                        num = 27;
                    if (num == 1)
                        num = 101;
                    if (num == 2)
                        num = 102;
                }

                if (flag)
                {
                    ItemLoader.UseItem(item, player);
                    legacySoundStyle = item.UseSound;
                    int num2 = item.buffTime;
                    if (num2 == 0)
                        num2 = 3600;
                    player.AddBuff(num, num2);
                    if (item.consumable)
                    {
                        if (ItemLoader.ConsumeItem(item, player))
                            item.stack--;
                        if (item.stack <= 0)
                            item.TurnToAir();
                    }
                }
            }

            if (legacySoundStyle == null)
                return;
            Main.PlaySound(legacySoundStyle, player.position);
            FindRecipes();
        }

        public static void ApiQuickHeal(this Player player)
        {
            if (player.noItems)
                return;
            if (player.statLife == player.statLifeMax2 || player.potionDelay > 0)
                return;
            Item item = player.APIQuickHeal_GetItemToUse();
            if (item == null)
                return;
            Main.PlaySound(item.UseSound, player.position);
            if (item.potion)
            {
                if (item.type == 227)
                {
                    player.potionDelay = player.restorationDelayTime;
                    player.AddBuff(21, player.potionDelay);
                }
                else
                {
                    player.potionDelay = player.potionDelayTime;
                    player.AddBuff(21, player.potionDelay);
                }
            }

            ItemLoader.UseItem(item, player);
            player.statLife += item.healLife;
            player.statMana += item.healMana;
            if (player.statLife > player.statLifeMax2)
                player.statLife = player.statLifeMax2;
            if (player.statMana > player.statManaMax2)
                player.statMana = player.statManaMax2;
            if (item.healLife > 0 && Main.myPlayer == player.whoAmI)
                player.HealEffect(item.healLife);
            if (item.healMana > 0)
                if (Main.myPlayer == player.whoAmI)
                    player.ManaEffect(item.healMana);
            if (ItemLoader.ConsumeItem(item, player))
                item.stack--;
            if (item.stack <= 0)
                item.TurnToAir();
            FindRecipes();
        }

        public static Item APIQuickHeal_GetItemToUse(this Player player)
        {
            int num = player.statLifeMax2 - player.statLife;
            Item result = null;
            int num2 = -player.statLifeMax2;
            for (int i = 0; i < 58; i++)
            {
                Item item = player.inventory[i];
                if (item.stack <= 0 || item.type <= 0 || !item.potion || item.healLife <= 0 || !ItemLoader.CanUseItem(item, player))
                    continue;
                int num3 = item.healLife - num;
                if (num2 < 0)
                {
                    if (num3 <= num2)
                        continue;
                    result = item;
                    num2 = num3;
                }
                else if (num3 < num2 && num3 >= 0)
                {
                    result = item;
                    num2 = num3;
                }
            }

            PlayerCharacter character = player.GetModPlayer<PlayerCharacter>();
            for (int i = 0; i < character.Inventories.Length; i += 1)
                if (character.ActiveInvPage != i)
                    for (int j = 0; j < character.Inventories[i].Length; j += 1)
                    {
                        Item item = character.Inventories[i][j];
                        if (item.stack <= 0 || item.type <= 0 || !item.potion || item.healLife <= 0 || !ItemLoader.CanUseItem(item, player))
                            continue;
                        int num3 = item.healLife - num;
                        if (num2 < 0)
                        {
                            if (num3 <= num2)
                                continue;
                            result = item;
                            num2 = num3;
                        }
                        else if (num3 < num2 && num3 >= 0)
                        {
                            result = item;
                            num2 = num3;
                        }
                    }

            return result;
        }

        public static void ApiQuickMana(this Player player)
        {
            if (player.noItems)
                return;
            if (player.statMana == player.statManaMax2)
                return;
            for (int i = 0; i < 58; i++)
                APIQuickMana_TryItem(player, player.inventory[i]);

            PlayerCharacter character = player.GetModPlayer<PlayerCharacter>();
            for (int i = 0; i < character.Inventories.Length; i += 1)
                if (character.ActiveInvPage != i)
                    for (int j = 0; j < character.Inventories[i].Length; j += 1)
                        APIQuickMana_TryItem(player, character.Inventories[i][j]);
        }

        private static void APIQuickMana_TryItem(Player player, Item item)
        {
            if (item.stack <= 0 || item.type <= 0 || item.healMana <= 0 || player.potionDelay != 0 && item.potion || !ItemLoader.CanUseItem(item, player))
                return;
            Main.PlaySound(item.UseSound, player.position);
            if (item.potion)
            {
                if (item.type == 227)
                {
                    player.potionDelay = player.restorationDelayTime;
                    player.AddBuff(21, player.potionDelay);
                }
                else
                {
                    player.potionDelay = player.potionDelayTime;
                    player.AddBuff(21, player.potionDelay);
                }
            }

            ItemLoader.UseItem(item, player);
            player.statLife += item.healLife;
            player.statMana += item.healMana;
            if (player.statLife > player.statLifeMax2)
                player.statLife = player.statLifeMax2;
            if (player.statMana > player.statManaMax2)
                player.statMana = player.statManaMax2;
            if (item.healLife > 0 && Main.myPlayer == player.whoAmI)
                player.HealEffect(item.healLife);
            if (item.healMana > 0)
            {
                player.AddBuff(94, Player.manaSickTime);
                if (Main.myPlayer == player.whoAmI)
                    player.ManaEffect(item.healMana);
            }

            if (ItemLoader.ConsumeItem(item, player))
                item.stack--;
            if (item.stack <= 0)
                item.TurnToAir();
            FindRecipes();
        }

        public static int CoinStackValue(Item i)
        {
            int coins = 0;
            switch (i.type)
            {
                case ItemID.CopperCoin:
                    coins += i.stack;
                    break;
                case ItemID.SilverCoin:
                    coins += i.stack * 100;
                    break;
                case ItemID.GoldCoin:
                    coins += i.stack * 10000;
                    break;
                case ItemID.PlatinumCoin:
                    coins += i.stack * 1000000;
                    break;
            }

            return coins;
        }

        //This is just bad code, it's a circular reference that will never resolve.
        //public static bool Contains<T>(this IEnumerable<T> e, Predicate<T> match)
        //{
        //    List<T> values = e.ToList();
        //    return values.Contains(match);
        //}

        public static bool Contains<T>(this IEnumerable<T> e, T item)
        {
            List<T> values = e.ToList();
            return values.Contains(item);
        }

        public static int[] CountCoins(Item i)
        {
            int[] coins = new int[4];
            switch (i.type)
            {
                case 71:
                    coins[c] += i.stack;
                    break;
                case 72:
                    coins[s] += i.stack * 100;
                    break;
                case 73:
                    coins[g] += i.stack * 10000;
                    break;
                case 74:
                    coins[p] += i.stack * 1000000;
                    break;
            }

            return coins;
        }

        public static void CraftItem(Recipe r)
        {
            int stack = Main.mouseItem.stack;
            Main.mouseItem = r.createItem.Clone();
            Main.mouseItem.stack += stack;
            if (stack <= 0)
                Main.mouseItem.Prefix(-1);
            Main.mouseItem.position.X = Main.player[Main.myPlayer].position.X + Main.player[Main.myPlayer].width / 2f - Main.mouseItem.width / 2f;
            Main.mouseItem.position.Y = Main.player[Main.myPlayer].position.Y + Main.player[Main.myPlayer].height / 2f - Main.mouseItem.height / 2f;
            ItemText.NewText(Main.mouseItem, r.createItem.stack);
            r.ApiCreate();
            if (Main.mouseItem.type <= 0 && r.createItem.type <= 0)
                return;
            RecipeHooks.OnCraft(Main.mouseItem, r);
            ItemLoader.OnCraft(Main.mouseItem, r);
            //Main.PlaySound(7);
            SoundManager.PlaySound(Sounds.Grass);
        }

        public static void Draw(this SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Color color, float scale)
        {
            spriteBatch.Draw(texture, position, null, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public static void Draw(this SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float scale)
        {
            spriteBatch.Draw(texture, position, sourceRectangle, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        // private static FieldInfo _inventoryGlowHueChest = typeof(ItemSlot).GetField("inventoryGlowHueChest", BindingFlags.NonPublic | BindingFlags.Static);

        public static void DrawStringWithShadow(this SpriteBatch spriteBatch, DynamicSpriteFont font, string text, Vector2 position, Color color,
            float scale = 1f)
        {
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, position, color, 0f, Vector2.Zero, Vector2.One * scale);
        }

        public static void FindRecipes()
        {
            if (Main.netMode == 2) return;
            int num = Main.availableRecipe[Main.focusRecipe];
            float num2 = Main.availableRecipeY[Main.focusRecipe];
            for (int i = 0; i < Recipe.maxRecipes; i++)
                Main.availableRecipe[i] = 0;
            Main.numAvailableRecipes = 0;
            bool flag = Main.guideItem.type > 0 && Main.guideItem.stack > 0 && Main.guideItem.Name != "";
            if (flag)
            {
                for (int j = 0; j < Recipe.maxRecipes; j++)
                {
                    if (Main.recipe[j].createItem.type == 0)
                        break;
                    int num3 = 0;
                    while (num3 < Recipe.maxRequirements && Main.recipe[j].requiredItem[num3].type != 0)
                    {
                        if (Main.guideItem.IsTheSameAs(Main.recipe[j].requiredItem[num3]) ||
                            Main.recipe[j].useWood(Main.guideItem.type, Main.recipe[j].requiredItem[num3].type) ||
                            Main.recipe[j].useSand(Main.guideItem.type, Main.recipe[j].requiredItem[num3].type) ||
                            Main.recipe[j].useIronBar(Main.guideItem.type, Main.recipe[j].requiredItem[num3].type) ||
                            Main.recipe[j].useFragment(Main.guideItem.type, Main.recipe[j].requiredItem[num3].type) ||
                            Main.recipe[j].AcceptedByItemGroups(Main.guideItem.type, Main.recipe[j].requiredItem[num3].type) ||
                            Main.recipe[j].usePressurePlate(Main.guideItem.type, Main.recipe[j].requiredItem[num3].type))
                        {
                            Main.availableRecipe[Main.numAvailableRecipes] = j;
                            Main.numAvailableRecipes++;
                            break;
                        }

                        num3++;
                    }
                }
            }
            else
            {
                Dictionary<int, int> dictionary = new Dictionary<int, int>();
                Item item;
                Item[] inv = Main.player[Main.myPlayer].inventory;
                foreach (Item t in inv)
                {
                    item = t;
                    if (item.stack <= 0)
                        continue;
                    if (dictionary.ContainsKey(item.netID))
                    {
                        Dictionary<int, int> dictionary2;
                        int netId;
                        (dictionary2 = dictionary)[netId = item.netID] = dictionary2[netId] + item.stack;
                    }
                    else
                    {
                        dictionary[item.netID] = item.stack;
                    }
                }

                if (Main.player[Main.myPlayer].active)
                {
                    PlayerCharacter character = Main.player[Main.myPlayer].GetModPlayer<PlayerCharacter>();
                    for (int j = 0; j < character.Inventories.Length; j += 1)
                        if (j != character.ActiveInvPage)
                            foreach (Item i in character.Inventories[j])
                                if (dictionary.ContainsKey(i.netID))
                                {
                                    Dictionary<int, int> dictionary2;
                                    int netId;
                                    (dictionary2 = dictionary)[netId = i.netID] = dictionary2[netId] + i.stack;
                                }
                                else
                                {
                                    dictionary[i.netID] = i.stack;
                                }
                }

                Item[] array = new Item[0];
                if (Main.player[Main.myPlayer].chest != -1)
                {
                    if (Main.player[Main.myPlayer].chest > -1)
                        array = Main.chest[Main.player[Main.myPlayer].chest].item;
                    else
                        switch (Main.player[Main.myPlayer].chest)
                        {
                            //Inventory Bank Main I
                            case -2:
                                array = Main.player[Main.myPlayer].bank.item;
                                break;
                            //Inventory Bank Page II
                            case -3:
                                array = Main.player[Main.myPlayer].bank2.item;
                                break;
                            //Inventory Bank Page III
                            case -4:
                                array = Main.player[Main.myPlayer].bank3.item;
                                break;
                        }

                    for (int l = 0; l < 40; l++)
                    {
                        item = array[l];
                        if (item.stack <= 0)
                            continue;
                        if (dictionary.ContainsKey(item.netID))
                        {
                            Dictionary<int, int> dictionary3;
                            int netId2;
                            //todo, Ok, no idea what this code is doing, will need to figure it out
                            (dictionary3 = dictionary)[netId2 = item.netID] = dictionary3[netId2] + item.stack;
                        }
                        else
                        {
                            dictionary[item.netID] = item.stack;
                        }
                    }
                }

                int num4 = 0;
                while (num4 < Recipe.maxRecipes && Main.recipe[num4].createItem.type != 0)
                {
                    bool flag2 = true;
                    if (flag2)
                    {
                        int num5 = 0;
                        while (num5 < Recipe.maxRequirements && Main.recipe[num4].requiredTile[num5] != -1)
                        {
                            if (!Main.player[Main.myPlayer].adjTile[Main.recipe[num4].requiredTile[num5]])
                            {
                                flag2 = false;
                                break;
                            }

                            num5++;
                        }
                    }

                    if (flag2)
                        for (int m = 0; m < Recipe.maxRequirements; m++)
                        {
                            item = Main.recipe[num4].requiredItem[m];
                            if (item.type == 0)
                                break;
                            int num6 = item.stack;
                            bool flag3 = false;
                            foreach (int current in dictionary.Keys.Where(current =>
                                Main.recipe[num4].useWood(current, item.type) || Main.recipe[num4].useSand(current, item.type) ||
                                Main.recipe[num4].useIronBar(current, item.type) || Main.recipe[num4].useFragment(current, item.type) ||
                                Main.recipe[num4].AcceptedByItemGroups(current, item.type) || Main.recipe[num4].usePressurePlate(current, item.type)))
                            {
                                num6 -= dictionary[current];
                                flag3 = true;
                            }

                            if (!flag3 && dictionary.ContainsKey(item.netID))
                                num6 -= dictionary[item.netID];

                            if (num6 <= 0)
                                continue;
                            flag2 = false;
                            break;
                        }

                    if (flag2)
                    {
                        bool flag4 = !Main.recipe[num4].needWater || Main.player[Main.myPlayer].adjWater || Main.player[Main.myPlayer].adjTile[172];
                        bool flag5 = !Main.recipe[num4].needHoney || Main.recipe[num4].needHoney == Main.player[Main.myPlayer].adjHoney;
                        bool flag6 = !Main.recipe[num4].needLava || Main.recipe[num4].needLava == Main.player[Main.myPlayer].adjLava;
                        bool flag7 = !Main.recipe[num4].needSnowBiome || Main.player[Main.myPlayer].ZoneSnow;
                        if (!flag4 || !flag5 || !flag6 || !flag7)
                            flag2 = false;
                    }

                    if (flag2 && RecipeHooks.RecipeAvailable(Main.recipe[num4]))
                    {
                        Main.availableRecipe[Main.numAvailableRecipes] = num4;
                        Main.numAvailableRecipes++;
                    }

                    num4++;
                }
            }

            for (int n = 0; n < Main.numAvailableRecipes; n++)
            {
                if (num != Main.availableRecipe[n])
                    continue;
                Main.focusRecipe = n;
                break;
            }

            if (Main.focusRecipe >= Main.numAvailableRecipes)
                Main.focusRecipe = Main.numAvailableRecipes - 1;
            if (Main.focusRecipe < 0)
                Main.focusRecipe = 0;
            float num7 = Main.availableRecipeY[Main.focusRecipe] - num2;
            for (int num8 = 0; num8 < Recipe.maxRecipes; num8++)
                Main.availableRecipeY[num8] -= num7;
        }

        public static Item GetItem(this Player player, Item newItem)
        {
            int plr = player.whoAmI;
            PlayerCharacter character = player.GetModPlayer<PlayerCharacter>();
            bool flag = newItem.type >= 71 && newItem.type <= 74;
            Item item = newItem;
            int num = 50;
            if (newItem.noGrabDelay > 0)
                return item;
            int num2 = 0;
            if (newItem.uniqueStack && player.HasItem(newItem.type))
                return item;
            if (newItem.type == 71 || newItem.type == 72 || newItem.type == 73 || newItem.type == 74)
            {
                num2 = -4;
                num = 54;
            }

            if ((item.ammo > 0 || item.bait > 0) && !item.notAmmo || item.type == 530)
            {
                item = player.FillAmmo(plr, item);
                if (item.type == 0 || item.stack == 0)
                    return new Item();
            }

            for (int i = num2; i < 50; i++)
            {
                int num3 = i;
                if (num3 < 0)
                    num3 = 54 + i;
                Item x = player.inventory[num3];
                if (x.type <= 0 || x.stack <= 0 || x.stack >= x.maxStack || !item.IsTheSameAs(x))
                    continue;
                SoundManager.PlaySound(flag ? Sounds.Meowmere : Sounds.Grass, player.position);

                if (item.stack + x.stack <= x.maxStack)
                {
                    x.stack += item.stack;
                    ItemText.NewText(newItem, item.stack);
                    player.DoCoins(num3);
                    if (plr == Main.myPlayer)
                        FindRecipes();
                    AchievementsHelper.NotifyItemPickup(player, item);
                    return new Item();
                }

                AchievementsHelper.NotifyItemPickup(player, item, x.maxStack - x.stack);
                item.stack -= x.maxStack - x.stack;
                ItemText.NewText(newItem, x.maxStack - x.stack);
                x.stack = x.maxStack;
                player.DoCoins(num3);
                if (plr == Main.myPlayer)
                    FindRecipes();
            }

            for (int i = 0; i < character.Inventories.Length; i += 1)
            {
                if (character.ActiveInvPage == i)
                    continue;
                for (int j = 0; j < character.Inventories[i].Length; j += 1)
                {
                    Item x = character.Inventories[i][j];
                    if (x.type <= 0 || x.stack >= x.maxStack || !item.IsTheSameAs(x))
                        continue;
                    if (flag)
                    {
                        //Main.PlaySound(38, (int)player.position.X, (int)player.position.Y);
                        SoundManager.PlaySound(Sounds.Meowmere, player.position);
                    }

                    else
                    {
                        SoundManager.PlaySound(Sounds.Grass, player.position);
                        //Main.PlaySound(7, (int) player.position.X, (int) player.position.Y);
                    }

                    if (item.stack + x.stack <= x.maxStack)
                    {
                        x.stack += item.stack;
                        ItemText.NewText(newItem, item.stack);
                        AchievementsHelper.NotifyItemPickup(player, item);
                        return new Item();
                    }

                    AchievementsHelper.NotifyItemPickup(player, item, x.maxStack - x.stack);
                    item.stack -= x.maxStack - x.stack;
                    ItemText.NewText(newItem, x.maxStack - x.stack);
                    x.stack = x.maxStack;
                    if (plr == Main.myPlayer)
                        FindRecipes();
                }
            }

            if (newItem.type != 71 && newItem.type != 72 && newItem.type != 73 && newItem.type != 74 && newItem.useStyle > 0)
                for (int j = 0; j < 10; j++)
                {
                    if (player.inventory[j].type != 0)
                        continue;
                    player.inventory[j] = item;
                    ItemText.NewText(newItem, newItem.stack);
                    player.DoCoins(j);
                    //Main.PlaySound(flag ? 38 : 7, (int) player.position.X, (int) player.position.Y);
                    SoundManager.PlaySound(flag ? Sounds.Meowmere : Sounds.Grass, player.position);

                    if (plr == Main.myPlayer)
                        FindRecipes();
                    AchievementsHelper.NotifyItemPickup(player, item);
                    return new Item();
                }

            if ((newItem.modItem is Glyph || newItem.modItem is ProceduralItem || newItem.modItem is RangedWeapon) && kConfig.ClientSide.SmartInventory)
            {
                if (character.ActiveInvPage == 2)
                    for (int i = 10; i < 50; i += 1)
                    {
                        if (player.inventory[i].type != 0)
                            continue;
                        player.inventory[i] = item;
                        ItemText.NewText(newItem, newItem.stack);
                        //Main.PlaySound(flag ? 38 : 7, (int)player.position.X, (int)player.position.Y);
                        SoundManager.PlaySound(flag ? Sounds.Meowmere : Sounds.Grass, player.position);
                        AchievementsHelper.NotifyItemPickup(player, item);
                        return new Item();
                    }
                else
                    for (int i = 0; i < character.Inventories[2].Length; i += 1)
                    {
                        if (character.Inventories[2][i].type != 0)
                            continue;
                        character.Inventories[2][i] = item;
                        ItemText.NewText(newItem, newItem.stack);
                        //Main.PlaySound(flag ? 38 : 7, (int)player.position.X, (int)player.position.Y);
                        SoundManager.PlaySound(flag ? Sounds.Meowmere : Sounds.Grass, player.position);
                        AchievementsHelper.NotifyItemPickup(player, item);
                        return new Item();
                    }
            }

            if (newItem.favorited)
            {
                for (int k = 0; k < num; k++)
                {
                    if (player.inventory[k].type != 0 || kConfig.ClientSide.ManualInventory && character.ActiveInvPage != 0)
                        continue;
                    player.inventory[k] = item;
                    ItemText.NewText(newItem, newItem.stack);
                    player.DoCoins(k);
                    //Main.PlaySound(flag ? 38 : 7, (int)player.position.X, (int)player.position.Y);
                    SoundManager.PlaySound(flag ? Sounds.Meowmere : Sounds.Grass, player.position);
                    if (plr == Main.myPlayer)
                        FindRecipes();
                    AchievementsHelper.NotifyItemPickup(player, item);
                    return new Item();
                }
            }
            else
            {
                for (int l = num - 1; l >= 0; l--)
                {
                    if (player.inventory[l].type != 0 || kConfig.ClientSide.ManualInventory && character.ActiveInvPage != 0 && !flag)
                        continue;
                    player.inventory[l] = item;
                    ItemText.NewText(newItem, newItem.stack);
                    player.DoCoins(l);
                    //Main.PlaySound(flag ? 38 : 7, (int)player.position.X, (int)player.position.Y);
                    SoundManager.PlaySound(flag ? Sounds.Meowmere : Sounds.Grass, player.position);
                    if (plr == Main.myPlayer)
                        FindRecipes();
                    AchievementsHelper.NotifyItemPickup(player, item);
                    return new Item();
                }

                for (int i = 0; i < character.Inventories.Length; i += 1)
                {
                    if (character.ActiveInvPage == i)
                        continue;
                    for (int j = 0; j < character.Inventories[i].Length; j += 1)
                    {
                        if (character.Inventories[i][j].type != 0)
                            continue;
                        character.Inventories[i][j] = item;
                        ItemText.NewText(newItem, newItem.stack);
                        //Main.PlaySound(flag ? 38 : 7, (int)player.position.X, (int)player.position.Y);
                        SoundManager.PlaySound(flag ? Sounds.Meowmere : Sounds.Grass, player.position);
                        AchievementsHelper.NotifyItemPickup(player, item);
                        return new Item();
                    }
                }
            }

            return item;
        }

        private static void InvLogic(this Recipe recipe, Item[] array, Item requiredItem, int requiredAmount)
        {
            for (int k = 0; k < array.Length; k++)
            {
                Item item2 = array[k];
                if (requiredAmount <= 0)
                    break;

                if (!item2.IsTheSameAs(requiredItem) && !recipe.useWood(item2.type, requiredItem.type) && !recipe.useSand(item2.type, requiredItem.type) &&
                    !recipe.useFragment(item2.type, requiredItem.type) && !recipe.useIronBar(item2.type, requiredItem.type) &&
                    !recipe.usePressurePlate(item2.type, requiredItem.type) && !recipe.AcceptedByItemGroups(item2.type, requiredItem.type))
                    continue;
                if (item2.stack > requiredAmount)
                {
                    item2.stack -= requiredAmount;
                    requiredAmount = 0;
                }
                else
                {
                    requiredAmount -= item2.stack;
                    array[k] = new Item();
                }
            }
        }

        public static void ItemSlotDrawExtension(SpriteBatch spriteBatch, Item[] inv, int context, int slot, Vector2 position, Color overrideColor,
            Color lightColor = default, bool drawSelected = true)
        {
            Player player = Main.player[Main.myPlayer];
            Item item = inv[slot];
            float inventoryScale = Main.inventoryScale;
            Color color = Color.White;
            if (lightColor != Color.Transparent)
                color = lightColor;
            int num = -1;
            bool flag = false;
            int num2 = 0;
            if (PlayerInput.UsingGamepadUI)
            {
                switch (context)
                {
                    case 0:
                    case 1:
                    case 2:
                        num = slot;
                        break;
                    case 3:
                    case 4:
                        num = 400 + slot;
                        break;
                    case 5:
                        num = 303;
                        break;
                    case 6:
                        num = 300;
                        break;
                    case 7:
                        num = 1500;
                        break;
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                        num = 100 + slot;
                        break;
                    case 12:
                        if (inv == player.dye)
                            num = 120 + slot;
                        if (inv == player.miscDyes)
                            num = 185 + slot;
                        break;
                    case 15:
                        num = 2700 + slot;
                        break;
                    case 16:
                        num = 184;
                        break;
                    case 17:
                        num = 183;
                        break;
                    case 18:
                        num = 182;
                        break;
                    case 19:
                        num = 180;
                        break;
                    case 20:
                        num = 181;
                        break;
                    case 22:
                        if (UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig != -1)
                            num = 700 + UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeBig;
                        if (UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall != -1)
                            num = 1500 + UILinkPointNavigator.Shortcuts.CRAFT_CurrentRecipeSmall + 1;
                        break;
                }

                flag = UILinkPointNavigator.CurrentPoint == num;
                if (context == 0)
                {
                    int drawMode = player.DpadRadial.GetDrawMode(slot);
                    num2 = drawMode;
                    if (num2 > 0 && !PlayerInput.CurrentProfile.UsingDpadHotbar())
                        num2 = 0;
                }
            }

            Texture2D texture2D = Main.inventoryBackTexture;
            Color color2 = Main.inventoryBack;
            bool flag2 = false;
            if (item.type > 0 && item.stack > 0 && item.favorited && context != 13 && context != 21 && context != 22 && context != 14)
            {
                texture2D = Main.inventoryBack10Texture;
            }
            else if (item.type > 0 && item.stack > 0 && ItemSlot.Options.HighlightNewItems && item.newAndShiny && context != 13 && context != 21 &&
                     context != 14 && context != 22)
            {
                texture2D = Main.inventoryBack15Texture;
                float num3 = Main.mouseTextColor / 255f;
                num3 = num3 * 0.2f + 0.8f;
                color2.MultiplyRGBA(new Color(num3, num3, num3));
            }
            else if (PlayerInput.UsingGamepadUI && item.type > 0 && item.stack > 0 && num2 != 0 && context != 13 && context != 21 && context != 22)
            {
                texture2D = Main.inventoryBack15Texture;
                float num4 = Main.mouseTextColor / 255f;
                num4 = num4 * 0.2f + 0.8f;
                color2.MultiplyRGBA(num2 == 1 ? new Color(num4, num4 / 2f, num4 / 2f) : new Color(num4 / 2f, num4, num4 / 2f));
            }
            else
            {
                switch (context)
                {
                    case 0 when slot < 10:
                        texture2D = Main.inventoryBack9Texture;
                        break;
                    case 10:
                    case 8:
                    case 16:
                    case 17:
                    case 19:
                    case 18:
                    case 20:
                        texture2D = Main.inventoryBack3Texture;
                        break;
                    case 11:
                    case 9:
                        texture2D = Main.inventoryBack8Texture;
                        break;
                    case 12:
                        texture2D = Main.inventoryBack12Texture;
                        break;
                    case 3:
                        texture2D = Main.inventoryBack5Texture;
                        break;
                    case 4:
                        texture2D = Main.inventoryBack2Texture;
                        break;
                    case 7:
                    case 5:
                        texture2D = Main.inventoryBack4Texture;
                        break;
                    case 6:
                        texture2D = Main.inventoryBack7Texture;
                        break;
                    case 13:
                        {
                            byte b = 200;
                            if (slot == Main.player[Main.myPlayer].selectedItem)
                            {
                                texture2D = Main.inventoryBack14Texture;
                                b = 255;
                            }

                            new Color(b, b, b, b);
                            break;
                        }
                    case 14:
                    case 21:
                        flag2 = true;
                        break;
                    case 15:
                        texture2D = Main.inventoryBack6Texture;
                        break;
                    case 22:
                        texture2D = Main.inventoryBack4Texture;
                        break;
                }
            }

            if (context == 0 && ((int[])InventoryGlowTime.GetValue(null))[slot] > 0 && !inv[slot].favorited)
            {
                float scale = Main.invAlpha / 255f;
                Color value = new Color(63, 65, 151, 255) * scale;
                Color value2 = Main.hslToRgb(((float[])InventoryGlowHue.GetValue(null))[slot], 1f, 0.5f) * scale;
                float num5 = ((int[])InventoryGlowTime.GetValue(null))[slot] / 300f;
                num5 *= num5;
                Color.Lerp(value, value2, num5 / 2f);
                texture2D = Main.inventoryBack13Texture;
            }

            if ((context == 4 || context == 3) && ((int[])InventoryGlowTimeChest.GetValue(null))[slot] > 0 && !inv[slot].favorited)
            {
                float scale2 = Main.invAlpha / 255f;
                Color value3 = new Color(130, 62, 102, 255) * scale2;
                if (context == 3)
                    value3 = new Color(104, 52, 52, 255) * scale2;
                Color value4 = Main.hslToRgb(((float[])InventoryGlowHue.GetValue(null))[slot], 1f, 0.5f) * scale2;
                float num6 = ((int[])InventoryGlowTimeChest.GetValue(null))[slot] / 300f;
                num6 *= num6;
                Color.Lerp(value3, value4, num6 / 2f);
                texture2D = Main.inventoryBack13Texture;
            }

            if (flag)
            {
                texture2D = Main.inventoryBack14Texture;
                color2 = Color.White;
            }

            if (!flag2)
                spriteBatch.Draw(slot == Main.player[Main.myPlayer].selectedItem && drawSelected ? Main.inventoryBack14Texture : texture2D, position, null,
                    overrideColor, 0f, default, inventoryScale, SpriteEffects.None, 0f);
            int num7 = -1;
            switch (context)
            {
                case 8:
                    switch (slot)
                    {
                        case 0:
                            num7 = 0;
                            break;
                        case 1:
                            num7 = 6;
                            break;
                        case 2:
                            num7 = 12;
                            break;
                    }

                    break;
                case 9:
                    switch (slot)
                    {
                        case 10:
                            num7 = 3;
                            break;
                        case 11:
                            num7 = 9;
                            break;
                        case 12:
                            num7 = 15;
                            break;
                    }

                    break;
                case 10:
                    num7 = 11;
                    break;
                case 11:
                    num7 = 2;
                    break;
                case 12:
                    num7 = 1;
                    break;
                case 16:
                    num7 = 4;
                    break;
                case 17:
                    num7 = 13;
                    break;
                case 18:
                    num7 = 7;
                    break;
                case 19:
                    num7 = 10;
                    break;
                case 20:
                    num7 = 17;
                    break;
            }

            if ((item.type <= 0 || item.stack <= 0) && num7 != -1)
            {
                Texture2D texture2D2 = Main.extraTexture[54];
                Rectangle rectangle = texture2D2.Frame(3, 6, num7 % 3, num7 / 3);
                rectangle.Width -= 2;
                rectangle.Height -= 2;
                spriteBatch.Draw(texture2D2, position + texture2D.Size() / 2f * inventoryScale, rectangle, Color.White * 0.35f, 0f, rectangle.Size() / 2f,
                    inventoryScale, SpriteEffects.None, 0f);
            }

            Vector2 vector = texture2D.Size() * inventoryScale;
            if (item.type > 0 && item.stack > 0)
            {
                Texture2D texture2D3 = item.modItem is ProceduralItem ? ((ProceduralItem)item.modItem).LocalTexture : Main.itemTexture[item.type];
                Rectangle rectangle2;
                rectangle2 = Main.itemAnimations[item.type] != null ? Main.itemAnimations[item.type].GetFrame(texture2D3) : texture2D3.Frame();
                Color newColor = color;
                float num8 = 1f;
                ItemSlot.GetItemLight(ref newColor, ref num8, item);
                float num9 = 1f;
                if (rectangle2.Width > 32 || rectangle2.Height > 32)
                {
                    if (rectangle2.Width > rectangle2.Height)
                        num9 = 32f / rectangle2.Width;
                    else
                        num9 = 32f / rectangle2.Height;
                }

                num9 *= inventoryScale;
                Vector2 position2 = position + vector / 2f - rectangle2.Size() * num9 / 2f;
                Vector2 origin = rectangle2.Size() * (num8 / 2f - 0.5f);
                if (ItemLoader.PreDrawInInventory(item, spriteBatch, position2, rectangle2, item.GetAlpha(newColor), item.GetColor(color), origin, num9 * num8))
                {
                    spriteBatch.Draw(texture2D3, position2, rectangle2, item.GetAlpha(newColor), 0f, origin, num9 * num8, SpriteEffects.None, 0f);
                    if (item.color != Color.Transparent)
                        spriteBatch.Draw(texture2D3, position2, rectangle2, item.GetColor(color), 0f, origin, num9 * num8, SpriteEffects.None, 0f);
                }

                ItemLoader.PostDrawInInventory(item, spriteBatch, position2, rectangle2, item.GetAlpha(newColor), item.GetColor(color), origin, num9 * num8);
                if (ItemID.Sets.TrapSigned[item.type])
                    spriteBatch.Draw(Main.wireTexture, position + new Vector2(40f, 40f) * inventoryScale, new Rectangle(4, 58, 8, 8), color, 0f,
                        new Vector2(4f), 1f, SpriteEffects.None, 0f);
                if (item.stack > 1)
                    ChatManager.DrawColorCodedStringWithShadow(spriteBatch, Main.fontItemStack, item.stack.ToString(),
                        position + new Vector2(10f, 26f) * inventoryScale, color, 0f, Vector2.Zero, new Vector2(inventoryScale), -1f, inventoryScale);
                int num10 = -1;
                if (context == 13)
                {
                    if (item.DD2Summon)
                    {
                        for (int i = 0; i < 58; i++)
                            if (inv[i].type == 3822)
                                num10 += inv[i].stack;
                        if (num10 >= 0)
                            num10++;
                    }

                    if (item.useAmmo > 0)
                    {
                        int useAmmo = item.useAmmo;
                        num10 = 0;
                        for (int j = 0; j < 58; j++)
                            if (inv[j].ammo == useAmmo)
                                num10 += inv[j].stack;
                    }

                    if (item.fishingPole > 0)
                    {
                        num10 = 0;
                        for (int k = 0; k < 58; k++)
                            if (inv[k].bait > 0)
                                num10 += inv[k].stack;
                    }

                    if (item.tileWand > 0)
                    {
                        int tileWand = item.tileWand;
                        num10 = 0;
                        for (int l = 0; l < 58; l++)
                            if (inv[l].type == tileWand)
                                num10 += inv[l].stack;
                    }

                    if (item.type == 509 || item.type == 851 || item.type == 850 || item.type == 3612 || item.type == 3625 || item.type == 3611)
                    {
                        num10 = 0;
                        for (int m = 0; m < 58; m++)
                            if (inv[m].type == 530)
                                num10 += inv[m].stack;
                    }
                }

                if (num10 != -1)
                    ChatManager.DrawColorCodedStringWithShadow(spriteBatch, Main.fontItemStack, num10.ToString(),
                        position + new Vector2(8f, 30f) * inventoryScale, color, 0f, Vector2.Zero, new Vector2(inventoryScale * 0.8f), -1f, inventoryScale);
                if (context == 13)
                {
                    string text = string.Concat(slot + 1);
                    if (text == "10")
                        text = "0";
                    ChatManager.DrawColorCodedStringWithShadow(spriteBatch, Main.fontItemStack, text, position + new Vector2(8f, 4f) * inventoryScale, color,
                        0f, Vector2.Zero, new Vector2(inventoryScale), -1f, inventoryScale);
                }

                if (context == 13 && item.potion)
                {
                    Vector2 position3 = position + texture2D.Size() * inventoryScale / 2f - Main.cdTexture.Size() * inventoryScale / 2f;
                    Color color3 = item.GetAlpha(color) * (player.potionDelay / (float)player.potionDelayTime);
                    spriteBatch.Draw(Main.cdTexture, position3, null, color3, 0f, default, num9, SpriteEffects.None, 0f);
                }

                if ((context == 10 || context == 18) && item.expertOnly && !Main.expertMode)
                {
                    Vector2 position4 = position + texture2D.Size() * inventoryScale / 2f - Main.cdTexture.Size() * inventoryScale / 2f;
                    Color white = Color.White;
                    spriteBatch.Draw(Main.cdTexture, position4, null, white, 0f, default, num9, SpriteEffects.None, 0f);
                }
            }
            else if (context == 6)
            {
                Texture2D trashTexture = Main.trashTexture;
                Vector2 position5 = position + texture2D.Size() * inventoryScale / 2f - trashTexture.Size() * inventoryScale / 2f;
                spriteBatch.Draw(trashTexture, position5, null, new Color(100, 100, 100, 100), 0f, default, inventoryScale, SpriteEffects.None, 0f);
            }

            if (context == 0 && slot < 10)
            {
                string text2 = string.Concat(slot + 1);
                if (text2 == "10")
                    text2 = "0";
                Color inventoryBack = Main.inventoryBack;
                int num12 = 0;
                if (Main.player[Main.myPlayer].selectedItem == slot)
                {
                    num12 -= 3;
                    inventoryBack.R = 255;
                    inventoryBack.B = 0;
                    inventoryBack.G = 210;
                    inventoryBack.A = 100;
                }

                ChatManager.DrawColorCodedStringWithShadow(spriteBatch, Main.fontItemStack, text2, position + new Vector2(6f, 4 + num12) * inventoryScale,
                    inventoryBack, 0f, Vector2.Zero, new Vector2(inventoryScale), -1f, inventoryScale);
            }

            if (num != -1)
                UILinkPointNavigator.SetPosition(num, position + vector * 0.75f);
        }

        public static string MoneyToString(int amount)
        {
            string output = "";
            int[] coins = SeparateCoinTypes(amount);

            if (coins[p] > 0)
                output += coins[p] + " platinum ";
            if (coins[g] > 0)
                output += coins[g] + " gold ";
            if (coins[s] > 0)
                output += coins[s] + " silver ";
            if (coins[c] > 0)
                output += coins[c] + " copper ";

            return output;
        }

        public static void NinjaDodge(this Player player, int time, bool factorLongImmune = true)
        {
            player.immune = true;
            player.immuneTime = time;
            if (player.longInvince && factorLongImmune)
                player.immuneTime *= 2;
            for (int i = 0; i < player.hurtCooldowns.Length; i++)
                player.hurtCooldowns[i] = player.immuneTime;
            for (int j = 0; j < 100; j++)
            {
                int num = Dust.NewDust(new Vector2(player.position.X, player.position.Y), player.width, player.height, 31, 0f, 0f, 100, default, 2f);
                Dust dust = Main.dust[num];
                dust.position.X = dust.position.X + Main.rand.Next(-20, 21);
                Dust dust2 = Main.dust[num];
                dust2.position.Y = dust2.position.Y + Main.rand.Next(-20, 21);
                Main.dust[num].velocity *= 0.4f;
                Main.dust[num].scale *= 0.8f + Main.rand.Next(40) * 0.01f;
                Main.dust[num].shader = GameShaders.Armor.GetSecondaryShader(player.cWaist, player);
                if (Main.rand.Next(2) != 0)
                    continue;
                Main.dust[num].scale *= 1f + Main.rand.Next(40) * 0.01f;
                Main.dust[num].noGravity = true;
            }

            int num2 = Gore.NewGore(
                new Vector2(player.position.X + (float)(player.width / 2.0) - 24f, player.position.Y + (float)(player.height / 2.0) - 24f), default,
                Main.rand.Next(61, 64));
            //Main.gore[num2].scale = 1.5f;
            Main.gore[num2].velocity.X = Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity.Y = Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity *= 0.4f;
            num2 = Gore.NewGore(new Vector2(player.position.X + (float)(player.width / 2.0) - 24f, player.position.Y + (float)(player.height / 2.0) - 24f),
                default, Main.rand.Next(61, 64));
            //Main.gore[num2].scale = 1.5f;
            Main.gore[num2].velocity.X = 1.5f + Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity.Y = 1.5f + Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity *= 0.4f;
            num2 = Gore.NewGore(new Vector2(player.position.X + (float)(player.width / 2.0) - 24f, player.position.Y + (float)(player.height / 2.0) - 24f),
                default, Main.rand.Next(61, 64));
            //Main.gore[num2].scale = 1.5f;
            Main.gore[num2].velocity.X = -1.5f - Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity.Y = 1.5f + Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity *= 0.4f;
            num2 = Gore.NewGore(new Vector2(player.position.X + (float)(player.width / 2.0) - 24f, player.position.Y + (float)(player.height / 2.0) - 24f),
                default, Main.rand.Next(61, 64));
            //Main.gore[num2].scale = 1.5f;
            Main.gore[num2].velocity.X = 1.5f + Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity.Y = -1.5f - Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity *= 0.4f;
            num2 = Gore.NewGore(new Vector2(player.position.X + (float)(player.width / 2.0) - 24f, player.position.Y + (float)(player.height / 2.0) - 24f),
                default, Main.rand.Next(61, 64));
            //Main.gore[num2].scale = 1.5f;
            Main.gore[num2].velocity.X = -1.5f - Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity.Y = -1.5f - Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity *= 0.4f;
            if (player.whoAmI == Main.myPlayer)
                NetMessage.SendData(62, -1, -1, null, player.whoAmI, 1f);
        }

        public static void NinjaDodge(this NPC npc, Entity dustPos, int time, bool factorLongImmune = true)
        {
            npc.GetGlobalNPC<kNPC>().ImmuneTime = time;
            for (int j = 0; j < 100; j++)
            {
                int num = Dust.NewDust(new Vector2(dustPos.position.X, dustPos.position.Y), dustPos.width, dustPos.height, 31, 0f, 0f, 152, default, 2f);
                Dust dust = Main.dust[num];
                dust.position.X = dust.position.X + Main.rand.Next(-20, 21);
                Dust dust2 = Main.dust[num];
                dust2.position.Y = dust2.position.Y + Main.rand.Next(-20, 21);
                Main.dust[num].velocity *= 0.4f;
                Main.dust[num].scale *= 0.7f + Main.rand.Next(30) * 0.01f;
                if (Main.rand.Next(2) != 0)
                    continue;
                Main.dust[num].scale *= 1f + Main.rand.Next(40) * 0.01f;
                Main.dust[num].noGravity = true;
            }

            int num2 = Gore.NewGore(new Vector2(dustPos.position.X + dustPos.width / 2f - 24f, dustPos.position.Y + dustPos.height / 2f - 24f), default,
                Main.rand.Next(61, 64));
            Main.gore[num2].scale = 0.8f;
            Main.gore[num2].velocity.X = Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity.Y = Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity *= 0.4f;
            num2 = Gore.NewGore(new Vector2(dustPos.position.X + dustPos.width / 2f - 24f, dustPos.position.Y + dustPos.height / 2f - 24f), default,
                Main.rand.Next(61, 64));
            Main.gore[num2].scale = 0.8f;
            Main.gore[num2].velocity.X = 1.5f + Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity.Y = 1.5f + Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity *= 0.4f;
            num2 = Gore.NewGore(new Vector2(dustPos.position.X + dustPos.width / 2f - 24f, dustPos.position.Y + dustPos.height / 2f - 24f), default,
                Main.rand.Next(61, 64));
            Main.gore[num2].scale = 0.8f;
            Main.gore[num2].velocity.X = -1.5f - Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity.Y = 1.5f + Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity *= 0.4f;
            num2 = Gore.NewGore(new Vector2(dustPos.position.X + dustPos.width / 2f - 24f, dustPos.position.Y + dustPos.height / 2f - 24f), default,
                Main.rand.Next(61, 64));
            Main.gore[num2].scale = 0.8f;
            Main.gore[num2].velocity.X = 1.5f + Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity.Y = -1.5f - Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity *= 0.4f;
            num2 = Gore.NewGore(new Vector2(dustPos.position.X + dustPos.width / 2f - 24f, dustPos.position.Y + dustPos.height / 2f - 24f), default,
                Main.rand.Next(61, 64));
            Main.gore[num2].scale = 0.8f;
            Main.gore[num2].velocity.X = -1.5f - Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity.Y = -1.5f - Main.rand.Next(-50, 51) * 0.01f;
            Main.gore[num2].velocity *= 0.4f;
        }

        public static TV Random<TK, TV>(this Dictionary<TK, TV> dictionary)
        {
            return dictionary.Values.ToList().Random();
        }

        public static T Random<T>(this IEnumerable<T> e)
        {
            List<T> values = e.ToList();
            return values[Main.rand.Next(values.Count)];
        }

        public static void RemoveCoins(this Player player, int amount)
        {
            int[] coinType = { 71, 72, 73, 74 };

            //splitting the cost into individual coin types
            int[] cost = SeparateCoinTypes(amount);

            int[] coins = new int[4];
            for (int i = 0; i < coins.Length; i++)
                coins[i] = 0;

            coins = SeparateCoinTypes(player.Wealth());

            PlayerCharacter character = player.GetModPlayer<PlayerCharacter>();
            //foreach (Item[] inventory in character.inventories)
            //    foreach (Item i in inventory)
            //        coins = SumCoins(coins, CountCoins(i));

            //foreach (Item i in player.bank.item)
            //    coins = SumCoins(coins, CountCoins(i));

            //foreach (Item i in player.bank2.item)
            //    coins = SumCoins(coins, CountCoins(i));

            //foreach (Item i in player.bank3.item)
            //    coins = SumCoins(coins, CountCoins(i));

            for (int i = 0; i < 4; i++)
                if (coins[i] >= cost[i])
                {
                    foreach (Item item in player.inventory)
                        item.stack = RemoveCoins(item, coinType[i], ref cost[i]);

                    for (int j = 0; j < character.Inventories.Length; j += 1)
                        if (character.ActiveInvPage != j)
                            foreach (Item item in character.Inventories[j])
                                item.stack = RemoveCoins(item, coinType[i], ref cost[i]);

                    foreach (Item item in player.bank.item)
                        item.stack = RemoveCoins(item, coinType[i], ref cost[i]);

                    foreach (Item item in player.bank2.item)
                        item.stack = RemoveCoins(item, coinType[i], ref cost[i]);

                    foreach (Item item in player.bank3.item)
                        item.stack = RemoveCoins(item, coinType[i], ref cost[i]);
                }

                else
                {
                    cost[i + 1] += 1;
                    cost[i] -= 100;
                    Item.NewItem((int)player.position.X, (int)player.position.Y, 0, 0, coinType[i], -cost[i], true, 0, true);
                }
        }

        public static int RemoveCoins(Item item, int coinType, ref int amount)
        {
            int stackSize = item.stack;
            if (item.type != coinType)
                return stackSize;
            if (stackSize >= amount)
            {
                stackSize -= amount;
                amount = 0;
            }
            else
            {
                amount -= item.stack;
                stackSize = 0;
            }

            return stackSize;
        }

        public static int[] SeparateCoinTypes(int amount)
        {
            //splitting the cost into individual coin types
            int[] output = new int[4];
            output[c] = Math.Max(0, amount % 100);
            output[s] = Math.Max(0, (amount % 10000 - output[c]) / 100);
            output[g] = Math.Max(0, (amount % 1000000 - output[c] - output[s]) / 10000);
            output[p] = Math.Max(0, (amount - output[c] - output[s] - output[g]) / 1000000);

            return output;
        }

        public static void SetItemDefaults(this Item item, int type, bool noMatCheck = false, bool createModItem = true)
        {
            try
            {
                object globalItems = GlobalItemsProperty?.GetValue(item);
                item.ClearNameOverride();
                if (Main.netMode == 1 || Main.netMode == 2)
                    item.owner = 255;
                else
                    item.owner = Main.myPlayer;
                item.ResetStats(type);
                if (item.type == 0)
                {
                    item.netID = 0;
                    item.stack = 0;
                }
                else if (item.type <= 1000)
                {
                    item.SetDefaults1(item.type);
                }
                else if (item.type <= 2001)
                {
                    item.SetDefaults2(item.type);
                }
                else if (item.type <= 3000)
                {
                    item.SetDefaults3(item.type);
                }
                else
                {
                    item.SetDefaults4(item.type);
                }

                item.dye = (byte)GameShaders.Armor.GetShaderIdFromItemId(item.type);
                if (item.hairDye != 0)
                    item.hairDye = GameShaders.Hair.GetShaderIdFromItemId(item.type);
                switch (item.type)
                {
                    case 2015:
                        item.value = Item.sellPrice(0, 0, 5);
                        break;
                    case 2016:
                    case 2017:
                        item.value = Item.sellPrice(0, 0, 7, 50);
                        break;
                    case 2019:
                    case 2018:
                    case 3563:
                        item.value = Item.sellPrice(0, 0, 5);
                        break;
                    case 261:
                        item.value = Item.sellPrice(0, 0, 7, 50);
                        break;
                    case 2205:
                        item.value = Item.sellPrice(0, 0, 12, 50);
                        break;
                    case 2123:
                    case 2122:
                        item.value = Item.sellPrice(0, 0, 7, 50);
                        break;
                    case 2003:
                        item.value = Item.sellPrice(0, 0, 20);
                        break;
                    case 2156:
                    case 2157:
                    case 2121:
                        item.value = Item.sellPrice(0, 0, 15);
                        break;
                    case 1992:
                        item.value = Item.sellPrice(0, 0, 3);
                        break;
                    case 2004:
                    case 2002:
                        item.value = Item.sellPrice(0, 0, 5);
                        break;
                    case 2740:
                        item.value = Item.sellPrice(0, 0, 2, 50);
                        break;
                    case 2006:
                    case 3191:
                        item.value = Item.sellPrice(0, 0, 20);
                        break;
                    case 3192:
                        item.value = Item.sellPrice(0, 0, 2, 50);
                        break;
                    case 3193:
                        item.value = Item.sellPrice(0, 0, 5);
                        break;
                    case 3194:
                        item.value = Item.sellPrice(0, 0, 10);
                        break;
                    case 2007:
                        item.value = Item.sellPrice(0, 0, 50);
                        break;
                    case 2673:
                        item.value = Item.sellPrice(0, 10);
                        break;
                }

                if (item.bait > 0)
                {
                    if (item.bait >= 50)
                        item.rare = 3;
                    else if (item.bait >= 30)
                        item.rare = 2;
                    else if (item.bait >= 15)
                        item.rare = 1;
                }

                if (item.type >= 1994 && item.type <= 2001)
                {
                    int num = item.type - 1994;
                    switch (num)
                    {
                        case 0:
                            item.value = Item.sellPrice(0, 0, 5);
                            break;
                        case 4:
                            item.value = Item.sellPrice(0, 0, 10);
                            break;
                        case 6:
                            item.value = Item.sellPrice(0, 0, 15);
                            break;
                        case 3:
                            item.value = Item.sellPrice(0, 0, 20);
                            break;
                        case 7:
                            item.value = Item.sellPrice(0, 0, 30);
                            break;
                        case 2:
                            item.value = Item.sellPrice(0, 0, 40);
                            break;
                        case 1:
                            item.value = Item.sellPrice(0, 0, 75);
                            break;
                        case 5:
                            item.value = Item.sellPrice(0, 1);
                            break;
                    }
                }

                switch (item.type)
                {
                    case 483:
                    case 1192:
                    case 482:
                    case 1185:
                    case 484:
                    case 1199:
                    case 368:
                        item.autoReuse = true;
                        item.damage = (int)(item.damage * 1.15);
                        break;
                    case 2663:
                    case 1720:
                    case 2137:
                    case 2155:
                    case 2151:
                    case 1704:
                    case 2143:
                    case 1710:
                    case 2238:
                    case 2133:
                    case 2147:
                    case 2405:
                    case 1716:
                    case 1705:
                        item.value = Item.sellPrice(0, 2);
                        break;
                }

                if (Main.projHook[item.shoot])
                {
                    item.useStyle = 0;
                    item.useTime = 0;
                    item.useAnimation = 0;
                }

                if (item.type >= 1803 && item.type <= 1807)
                    item.SetDefaults(1533 + item.type - 1803, true);
                if (item.dye > 0)
                    item.maxStack = 99;
                if (item.createTile == 19)
                    item.maxStack = 999;
                item.netID = item.type;

                if ((bool)IsModItem.Invoke(null, new object[] { item.type }) && createModItem)
                    ModItem.SetValue(item, ItemLoader.GetItem(item.type).NewInstance(item));

                item.modItem?.AutoDefaults();
                //Removing references to SetDefaults
                //item.modItem?.SetDefaults();

                if (!noMatCheck)
                    item.checkMat();
                item.RebuildTooltip();
                if (item.type > 0 && ItemID.Sets.Deprecated[item.type])
                {
                    item.netID = 0;
                    item.type = 0;
                    item.stack = 0;
                }



                GlobalItemsProperty?.SetValue(item, globalItems);
            }
            catch (SystemException e)
            {
                Main.NewText(e.ToString());
            }
        }


        public static int Wealth(this Player player)
        {
            PlayerCharacter character = player.GetModPlayer<PlayerCharacter>();

            int coins = player.inventory.Sum(CoinStackValue);

            for (int i = 0; i < character.Inventories.Length; i += 1)
                if (character.ActiveInvPage != i)
                    coins += character.Inventories[i].Sum(CoinStackValue);

            coins += player.bank.item.Sum(CoinStackValue);

            coins += player.bank2.item.Sum(CoinStackValue);

            coins += player.bank3.item.Sum(CoinStackValue);

            return coins;
        }
    }
}