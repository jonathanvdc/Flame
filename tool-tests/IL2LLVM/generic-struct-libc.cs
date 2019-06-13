//! run

using System.Runtime.InteropServices;

public struct Pair<T>
{
    public Pair(T first, T second)
    {
        this.first = first;
        this.second = second;
    }

    public T first;
    public T second;
}

public static class Pair
{
    public static Pair<T> Create<T>(T first, T second)
    {
        return new Pair<T>(first, second);
    }
}

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

    private static void Print(int value)
    {
        byte* fmt = CreateString();
        printf(fmt, value);
        free((void*)fmt);
    }

    private static void Print(Pair<int> pair)
    {
        Print(pair.first);
        Print(pair.second);
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

    private static Pair<int> FactorialPair(int value)
    {
        return Pair.Create(Factorial(value - 1), Factorial(value));
    }

    public static int Main()
    {
        Print(FactorialPair(6));
        return 0;
    }
}
