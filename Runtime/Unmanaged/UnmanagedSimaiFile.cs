#if NET7_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MajSimai.Unmanaged;
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct UnmanagedSimaiFile
{
    public char* title;
    public int titleLen;

    public char* artist;
    public int artistLen;

    public float offset;

    public UnmanagedSimaiChart* charts;
    public int chartsLen;

    public UnmanagedSimaiCommand* commands;
    public int commandsLen;
}
#endif