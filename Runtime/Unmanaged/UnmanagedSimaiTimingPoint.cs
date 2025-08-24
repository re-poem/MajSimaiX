#if NET7_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MajSimai.Unmanaged;
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct UnmanagedSimaiTimingPoint
{
    public double timing;
    public float bpm;
    public float hSpeed;
    public int rawTextPositionX;
    public int rawTextPositionY;
    public int rawTextPosition;

    public bool isEmpty;

    public UnmanagedSimaiNote* notes;
    public int notesLen;

    public char* rawContent;
    public int rawContentLen;
}
#endif