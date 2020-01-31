﻿//  Fairfield Tek L.L.C.
//  Copyright (c) 2016, Fairfield Tek L.L.C.
// 
// 
// THIS SOFTWARE IS PROVIDED BY FairfieldTek LLC ''AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES,
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
// PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL FAIRFIELDTEK LLC BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
// OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
// DAMAGE.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace kRPG2.GUI
{
    public class StatusBar : BaseGui
    {
        private const int BarLifeLength = 302;
        private const int BarLifeThickness = 28;

        // ReSharper disable once IdentifierTypo
        private const int BarManaLength = 286;

        // ReSharper disable once IdentifierTypo
        private const int BarManaThickness = 18;
        private const int BarXpLength = 138;
        private const int BarXpThickness = 6;
        private const int BubblesLength = 132;
        private const int BubblesThickness = 22;

        private static readonly MethodInfo DrawBuffIcon = typeof(Main).GetMethod("DrawBuffIcon", BindingFlags.NonPublic | BindingFlags.Static);

        //private Vector2 buffposition => new Vector2(Main.playerInventory ? 560f : 24f, Main.playerInventory ? 16f : 80f);

        private readonly Vector2 barLifeOrigin;

        // ReSharper disable once IdentifierTypo
        private readonly Vector2 barManaOrigin;

        private readonly Vector2 barXpOrigin;

        private readonly Vector2 bubblesOrigin;
        private PlayerCharacter character;

        public StatusBar(PlayerCharacter character, Mod mod)
        {
            this.character = character;
            barLifeOrigin = new Vector2(278f, 50f);
            barManaOrigin = new Vector2(254f, 92f);
            barXpOrigin = new Vector2(370f, 122f);
            bubblesOrigin = new Vector2(284, 134);

            AddButton(
                () => new Rectangle((int) pointsOrigin.X, (int) pointsOrigin.Y, (int) (GFX.UnspentPoints.Width * Scale),
                    (int) (GFX.UnspentPoints.Height * Scale)), delegate(Player player)
                {
                    character.CloseGuIs();
                    Main.PlaySound(SoundID.MenuTick);
                    character.LevelGui.GuiActive = player.GetModPlayer<PlayerCharacter>().UnspentPoints() && !Main.playerInventory;
                }, delegate
                {
                    Main.LocalPlayer.mouseInterface = true;
                    string s = Main.player[Main.myPlayer].GetModPlayer<PlayerCharacter>().UnspentPoints()
                        ? "Click here to allocate stat points"
                        : "You have no unspent stat points";
                    Main.instance.MouseText(s);
                });
        }

        private static Vector2 GuiPosition => new Vector2(4f, 6f) * Scale;

        private static Vector2 pointsOrigin => GuiPosition + new Vector2(242f, 112f) * Scale;

        private static float Scale => Math.Min(1f, Main.screenWidth / Constants.MaxScreenWidth + 0.4f);

        public static void DrawBuffs()
        {
            int num = -1;
            int num2 = 11;
            for (int i = 0; i < 22; i++)
                if (Main.player[Main.myPlayer].buffType[i] > 0)
                {
                    int b = Main.player[Main.myPlayer].buffType[i];
                    int x = 320 + i * 38;
                    int num3 = 8;
                    if (i >= num2)
                    {
                        x = 32 + (i - num2) * 38;
                        num3 += 50;
                    }

                    num = (int) DrawBuffIcon.Invoke(null, new object[] {num, i, b, x, num3}); // Main.DrawBuffIcon(num, i, b, x, num3);
                }
                else
                {
                    Main.buffAlpha[i] = 0.4f;
                }

            if (num < 0)
                return;
            int num4 = Main.player[Main.myPlayer].buffType[num];
            if (num4 <= 0)
                return;
            Main.buffString = Lang.GetBuffDescription(num4);
            int itemRarity = 0;
            switch (num4)
            {
                case 26 when Main.expertMode:
                    Main.buffString = Language.GetTextValue("BuffDescription.WellFed_Expert");
                    break;
                case 147:
                    Main.bannerMouseOver = true;
                    break;
                case 94:
                {
                    int num5 = (int) (Main.player[Main.myPlayer].manaSickReduction * 100f) + 1;
                    Main.buffString = Main.buffString + num5 + "%";
                    break;
                }
            }

            if (Main.meleeBuff[num4])
                itemRarity = -10;
            BuffLoader.ModifyBuffTip(num4, ref Main.buffString, ref itemRarity);
            Main.instance.MouseTextHackZoom(Lang.GetBuffName(num4), itemRarity);
        }

        private void DrawHotbar()
        {
            string text = "";
            if (!string.IsNullOrEmpty(Main.player[Main.myPlayer].inventory[Main.player[Main.myPlayer].selectedItem].Name))
                text = Main.player[Main.myPlayer].inventory[Main.player[Main.myPlayer].selectedItem].AffixName();
            var vector = Main.fontMouseText.MeasureString(text) / 2;
            Main.spriteBatch.DrawStringWithShadow(Main.fontMouseText, text, new Vector2(Main.screenWidth - 240 - vector.X - 16f, 0f),
                new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor));
            int posX = Main.screenWidth - 480;
            for (int i = 0; i < 10; i++)
            {
                if (i == Main.player[Main.myPlayer].selectedItem)
                {
                    if (Main.hotbarScale[i] < 1f)
                        Main.hotbarScale[i] += 0.05f;
                }
                else if (Main.hotbarScale[i] > 0.75)
                {
                    Main.hotbarScale[i] -= 0.05f;
                }

                float num2 = Main.hotbarScale[i];
                int num3 = (int) (20f + 22f * (1f - num2));
                int a = (int) (75f + 150f * num2);
                new Color(255, 255, 255, a);
                if (!Main.player[Main.myPlayer].hbLocked && !PlayerInput.IgnoreMouseInterface && Main.mouseX >= posX &&
                    Main.mouseX <= posX + Main.inventoryBackTexture.Width * Main.hotbarScale[i] && Main.mouseY >= num3 &&
                    Main.mouseY <= num3 + Main.inventoryBackTexture.Height * Main.hotbarScale[i] && !Main.player[Main.myPlayer].channel)
                {
                    Main.player[Main.myPlayer].mouseInterface = true;
                    Main.player[Main.myPlayer].showItemIcon = false;
                    if (Main.mouseLeft && !Main.player[Main.myPlayer].hbLocked && !Main.blockMouse)
                        Main.player[Main.myPlayer].changeItem = i;
                    Main.hoverItemName = Main.player[Main.myPlayer].inventory[i].AffixName();
                    if (Main.player[Main.myPlayer].inventory[i].stack > 1)
                    {
                        object obj = Main.hoverItemName;
                        Main.hoverItemName = string.Concat(obj, " (", Main.player[Main.myPlayer].inventory[i].stack, ")");
                    }

                    Main.rare = Main.player[Main.myPlayer].inventory[i].rare;
                }

                float num4 = Main.inventoryScale;
                Main.inventoryScale = num2;
                ItemSlot.Draw(Main.spriteBatch, Main.player[Main.myPlayer].inventory, 13, i, new Vector2(posX, num3), Color.White);
                Main.inventoryScale = num4;
                posX += (int) (Main.inventoryBackTexture.Width * Main.hotbarScale[i]) + 4;
            }

            int selectedItem = Main.player[Main.myPlayer].selectedItem;
            if (selectedItem < 10 || selectedItem == 58 && Main.mouseItem.type <= 0)
                return;
            float num5 = 1f;
            int num6 = (int) (20f + 22f * (1f - num5));
            int a2 = (int) (75f + 150f * num5);
            var lightColor2 = new Color(255, 255, 255, a2);
            float num7 = Main.inventoryScale;
            Main.inventoryScale = num5;
            ItemSlot.Draw(Main.spriteBatch, Main.player[Main.myPlayer].inventory, 13, selectedItem, new Vector2(posX, num6), Color.White);
            Main.inventoryScale = num7;
        }

        public static void DrawNumerals(SpriteBatch spriteBatch, int level, float scale)
        {
            var origin = Main.playerInventory ? new Vector2(132f, 60f) * scale : new Vector2(190f, 58f) * scale;
            if (level < 10)
            {
                spriteBatch.Draw(GFX.GothicNumeral[level], new Vector2(origin.X - 16f * scale, origin.Y), null, Color.White, 0f, Vector2.Zero, scale,
                    SpriteEffects.None, 0f);
            }
            else if (level < 100)
            {
                spriteBatch.Draw(GFX.GothicNumeral[level / 10], new Vector2(origin.X - 34f * scale, origin.Y), null, Color.White, 0f, Vector2.Zero, scale,
                    SpriteEffects.None, 0f);
                spriteBatch.Draw(GFX.GothicNumeral[level % 10], new Vector2(origin.X + 2f * scale, origin.Y), null, Color.White, 0f, Vector2.Zero, scale,
                    SpriteEffects.None, 0f);
            }
            else if (level < 1000)
            {
                spriteBatch.Draw(GFX.GothicNumeral[level / 100], new Vector2(origin.X - 52f * scale, origin.Y), null, Color.White, 0f, Vector2.Zero, scale,
                    SpriteEffects.None, 0f);
                spriteBatch.Draw(GFX.GothicNumeral[level % 100 / 10], new Vector2(origin.X - 16f, origin.Y), null, Color.White, 0f, Vector2.Zero, scale,
                    SpriteEffects.None, 0f);
                spriteBatch.Draw(GFX.GothicNumeral[level % 10], new Vector2(origin.X + 20f * scale, origin.Y), null, Color.White, 0f, Vector2.Zero, scale,
                    SpriteEffects.None, 0f);
            }
        }

        public override void PostDraw(SpriteBatch spriteBatch, Player player)
        {
            if (Main.playerInventory || Main.player[Main.myPlayer].ghost)
                return;

            character = player.GetModPlayer<PlayerCharacter>();

            DrawHotbar();

            spriteBatch.Draw(GFX.StatusBarsBg, GuiPosition, null, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);

            int currentLifeLength = (int) Math.Round(player.statLife / (decimal) player.statLifeMax2 * BarLifeLength);
            spriteBatch.Draw(GFX.StatusBars, GuiPosition + barLifeOrigin * Scale,
                new Rectangle((int) (barLifeOrigin.X + BarLifeLength - currentLifeLength), (int) barLifeOrigin.Y, currentLifeLength, BarLifeThickness),
                Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
            int currentManaLength = (int) Math.Round(character.Mana / (decimal) player.statManaMax2 * BarManaLength);
            spriteBatch.Draw(GFX.StatusBars, GuiPosition + barManaOrigin * Scale,
                new Rectangle((int) (barManaOrigin.X + BarManaLength - currentManaLength), (int) barManaOrigin.Y, currentManaLength, BarManaThickness),
                Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
            int currentXpLength = (int) Math.Round(BarXpLength * (decimal) character.Experience / character.ExperienceToLevel());
            spriteBatch.Draw(GFX.StatusBars, GuiPosition + barXpOrigin * Scale,
                new Rectangle((int) barXpOrigin.X, (int) barXpOrigin.Y, currentXpLength, BarXpThickness), Color.White, 0f, Vector2.Zero, Scale,
                SpriteEffects.None, 0f);

            spriteBatch.Draw(GFX.CharacterFrame, GuiPosition, null, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
            spriteBatch.DrawStringWithShadow(Main.fontMouseText, player.statLife + " / " + player.statLifeMax2,
                GuiPosition + new Vector2(barLifeOrigin.X * Scale + 24f * Scale, (barLifeOrigin.Y + 4f) * Scale), Color.White, Scale);
            spriteBatch.DrawStringWithShadow(Main.fontMouseText, character.Mana + " / " + player.statManaMax2,
                GuiPosition + new Vector2(barManaOrigin.X * Scale + 24f * Scale, barManaOrigin.Y * Scale), Color.White, 0.8f * Scale);

            DrawNumerals(spriteBatch, character.Level, Scale);

            if (character.UnspentPoints())
                spriteBatch.Draw(GFX.UnspentPoints, pointsOrigin, null, Color.White, 0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None,
                RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

            if (player.lavaTime < player.lavaMax)
            {
                int currentBubbles = (int) Math.Round((decimal) BubblesLength * player.lavaTime / player.lavaMax);
                spriteBatch.Draw(GFX.bubbles_lava, GuiPosition + bubblesOrigin * Scale, new Rectangle(0, 0, currentBubbles, BubblesThickness), Color.White,
                    Scale);
            }

            if (player.breath < player.breathMax)
            {
                int currentBubbles = (int) Math.Round((decimal) BubblesLength * player.breath / player.breathMax);
                spriteBatch.Draw(GFX.bubbles, GuiPosition + bubblesOrigin * Scale, new Rectangle(0, 0, currentBubbles, BubblesThickness), Color.White, Scale);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None,
                RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
            Main.buffString = "";
            Main.bannerMouseOver = false;
            if (!Main.recBigList)
                Main.recStart = 0;
            if (!Main.ingameOptionsWindow && !Main.playerInventory && !Main.inFancyUI)
                DrawBuffs();
        }
    }
}