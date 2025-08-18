using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MajSimai
{
    internal static class SpanExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(this ReadOnlySpan<T> source, T value) where T: IEquatable<T>
        {
            if(source.IsEmpty)
            {
                return false;
            }
            for (var i = 0; i < source.Length; i++)
            {
                if (source[i].Equals(value))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Determines whether the specified value appears at the start of the span.
        /// </summary>
        /// <param name="span">The span to search.</param>
        /// <param name="value">The value to compare.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>?
        {
            return span.Length != 0 && (span[0]?.Equals(value) ?? (object?)value is null);
        }
            
    }
}
