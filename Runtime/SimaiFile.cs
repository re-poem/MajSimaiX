using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace MajSimai
{
    public class SimaiFile
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public float Offset { get; set; }
        public SimaiChart[] Charts
        {
            get
            {
                return _charts;
            }
        }
        public IList<SimaiCommand> Commands
        {
            get
            {
                return _commands;
            }
        }

        readonly SimaiChart[] _charts = new SimaiChart[7];
        readonly List<SimaiCommand> _commands = new List<SimaiCommand>();

        public SimaiFile(string title, 
                         string artist, 
                         float offset, 
                         IEnumerable<SimaiChart>? levels, IEnumerable<SimaiCommand>? commands)
        {
            Title = title ?? string.Empty;
            Artist = artist ?? string.Empty;
            Offset = offset;

            var i = 0;
            Array.Fill(_charts, SimaiChart.Empty);
            foreach (var c in levels ?? Array.Empty<SimaiChart>())
            {
                _charts[i++] = c;
                if(i == 7)
                {
                    break;
                }
            }
            i = 0;
            _commands.AddRange(commands ?? Array.Empty<SimaiCommand>());
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

            return new SimaiFile(title, artist, 0, emptyCharts, Array.Empty<SimaiCommand>());
        }
    }
}
