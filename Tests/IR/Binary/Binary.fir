
#assembly(
    #member(Binary),
    #version(1, 0, 0, 0),
    #null,
    {
        #type_definition(
            #member(Binary, #public),
            { },
            { },
            { },
            {
                #method(#member(four, #public, #const),
                        { }, 0, #int(32), { }, { },
                        #return(#binary(#const_int32(1), "+", #const_int32(3))));
            }
        );
    },
    {

    }
);
