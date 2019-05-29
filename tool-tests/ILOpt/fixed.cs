//! run

// This test is based on the 'fixed Statement' article from the C# language
// reference, which can be found at
// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/fixed-statement 

using System;

class Point 
{ 
    public int x;
    public int y; 
}

public static class Program
{
    public unsafe static void Main()
    {
        // Variable pt is a managed variable, subject to garbage collection.
        Point pt = new Point();

        // Using fixed allows the address of pt members to be taken,
        // and "pins" pt so that it is not relocated.

        fixed (int* p = &pt.x)
        {
            *p = 1;
        }

        Console.WriteLine(pt.x);
    }
}
