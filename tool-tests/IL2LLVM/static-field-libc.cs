//! run

using System.Runtime.InteropServices;

public static unsafe class Program
{
    [DllImport("libc.so.6")]
    private static extern void* malloc(ulong size);

    [DllImport("libc.so.6")]
    private static extern void free(void* data);

    [DllImport("libc.so.6")]
    private static extern int puts(byte* str);

    private static byte* format;

    private static void FillString(byte* str)
    {
        *str = (byte)'h';
        *(str + 1) = (byte)'i';
        *(str + 2) = (byte)'\0';
    }

    private static void Initialize()
    {
        format = (byte*)malloc(3);
        FillString(format);
    }

    private static void Deinitialize()
    {
        free((void*)format);
        format = null;
    }

    public static int Main()
    {
        Initialize();
        puts(format);
        Deinitialize();
        return 0;
    }
}
