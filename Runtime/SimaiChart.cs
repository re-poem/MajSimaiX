using MajSimai.Utils;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
namespace MajSimai
{
    public class SimaiChart
    {
        public string Level { get; }
        public string Designer { get; }
        public bool IsEmpty
        {
            get
            {
                return _isEmpty;
            }
        }
        public string Fumen
        {
            get
            {
                return _fumen;
            }
        }
        public ReadOnlySpan<SimaiTimingPoint> NoteTimings
        {
            get
            {
                return _noteTimings;
            }
        }
        public ReadOnlySpan<SimaiTimingPoint> CommaTimings
        {
            get
            {
                return _commaTimings;
            }
        }
        public readonly static SimaiChart Empty = new SimaiChart();


        readonly bool _isEmpty;
        readonly string _fumen;
        readonly SimaiTimingPoint[] _noteTimings;
        readonly SimaiTimingPoint[] _commaTimings;
        public SimaiChart(string level, string designer, string fumen,
                          IEnumerable<SimaiTimingPoint>? noteTimings,
                          IEnumerable<SimaiTimingPoint>? commaTimings = null)
        {
            if(level is null)
            {
                throw new ArgumentNullException(nameof(level));
            }
            if (designer is null)
            {
                throw new ArgumentNullException(nameof(designer));
            }
            Level = level;
            Designer = designer;
            _noteTimings = Array.Empty<SimaiTimingPoint>();
            _commaTimings = Array.Empty<SimaiTimingPoint>();
            var i = 0;
            var buffer = ArrayPool<SimaiTimingPoint>.Shared.Rent(16);
            
            try
            {
                foreach (var n in noteTimings ?? Array.Empty<SimaiTimingPoint>())
                {
                    BufferHelper.EnsureBufferLength(i + 1, ref buffer);
                    buffer[i++] = n;
                }
                if(i != 0)
                {
                    if(string.IsNullOrEmpty(fumen))
                    {
                        throw new ArgumentException(nameof(fumen));
                    }
                    _noteTimings = new SimaiTimingPoint[i];
                    buffer.AsSpan(0, i).CopyTo(_noteTimings);
                }
                i = 0;

                foreach (var n in commaTimings ?? Array.Empty<SimaiTimingPoint>())
                {
                    BufferHelper.EnsureBufferLength(i + 1, ref buffer);
                    buffer[i++] = n;
                }
                if (i != 0)
                {
                    _commaTimings = new SimaiTimingPoint[i];
                    buffer.AsSpan(0, i).CopyTo(_commaTimings);
                }
            }
            finally
            {
                ArrayPool<SimaiTimingPoint>.Shared.Return(buffer);
            }
            _fumen = fumen;
            _isEmpty = _noteTimings.Length == 0;
        }
        SimaiChart()
        {
            Level = string.Empty;
            Designer = string.Empty;
            _fumen = string.Empty;
            _noteTimings = Array.Empty<SimaiTimingPoint>();
            _commaTimings = Array.Empty<SimaiTimingPoint>();
            _isEmpty = true;
        }
    }
}
