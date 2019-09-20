//! run

using System.Runtime.InteropServices;

public static unsafe class Program
{
    [DllImport("libc.so.6")]
    public static extern int puts(byte* str);

    public static int Main()
    {
        var str = new byte[] {
            (byte)'H',
            (byte)'e',
            (byte)'l',
            (byte)'l',
            (byte)'o',
            (byte)' ',
            (byte)'w',
            (byte)'o',
            (byte)'r',
            (byte)'l',
            (byte)'d',
            (byte)'!',
            (byte)'\0'
        };
        fixed (byte* ptr = str)
        {
            puts(ptr);
        }
        return 0;
    }
}
