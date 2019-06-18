//! run

using System.Runtime.InteropServices;

public static unsafe class Program
{
    [DllImport("libc.so.6")]
    public static extern int puts(byte* str);

    public static int Main()
    {
        var str = new byte[] { (byte)'h', (byte)'i', (byte)'\0' };
        fixed (byte* ptr = str)
        {
            puts(ptr);
        }
        return 0;
    }
}
