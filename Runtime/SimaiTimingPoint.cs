using System;
using System.Collections.Generic;
using System.Linq;

namespace MajSimai
{
    public class SimaiTimingPoint
    {
        public double Timing { get; set; } = 0;
        public float Bpm { get; set; } = -1;
        public float HSpeed { get; set; } = 1f;
        public string RawContent { get; } = string.Empty;
        public int RawTextPositionX { get; }
        public int RawTextPositionY { get; }
        public SimaiNote[] Notes { get; set; } = Array.Empty<SimaiNote>();
        public bool IsEmpty => Notes.Length == 0;

        public SimaiTimingPoint(double timing, SimaiNote[] notes, int textPosX = 0, int textPosY = 0, string rawContent = "", float bpm = 0f,
            float hspeed = 1f)
        {
            Timing = timing;
            RawTextPositionX = textPosX;
            RawTextPositionY = textPosY;
            RawContent = rawContent.Replace("\n", "").Replace(" ", "");
            Bpm = bpm;
            HSpeed = hspeed;
            if (notes != null)
            {
                Notes = notes;
            }
        }
    }
}
