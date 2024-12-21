using System;
using System.Collections.Generic;
using System.Text;

namespace MajSimaiParser.Exceptions
{
    internal class InvalidSimaiMarkupException: FormatException
    {
        public int Line { get; }
        public string Content { get; }
        public InvalidSimaiMarkupException(int line, string content) : base()
        {
            Line = line;
            Content = content;
        }
        public InvalidSimaiMarkupException(int line, string content,string message) : base(message)
        {
            Line = line;
            Content = content;
        }
    }
}
