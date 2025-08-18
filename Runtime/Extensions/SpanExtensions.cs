using System;
using System.Collections.Generic;
using System.Text;

namespace MajSimai
{
    internal static class SpanExtensions
    {
        public static int Split(this ReadOnlySpan<char> source, Span<Range> dest, char separator, StringSplitOptions options = StringSplitOptions.None)
        {
            var keepEmptyEntries = (options & StringSplitOptions.RemoveEmptyEntries) == 0;
            if (dest.IsEmpty)
            {
                return 0;
            }
            if (source.IsEmpty)
            {
                if (keepEmptyEntries)
                {
                    dest[0] = new Range(0, 0);
                    return 1;
                }
                return 0;
            }
            var startAt = 0;
            var destIndex = 0;
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                if (c == separator)
                {
                    var range = new Range(startAt, i);
                    var content = source[range];
                    if (!content.IsEmpty || keepEmptyEntries)
                    {
                        dest[destIndex++] = range;
                        if (dest.Length == destIndex)
                        {
                            return destIndex;
                        }
                    }
                    startAt = i + 1;
                }
            }
            var range2 = new Range(startAt, source.Length);
            var content2 = source[range2];
            if (!content2.IsEmpty || keepEmptyEntries)
            {
                dest[destIndex++] = range2;
            }
            return destIndex;
        }
    }
}
