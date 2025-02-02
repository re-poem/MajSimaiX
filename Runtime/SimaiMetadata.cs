using MajSimai;
using System;

namespace MajSimai
{
    public readonly struct SimaiMetadata
    {
        public string Title { get; }
        public string Artist { get; }
        public float Offset { get; }
        public string[] Designers { get; }
        public string[] Levels { get; }
        public string[] Fumens { get; }
        public SimaiCommand[] Commands { get; }
        public string Hash { get; }

        public SimaiMetadata(string title, string artist, float offset, string[] designers, string[] levels, string[] fumens, SimaiCommand[] commands,string hash)
        {
            if (fumens is null)
                throw new ArgumentNullException(nameof(fumens));
            if (fumens.Length != 7)
                throw new ArgumentException("The length of parameter \"fumens\" must be 7");
            Title = title;
            Artist = artist;
            Offset = offset;
            Designers = designers;
            Levels = levels;
            Fumens = fumens;
            Commands = commands;
            Hash = hash ?? string.Empty;
        }
    }
}
