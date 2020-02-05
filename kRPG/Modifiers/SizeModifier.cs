﻿using System;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace kRPG.Modifiers
{
    public class SizeModifier : NpcModifier
    {
        public SizeModifier(kNPC kNpc, NPC npc, float scaleModifier = 1.1f, float lifeModifier = 1.4f) : base(kNpc, npc)
        {
            this.npc = npc;
            ScaleModifier = scaleModifier;
            LifeModifier = lifeModifier;
            Apply();
        }

        private float LifeModifier { get; set; }
        private float ScaleModifier { get; set; }

        public override void Apply()
        {
            npc.scale *= ScaleModifier;
            npc.lifeMax = (int) (npc.lifeMax * LifeModifier);
            npc.life = (int) (npc.life * LifeModifier);
            if (ScaleModifier < 1)
                npc.GivenName = "Small " + npc.GivenName;
            else
                npc.GivenName = "Massive " + npc.GivenName;
            npc.GetGlobalNPC<kNPC>().SpeedModifier *= (float) Math.Pow(ScaleModifier, 0.9);
        }

        public new static NpcModifier New(kNPC kNpc, NPC npc)
        {
            return new SizeModifier(kNpc, npc);
        }

        public new static NpcModifier Random(kNPC kNpc, NPC npc)
        {
            return new SizeModifier(kNpc, npc, .5f + Main.rand.NextFloat(2), .5f + Main.rand.NextFloat(1));
        }

        public override void Read(BinaryReader reader)
        {
            ScaleModifier = reader.ReadSingle();
            LifeModifier = reader.ReadSingle();
        }

        public override void Write(ModPacket packet)
        {
            packet.Write(ScaleModifier);
            packet.Write(LifeModifier);
        }
    }
}