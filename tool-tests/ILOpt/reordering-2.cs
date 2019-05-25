//! run

using System;

public static class Program
{
    private static void Update(int[] array, int index)
    {
        var tmp = array[index];
        array[index] = array[0];
        array[0] = tmp;
    }

    public static void Main()
    {
        var arr = new int[2];
        arr[1] = 42;
        Update(arr, 1);
        Console.WriteLine(arr[0]);
        Console.WriteLine(arr[1]);
    }
}
