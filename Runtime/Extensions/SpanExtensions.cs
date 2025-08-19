using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        public static void Replace<T>(this Span<T> source, T oldValue, T newValue) where T : IEquatable<T>?
        {
            if (source.IsEmpty)
            {
                return;
            }
            for (var i = 0; i < source.Length; i++)
            {
                ref var value = ref source[i];
                if ((value?.Equals(oldValue) ?? oldValue is null))
                {
                    value = newValue;
                }
            }
        }
        /// <summary>
        /// Copies <paramref name="source"/> to <paramref name="dest"/>, replacing all occurrences of <paramref name="oldValue"/> with <paramref name="newValue"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the spans.</typeparam>
        /// <param name="source">The span to copy.</param>
        /// <param name="dest">The span into which the copied and replaced values should be written.</param>
        /// <param name="oldValue">The value to be replaced with <paramref name="newValue"/>.</param>
        /// <param name="newValue">The value to replace all occurrences of <paramref name="oldValue"/>.</param>
        /// <exception cref="ArgumentException">The <paramref name="dest"/> span was shorter than the <paramref name="source"/> span.</exception>
        /// <exception cref="ArgumentException">The <paramref name="source"/> and <paramref name="dest"/> were overlapping but not referring to the same starting location.</exception>
        public static unsafe void Replace<T>(this ReadOnlySpan<T> source, Span<T> dest, T oldValue, T newValue) where T : IEquatable<T>?
        {
#pragma warning disable CS8500
            if (dest.Length < source.Length)
            {
                throw new ArgumentException("Destination is too short", nameof(dest));
            }
            else if(source.IsEmpty)
            {
                return;
            }
            var srcPtr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(source));
            var dstPtr = Unsafe.AsPointer(ref MemoryMarshal.GetReference(dest));
            var offset = (ulong)(((T*)dstPtr - (T*)srcPtr) * sizeof(T));
            var srcLen = (ulong)(source.Length * sizeof(T));
            var dstLen = (ulong)(dest.Length * sizeof(T));
            
            if(offset != 0 && (offset < srcLen || ulong.MinValue - offset < dstLen))
            {
                throw new ArgumentException("The source and dest were overlapping but not referring to the same starting location");
            }

            for (var i = 0; i < source.Length; i++)
            {
                var value = source[i];
                if ((value?.Equals(oldValue) ?? oldValue is null))
                {
                    dest[i] = newValue;
                }
                else
                {
                    dest[i] = value;
                }
            }
#pragma warning restore CS8500
        }
    }
}
