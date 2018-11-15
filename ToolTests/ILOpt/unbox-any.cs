//! run

using System;

public static class Program
{
    public static void Main()
    {
        object boxedInt = 10;
        int unboxedInt = (int)boxedInt;
        Console.WriteLine("Oh hi unboxed object " + unboxedInt.ToString());
    }
}
