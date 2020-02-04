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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace kRPG.Projectiles
{
    public class SmokePellets : ModProjectile
    {
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.Confused, 210);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            projectile.frame = 19 - (int) Math.Ceiling(projectile.timeLeft / 3.0);
            Texture2D text = Main.projectileTexture[projectile.type];
            int height = text.Height / Main.projFrames[projectile.type];
            spriteBatch.Draw(text, projectile.position - Main.screenPosition, new Rectangle(0, projectile.frame * height, text.Width, height), Color.White);
            return false;
        }

        public override void SetDefaults()
        {
            projectile.width = 192;
            projectile.height = 128;
            projectile.timeLeft = 57;
            projectile.friendly = true;
            projectile.hostile = false;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.ownerHitCheck = true;
            projectile.scale = 1f;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[projectile.type] = 19;
            DisplayName.SetDefault("Smoke Pellets");
        }
    }
}