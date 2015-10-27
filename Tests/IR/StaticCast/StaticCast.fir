
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
                    { }, 0, #float(64), { }, { },
                    {
                        #return(#static_cast(#float(64), #const_int32(1)));
                    });
            }
        );
    },
    {

    }
);
