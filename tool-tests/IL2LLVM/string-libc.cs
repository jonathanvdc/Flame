//! run

using System.Runtime.InteropServices;

public static unsafe class Program
{
    [DllImport("libc.so.6")]
    private static extern int puts(byte* str);

    private static byte[] ToByteArray(string str)
    {
        var result = new byte[str.Length + 1];
        for (int i = 0; i < str.Length; i++)
        {
            result[i] = (byte)str[i];
        }
        result[str.Length] = (byte)'\0';
        return result;
    }

    private static string globalString;

    public static int Main()
    {
        var str = ToByteArray("hi");
        fixed (byte* ptr = str)
        {
            puts(ptr);
        }
        globalString = "hi";
        return 0;
    }
}
