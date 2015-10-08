
#assembly(
    #member(Field),
    #version(1, 0, 0, 0),
    #null,
    {
        #type_definition(
            #member(Field, #public),
            { },
            { },
            { },
            {
                #field(#member(x, #public), 0, #int(32), #const_int32(1));
                #field(#member(y, #public), 0, #int(32), #const_int32(1));
            }
        );
    },
    {

    }
);
