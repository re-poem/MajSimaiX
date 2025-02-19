using System;
using System.Collections.Generic;
using System.Text;

namespace MajSimai
{
    public class SimaiCommand
    {
        public string Prefix { get; set; }
        public string Value { get; set; }
        public SimaiCommand(string prefix, string value)
        {
            Prefix = prefix;
            Value = value;
        }
    }
}
