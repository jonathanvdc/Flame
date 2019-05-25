// ilopt test based on the example at https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/enum

using System;

public class EnumTest
{
    enum Day { Sun, Mon, Tue, Wed, Thu, Fri, Sat };

    static void Main()
    {
        var x = Day.Sun;
        var y = Day.Fri;
        Console.WriteLine("{0} = {1}", x, (int)x);
        Console.WriteLine("{0} = {1}", y, (int)y);
    }
}
