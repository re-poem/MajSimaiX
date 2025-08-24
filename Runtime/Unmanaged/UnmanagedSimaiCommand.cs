#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MajSimai.Unmanaged;
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct UnmanagedSimaiCommand
{
    public char* prefix;
    public int prefixLen;

    public char* value;
    public int valueLen;
}
#endif
