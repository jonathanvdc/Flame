// TODO: investigate, re-enable this test for Linux builds only: //! run

using System;

public unsafe static class Program
{
    public static void Main()
    {
        int* ptr = stackalloc int[1];
        *ptr = 42;
        Console.WriteLine(*ptr);
    }
}
