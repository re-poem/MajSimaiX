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
        public SimaiCommand[] Commands { get; }

        public SimaiFile(string path, string title, string artist, float offset, SimaiChart[] levels, string[] fumens, SimaiCommand[] commands)
        {
            if (levels is null)
                throw new ArgumentNullException(nameof(levels));
            if (fumens is null)
                throw new ArgumentNullException(nameof(fumens));
            if (levels.Length != 7)
                throw new ArgumentException("The length of parameter \"levels\" must be 7");
            if (fumens.Length != 7)
                throw new ArgumentException("The length of parameter \"fumens\" must be 7");
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
        public string ExportMetadata()
        {
            var sb = new StringBuilder();
            var finalDesigner = string.Empty;

            sb.AppendLine($"&title={Title}");
            sb.AppendLine($"&artist={Artist}");
            sb.AppendLine($"&first={Offset}");
            for (int i = 0; i < 7; i++)
            {
                var chart = Charts[i];
                if(chart is null)
                {
                    sb.AppendLine($"&des_{i}=");
                    sb.AppendLine($"&lv_{i}=");
                }
                else
                {
                    sb.AppendLine($"&des_{i}={chart.Designer}");
                    sb.AppendLine($"&lv_{i}={chart.Level}");
                    if (!string.IsNullOrEmpty(chart.Designer))
                    {
                        finalDesigner = chart.Designer;
                    }
                }
            }
            sb.AppendLine($"&des={finalDesigner}");
            return sb.ToString();
        }
    }
}
