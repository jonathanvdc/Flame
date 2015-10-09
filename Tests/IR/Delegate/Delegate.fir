#type_table(
    #type_reference("Delegate"),
    #delegate_type(#int(32), #int(32))
);

#method_table(
    #method_reference(#type_table_reference(0), "y", 1, { }, #int(32), { #int(32) });
);

#assembly(
    #member(Delegate),
    #version(1, 0, 0, 0),
    #null,
    {
        #type_definition(
            #member(Delegate, #public),
            { },
            { },
            { },
            {
                #method(
                    #member(x, #public, #const),
                    { }, 0, #type_table_reference(1), { }, { },
                    {
                        #return(#get_delegate(#method_table_reference(0), #null));
                    }
                );
                #method(
                    #member(y, #public, #const),
                    { }, 1, #int(32), { #param(#member(a), #int(32)) }, { },
                    {
                        #return(#const_int32(1));
                    }
                );
            }
        );
    },
    {

    }
);
