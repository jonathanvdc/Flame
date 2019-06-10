//! run

using System.Runtime.InteropServices;

public static unsafe class Program
{
    [DllImport("libc.so.6")]
    private static extern void* malloc(ulong size);

    [DllImport("libc.so.6")]
    private static extern void free(void* data);

    [DllImport("libc.so.6")]
    private static extern int printf(byte* str, int value);

    private static byte* CreateString()
    {
        var str = (byte*)malloc(4);
        *str = (byte)'%';
        *(str + 1) = (byte)'d';
        *(str + 2) = (byte)'\n';
        *(str + 3) = (byte)'\0';
        return str;
    }

    private static int Factorial(int value)
    {
        int result = 1;
        while (value > 1)
        {
            result *= value;
            value--;
        }
        return result;
    }

    public static int Main()
    {
        byte* fmt = CreateString();
        printf(fmt, Factorial(6));
        free((void*)fmt);
        return 0;
    }
}
