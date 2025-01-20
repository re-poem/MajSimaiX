using System;
using System.Collections.Generic;
using System.Text;
#nullable enable
namespace MajSimaiParser.Exceptions
{
    public class InvalidValueException: Exception
    {
        public int Line { get; }
        public string Content { get; }
        public InvalidValueException(int line, string content) : base()
        {
            Line = line;
            Content = content;
        }
        public InvalidValueException(int line, string content, string message) : base(message)
        {
            Line = line;
            Content = content;
        }
    }
}
