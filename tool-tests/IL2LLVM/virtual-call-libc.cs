//! run

using System.Runtime.InteropServices;

public unsafe class Base
{
    [DllImport("libc.so.6")]
    public static extern void* malloc(ulong size);

    [DllImport("libc.so.6")]
    public static extern void free(void* data);

    [DllImport("libc.so.6")]
    public static extern int puts(byte* str);

    public virtual byte* CreateFormatString()
    {
        var str = (byte*)malloc(3);
        *str = (byte)'h';
        *(str + 1) = (byte)'i';
        *(str + 2) = (byte)'\0';
        return str;
    }

    public void Print()
    {
        var str = CreateFormatString();
        puts(str);
        free((void*)str);
    }
}

public unsafe class Derived : Base
{
    public override byte* CreateFormatString()
    {
        var str = (byte*)malloc(4);
        *str = (byte)'b';
        *(str + 1) = (byte)'y';
        *(str + 2) = (byte)'e';
        *(str + 3) = (byte)'\0';
        return str;
    }
}

public static unsafe class Program
{
    public static int Main()
    {
        new Base().Print();
        new Derived().Print();
        return 0;
    }
}
