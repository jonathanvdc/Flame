//! run:hello

using System;

public class StringContainer
{
    public StringContainer(string contents)
    {
        this.Contents = contents;
    }

    public string Contents;
}

public static class Program
{
    public static void Main(string[] args)
    {
        object container = new StringContainer(args[0]);
        if (container is StringContainer)
        {
            Console.WriteLine((container as StringContainer).Contents);
        }
    }
}
