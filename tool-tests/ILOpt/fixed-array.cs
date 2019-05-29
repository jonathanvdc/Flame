//! run

using System;

class Program
{
    static void Main()
    {
        Print("Hello world!".ToCharArray());
    }

    unsafe static void Print(char[] value)
    {
        fixed (char* pointer = value)
        {
            // Add one to each of the characters.
            for (int i = 0; i < value.Length; ++i)
            {
                Console.Write(pointer[i]++);
            }
        }
        Console.WriteLine();
    }
}
