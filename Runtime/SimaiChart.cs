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
        public bool IsEmpty => NoteTimings.Length == 0;
        public static SimaiChart Empty { get; } = new SimaiChart(null, null, null);
        public SimaiChart(string? level, string? designer, SimaiTimingPoint[]? noteTimings)
        {
            Level = level is null ? string.Empty : level;
            Designer = designer is null ? "Undefined" : designer;
            NoteTimings = noteTimings is null ? Array.Empty<SimaiTimingPoint>() : noteTimings;
        }
    }
}
