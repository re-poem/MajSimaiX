using System;
using System.Collections.Generic;
using System.Text;
namespace MajSimaiParser
{
    public class SimaiFile
    {
        public string Path { get; }
        public string Title { get; }
        public string Artist { get; }
        public float Offset { get; }
        public SimaiChart[] Levels { get; }
        public string[] Fumens { get; }
        public SimaiCommand[] Commands { get; }
        
        public SimaiFile(string path,string title, string artist, float offset, SimaiChart[] levels,string[] fumens, SimaiCommand[] commands)
        {
            if (levels is null)
                throw new ArgumentNullException(nameof(levels));
            if (fumens is null)
                throw new ArgumentNullException(nameof(fumens));
            if (levels.Length != 7)
                throw new ArgumentException("The length of parameter \"levels\" must be 7");
            if(fumens.Length != 7)
                throw new ArgumentException("The length of parameter \"fumens\" must be 7");
            Path = path;
            Title = title; 
            Artist = artist;
            Offset = offset;
            Levels = levels;
            Fumens = fumens;
            Commands = commands;
        }
    }
}
