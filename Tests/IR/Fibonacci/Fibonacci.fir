#type_table(
    #type_reference("Fibonacci");
);

#method_table(
    #method_reference(#type_table_reference(0), "fib", 1, { }, #int(32), { #int(32) });
);

#assembly(
    #member(Fibonacci),
    #version(1, 0, 0, 0),
    #null,
    {
        #type_definition(
            #member(Fibonacci, #public),
            { },
            { },
            { },
            {
                #method(
                    #member(fib, #public, #const),
                    { }, 1, #int(32), { #param(#member(n), #int(32)) }, { },
                    {
                        #select(
                            #binary(#get_arg(0), "<=", #const_int32(1)),
                            #return(#const_int32(1)),
                            #return(
                                #binary(
                                    #invoke(#get_delegate(#method_table_reference(0), #null), #binary(#get_arg(0), "-", #const_int32(2))),
                                    "+",
                                    #invoke(#get_delegate(#method_table_reference(0), #null), #binary(#get_arg(0), "-", #const_int32(1)))
                                )
                            )
                        );
                    }
                );
            }
        );
    },
    {

    }
);
