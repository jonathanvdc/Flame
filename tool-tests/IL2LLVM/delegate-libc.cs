//! run

using System;
using System.Runtime.InteropServices;

public unsafe class HiPrinter
{
    [DllImport("libc.so.6")]
    public static extern int puts(byte* str);

    public void PrintHiInstance()
    {
        PrintHi();
    }

    public static void PrintHi()
    {
        var str = new byte[] { (byte)'h', (byte)'i', (byte)'\0' };
        fixed (byte* ptr = str)
        {
            puts(ptr);
        }
    }
}

public static class Program
{
    private static Action printFun;

    public static int Main()
    {
        printFun = new HiPrinter().PrintHiInstance;
        printFun();
        printFun = HiPrinter.PrintHi;
        printFun();
        return 0;
    }
}
