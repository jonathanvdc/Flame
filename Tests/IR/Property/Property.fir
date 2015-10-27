#type_table(
    #type_reference("Property")
);

#method_table(
    #accessor_reference(
        #type_table_reference(0), "y", 1, #int(32), { }, Get)
);

#assembly(
    #member(Property),
    #version(1, 0, 0, 0),
    #null,
    {
        #type_definition(
            #member(Property, #public),
            { },
            { },
            { },
            {
                #method(
                    #member(x, #public, #const),
                    { }, 0, #int(32), { }, { },
                    {
                        #return(#invoke(#get_delegate(#method_table_reference(0), #null)));
                    }
                );
                #property(
                    #member(y, #public),
                    1, #int(32), { },
                    {
                        #accessor(
                            #member(get_y, #public, #const),
                            Get, 1, #int(32), { }, { },
                            {
                                #return(#const_int32(1));
                            }
                        );
                    }
                );
            }
        );
    },
    {

    }
);
