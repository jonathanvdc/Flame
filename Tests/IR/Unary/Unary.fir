
#assembly(
    #member(Unary),
    #version(1, 0, 0, 0),
    #null,
    {
        #type_definition(
            #member(Unary, #public),
            { },
            { },
            { },
            {
                #method(#member(notZero, #public, #const),
                        { }, 0, #int(32), { }, { },
                        #return(#unary("!", #const_int32(0))));
            }
        );
    },
    {

    }
);
