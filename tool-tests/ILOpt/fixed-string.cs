//! run

using System;

class Program
{
    static void Main()
    {
        Print("Hello world!");
    }

    unsafe static void Print(string value)
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
