//! run:hello

using System;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            try
            {
                throw new NotSupportedException(args[0]);
            }
            catch (NotSupportedException)
            {
                throw;
            }
        }
        catch (NotSupportedException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
