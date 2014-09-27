﻿
using System;

namespace Mercraft.Core.Unity
{
    public struct Color32
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public Color32(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public override string ToString()
        {
            return String.Format("({0},{1},{2},{3})", r, g, b, a);
        }
    }
}
