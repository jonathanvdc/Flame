
#assembly(
    #member(Namespace),
    #version(1, 0, 0, 0),
    #null,
    { },
    {
        #namespace(#member(Namespace),
        {
            #type_definition(
                #member(Class, #public),
                { },
                { },
                { },
                {
                    #method(#member(one, #public, #const),
                            { }, 0, #int(32), { }, { },
                            #return(#const_int32(1)));
                }
            );
        },
        {

        });
    }
);
