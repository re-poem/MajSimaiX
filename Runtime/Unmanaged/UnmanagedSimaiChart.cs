#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MajSimai.Unmanaged;
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct UnmanagedSimaiChart
{
    public char* level;
    public int levelLen;

    public char* designer;
    public int designerLen;

    public char* fumen;
    public int fumenLen;

    public bool isEmpty;

    public UnmanagedSimaiTimingPoint* noteTimings;
    public int noteTimingsLen;

    public UnmanagedSimaiTimingPoint* commaTimings;
    public int commaTimingsLen;

    public readonly static UnmanagedSimaiChart Empty = new()
    {
        level = null,
        levelLen = 0,
        designer = null,
        designerLen = 0,
        fumen = null,
        fumenLen = 0,
        isEmpty = true,
        noteTimings = null,
        noteTimingsLen = 0,
        commaTimings = null,
        commaTimingsLen = 0,
    };
}
#endif