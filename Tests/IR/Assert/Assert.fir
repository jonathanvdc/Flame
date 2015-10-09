
#assembly(
    #member(Method),
    #version(1, 0, 0, 0),
    #null,
    {
        #type_definition(
            #member(Method, #public),
            { },
            { },
            { },
            {
                #method(
                    #member(one, #public, #const),
                    { }, 0, #int(32), { }, { },
                    {
                        #assert(#const_bool(0));
                        #return(#const_int32(1));
                    });
            }
        );
    },
    {

    }
);
