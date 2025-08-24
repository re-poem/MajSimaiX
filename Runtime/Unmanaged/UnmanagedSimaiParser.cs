#if NET7_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MajSimai.Unmanaged;
public unsafe class UnmanagedSimaiParser
{
    [UnmanagedCallersOnly(EntryPoint = "Parse")]
    public static void Parse(char* contentAnsi, int contentAnsiLen, UnmanagedSimaiParseResult* result)
    {
        var content = Marshal.PtrToStringAnsi((nint)contentAnsi, contentAnsiLen);
        if (string.IsNullOrEmpty(content))
        {
            *result = new()
            {
                code = UnmanagedSimaiParseResult.CODE_INVALID_CONTENT
            };
            return;
        }
        try
        {
            var simaiFile = SimaiParser.Parse(content);
            var unmanagedSimaiFile = simaiFile.ToUnmanaged();
            var unmanagedSimaiFilePtr = (UnmanagedSimaiFile*)Marshal.AllocHGlobal(sizeof(UnmanagedSimaiFile));
            *unmanagedSimaiFilePtr = unmanagedSimaiFile;
            *result = new()
            {
                code = UnmanagedSimaiParseResult.CODE_NO_ERROR,
                simaiFile = unmanagedSimaiFilePtr
            };
        }
        catch(Exception e)
        {
            var errorMsg = e.ToString();
            var errorMsgPtr = (char*)null;
            if(!string.IsNullOrEmpty(errorMsg))
            {
                errorMsgPtr = (char*)Marshal.StringToHGlobalAnsi(errorMsg);
            }
            *result = new()
            {
                code = UnmanagedSimaiParseResult.CODE_INTERNAL_ERROR,
                errorMsgAnsi = errorMsgPtr,
                errorMsgLen = errorMsg.Length
            };
        }
    }
    [UnmanagedCallersOnly(EntryPoint = "Free")]
    public static bool Free(nint ptr)
    {
        try
        {
            Marshal.FreeHGlobal(ptr);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
#endif
