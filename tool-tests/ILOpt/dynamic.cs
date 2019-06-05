//! run

// This test case is based on Nick Craver's DLR benchmark, which is hosted
// at https://github.com/NickCraver/PerfBenchmarks/blob/master/Benchmarks/DLR.cs .

using System;

namespace Benchmarks
{
    public class DLRTests
    {
        private static SomeType _type = new SomeType
        {
            Foo = "Foo",
            Foo2 = "Foo2",
            Conn = new DbConn
            {
                Name = "Test",
                ConnectionString = "MyServer"
            }
        };
        private static object _passed = _type;
        
        public static string Casting()
        {
            var t = ((SomeType)_passed);
            return t.Conn.ConnectionString;
        }
        
        public static string Dynamic()
        {
            dynamic t = _passed;
            return t.Conn.ConnectionString;
        }

        public class SomeType
        {
            public string Foo { get; set; }
            public string Foo2 { get; set; }
            public DbConn Conn { get; set; }
        }

        public class DbConn
        {
            public string Name { get; set; }
            public string ConnectionString { get; set; }
        }

        public static void Main()
        {
            Console.WriteLine(Casting());
            Console.WriteLine(Dynamic());
        }
    }
}
