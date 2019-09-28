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

    private static string GetStringToPrint(int index)
    {
        switch (index)
        {
            case 0:
                return "Hi!";
            case 1:
                return "Hello!";
            case 2:
                return "World!";
            case 3:
                return "Howdy!";
            case 4:
                return "General Kenobi!";
            default:
                return "Hey there!";
        }
    }

    private static void PrintString(string str)
    {
        fixed (byte* ptr = ToByteArray(str))
        {
            puts(ptr);
        }
    }

    public static int Main()
    {
        PrintString(GetStringToPrint(3));
        PrintString(GetStringToPrint(2));
        return 0;
    }
}
