using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable
namespace MajSimai
{
    internal readonly struct SimaiRawTimingPoint
    {
        public double Timing { get; }
        public float Bpm { get; }
        public float HSpeed { get; } 
        public string RawContent { get; }
        public int RawTextPositionX { get; }
        public int RawTextPositionY { get; }

        public SimaiRawTimingPoint(double timing, int textPosX = 0, int textPosY = 0, string rawContent = "", float bpm = 0f,
            float hspeed = 1f)
        {
            Timing = timing;
            RawTextPositionX = textPosX;
            RawTextPositionY = textPosY;
            if (!string.IsNullOrEmpty(rawContent))
            {
                var rRCSpan = rawContent.AsSpan();
                Span<char> rCSpan = stackalloc char[rRCSpan.Length];
                rRCSpan.Replace(rCSpan, '\n', ' ');
                var i2 = 0;
                for (var i = 0; i < rCSpan.Length; i++)
                {
                    var current = rCSpan[i];
                    if (char.IsWhiteSpace(current))
                    {
                        continue;
                    }
                    else
                    {
                        rCSpan[i2++] = current;
                    }
                }
                RawContent = new string(rCSpan.Slice(0, i2));
            }
            else
            {
                RawContent = string.Empty;
            }
            Bpm = bpm;
            HSpeed = hspeed;
        }
        public SimaiTimingPoint Parse()
        {
            var notes = SimaiNoteParser.GetNotes(Timing, Bpm, RawContent);

            return new SimaiTimingPoint(Timing, notes, RawTextPositionX, RawTextPositionY, RawContent, Bpm, HSpeed);
        }
        public Task<SimaiTimingPoint> ParseAsync()
        {
            return Task.Run(Parse);
        }
    }
}
