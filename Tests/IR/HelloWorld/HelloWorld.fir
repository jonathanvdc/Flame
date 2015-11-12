
#runtime_dependency("PlatformRT");

#type_table(
    // System.Console class
    #type_reference("System.Console"),
    // Program class
    #type_reference("Program")
);

#method_table(
    // static void System.Console.WriteLine(string)
    #method_reference(#type_table_reference(0), "WriteLine", @true, { }, #void, { #string }),
    // static void Program.Main(string[])
    #method_reference(#type_table_reference(1), "Main", @true, { }, #void, { #array_type(#string, 1) })
);

// assembly HelloWorld
#assembly(
    #member(HelloWorld),
    #version(1, 0, 0, 0),
    // Entry point: static void Program.Main(string[])
    #method_table_reference(1),
    {
        // public class Program
        #type_definition(
            #member(Program, #public),
            { },
            { },
            { },
            {
                // static void Main(string[] args)
                #method(
                    #member(Main, #public),
                    { }, @true, #void, { #param(#member(args), #array_type(#string, 1)) }, { },
                    {
                        #return(#invoke(#get_delegate(#method_table_reference(0), @null), #const_string("Hello, world!")));
                    }
                );
            }
        );
    },
    {

    }
);
