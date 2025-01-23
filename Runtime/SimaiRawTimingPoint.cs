using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable
namespace MajSimai
{
    internal class SimaiRawTimingPoint
    {
        public double Timing { get; } = 0;
        public float Bpm { get; } = -1;
        public float HSpeed { get; } = 1f;
        public string RawContent { get; } = string.Empty;
        public int RawTextPositionX { get; }
        public int RawTextPositionY { get; }

        public SimaiRawTimingPoint(double timing, int textPosX = 0, int textPosY = 0, string rawContent = "", float bpm = 0f,
            float hspeed = 1f)
        {
            Timing = timing;
            RawTextPositionX = textPosX;
            RawTextPositionY = textPosY;
            RawContent = rawContent.Replace("\n", "").Replace(" ", "");
            Bpm = bpm;
            HSpeed = hspeed;
        }
        public async Task<SimaiTimingPoint> ParseAsync()
        {
            return await Task.Run(() =>
            {
                var notes = SimaiHelper.GetNotes(Timing, Bpm, RawContent);

                return new SimaiTimingPoint(Timing, notes, RawTextPositionX, RawTextPositionY, RawContent, Bpm, HSpeed);
            });
        }
    }
}
