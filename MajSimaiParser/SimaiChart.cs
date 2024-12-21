using System;
using System.Collections.Generic;
using System.Text;

namespace MajSimaiParser
{
    public class SimaiChart
    {
        public string Level { get; }
        public string Designer { get; }
        public SimaiTimingPoint[] Notes { get; }
        public SimaiTimingPoint[] Timings { get; }
        public bool IsEmpty => Notes.Length == 0;
        public static SimaiChart Empty { get; } = new SimaiChart(null, null, null, null);
        public SimaiChart(string level, string designer, SimaiTimingPoint[] notes, SimaiTimingPoint[] timings)
        {
            Level = level is null ? string.Empty : level;
            Designer = designer is null ? "Undefined" : designer;
            Notes = notes is null ? Array.Empty<SimaiTimingPoint>() : notes;
            Timings = timings is null ? Array.Empty<SimaiTimingPoint>() : timings;
        }
    }
}
