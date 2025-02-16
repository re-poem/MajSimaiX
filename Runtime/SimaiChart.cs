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
        public static SimaiChart Empty { get; } = new SimaiChart(null, null, null);
        public SimaiChart(string? level, string? designer, SimaiTimingPoint[]? noteTimings, SimaiTimingPoint[]? commaTimings = null)
        {
            Level = level ?? string.Empty;
            Designer = designer ?? "Undefined";
            NoteTimings = noteTimings ?? Array.Empty<SimaiTimingPoint>();
            CommaTimings = commaTimings ?? Array.Empty<SimaiTimingPoint>();
        }
    }
}
