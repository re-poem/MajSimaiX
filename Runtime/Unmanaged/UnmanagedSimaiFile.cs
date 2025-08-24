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
    public int titleLen;
    public int artistLen;
    public float offset;
    public int chartsLen;
    public int commandsLen;

    public char* title;
    public char* artist;
    public UnmanagedSimaiChart* charts;
    public UnmanagedSimaiCommand* commands;
}
#endif