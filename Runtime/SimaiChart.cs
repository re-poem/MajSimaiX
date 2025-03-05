using System;
using System.Collections.Generic;
using System.Text;
namespace MajSimai
{
    public class SimaiChart
    {
        public string Level { get; set; }
        public string Designer { get; set; }
        public SimaiTimingPoint[] NoteTimings { get; set; }
        public SimaiTimingPoint[] CommaTimings { get; set; }
        public bool IsEmpty => NoteTimings.Length == 0;
        public SimaiChart(string? level, string? designer, SimaiTimingPoint[]? noteTimings, SimaiTimingPoint[]? commaTimings = null)
        {
            Level = level ?? string.Empty;
            Designer = designer ?? string.Empty;
            NoteTimings = noteTimings ?? Array.Empty<SimaiTimingPoint>();
            CommaTimings = commaTimings ?? Array.Empty<SimaiTimingPoint>();
        }

        public SimaiChart()
        {
            Level = "";
            Designer = "";
            NoteTimings = new SimaiTimingPoint[0];
            CommaTimings = new SimaiTimingPoint[0];
        }
    }
}
