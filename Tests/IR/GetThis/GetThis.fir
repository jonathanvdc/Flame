
#assembly(
    #member(GetThis),
    #version(1, 0, 0, 0),
    #null,
    {
        #type_definition(
            #member(GetThis, #public),
            { },
            { },
            { },
            {
                #method(#member(getThis, #public, #const),
                        { }, 0, #type_reference("GetThis"), { }, { },
                        #return(#get_this()));
            }
        );
    },
    {

    }
);
