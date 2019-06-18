//! run

using System.Runtime.InteropServices;

public interface IPrinter
{
    void Print();
}

public unsafe abstract class PrinterBase : IPrinter
{
    [DllImport("libc.so.6")]
    public static extern void* malloc(ulong size);

    [DllImport("libc.so.6")]
    public static extern void free(void* data);

    [DllImport("libc.so.6")]
    public static extern int puts(byte* str);

    protected abstract byte* CreateFormatString();

    public virtual void Print()
    {
        var str = CreateFormatString();
        puts(str);
        free((void*)str);
    }
}

public unsafe class HiPrinter : PrinterBase
{
    protected override byte* CreateFormatString()
    {
        var str = (byte*)malloc(3);
        *str = (byte)'h';
        *(str + 1) = (byte)'i';
        *(str + 2) = (byte)'\0';
        return str;
    }
}

public unsafe class ByePrinter : PrinterBase
{
    protected override byte* CreateFormatString()
    {
        var str = (byte*)malloc(4);
        *str = (byte)'b';
        *(str + 1) = (byte)'y';
        *(str + 2) = (byte)'e';
        *(str + 3) = (byte)'\0';
        return str;
    }
}

public unsafe class ByeByePrinter : ByePrinter
{
    public override void Print()
    {
        var str = (byte*)malloc(8);
        *str = (byte)'b';
        *(str + 1) = (byte)'y';
        *(str + 2) = (byte)'e';
        *(str + 3) = (byte)'-';
        *(str + 4) = (byte)'b';
        *(str + 5) = (byte)'y';
        *(str + 6) = (byte)'e';
        *(str + 7) = (byte)'\0';
        puts(str);
        free((void*)str);
    }
}

public static unsafe class Program
{
    private static void PrintWith(IPrinter printer)
    {
        printer.Print();
    }

    public static int Main()
    {
        PrintWith(new HiPrinter());
        PrintWith(new ByePrinter());
        PrintWith(new ByeByePrinter());
        return 0;
    }
}
