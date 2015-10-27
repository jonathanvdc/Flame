#type_table(
    #type_reference("Call");
);

#method_table(
    #method_reference(#type_table_reference(0), "y", 1, { }, #int(32), { });
);

#assembly(
    #member(Call),
    #version(1, 0, 0, 0),
    #null,
    {
        #type_definition(
            #member(Call, #public),
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
                #method(
                    #member(y, #public, #const),
                    { }, 1, #int(32), { }, { },
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
