
#runtime_dependency("PlatformRT");

#type_table(
    #type_reference("System.Console"),
    #type_reference("Program")
);

#method_table(
    #method_reference(#type_table_reference(0), "WriteLine", 1, { }, #void, { #string }),
    #method_reference(#type_table_reference(1), "Main", 1, { }, #void, { #array_type(#string, 1) })
);

#assembly(
    #member(HelloWorld),
    #version(1, 0, 0, 0),
    #method_table_reference(1),
    {
        #type_definition(
            #member(Program, #public),
            { },
            { },
            { },
            {
                #method(
                    #member(Main, #public),
                    { }, 1, #void, { #param(#member(args), #array_type(#string, 1)) }, { },
                    {
                        #return(#invoke(#get_delegate(#method_table_reference(0), #null), #const_string("Hello, world!")));
                    }
                );
            }
        );
    },
    {

    }
);
