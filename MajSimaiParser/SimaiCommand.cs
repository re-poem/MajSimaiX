using System;
using System.Collections.Generic;
using System.Text;

namespace MajSimaiParser
{
    public class SimaiCommand
    {
        public string Prefix { get; }
        public string Value { get; }
        public SimaiCommand(string prefix, string value)
        {
            Prefix = prefix;
            Value = value;
        }
    }
}
