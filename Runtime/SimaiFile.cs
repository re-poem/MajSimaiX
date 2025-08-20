using System;
using System.Collections.Generic;
using System.Text;
namespace MajSimai
{
    public class SimaiFile
    {
        public string Path { get; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public float Offset { get; set; }
        public SimaiChart[] Charts { get; set; }
        public string[] RawCharts { get; set; }
        public SimaiCommand[] Commands { get; set; }

        public SimaiFile(string path, string title, string artist, float offset, SimaiChart[] levels, string[] fumens, SimaiCommand[] commands)
        {
            if (levels is null)
            {
                throw new ArgumentNullException(nameof(levels));
            }
            if (fumens is null)
            {
                throw new ArgumentNullException(nameof(fumens));
            }
            if (levels.Length != 7)
            {
                throw new ArgumentException("The length of parameter \"levels\" must be 7");
            }
            if (fumens.Length != 7)
            {
                throw new ArgumentException("The length of parameter \"fumens\" must be 7");
            }
            Path = path;
            Title = title;
            Artist = artist;
            Offset = offset;
            Charts = levels;
            RawCharts = fumens;
            Commands = commands;
        }
        public static SimaiFile Empty(string title, string artist)
        {
            var emptyCharts = new SimaiChart[7]
            {
                SimaiChart.Empty,
                SimaiChart.Empty,
                SimaiChart.Empty,
                SimaiChart.Empty,
                SimaiChart.Empty,
                SimaiChart.Empty,
                SimaiChart.Empty,
            };
            var emptyFumens = new string[7]
            {
                string.Empty, 
                string.Empty,
                string.Empty, 
                string.Empty,
                string.Empty, 
                string.Empty, 
                string.Empty
            };

            return new SimaiFile(string.Empty, title, artist, 0, emptyCharts, emptyFumens, Array.Empty<SimaiCommand>());
        }
    }
}
