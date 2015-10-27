// Compile with `-recompilation-strategy conservative`
// (fields will be eliminated otherwise)

#assembly(
    #member(Private),
    #version(1, 0, 0, 0),
    #null,
    {
        #type_definition(
            #member(Private, #public),
            { },
            { },
            { },
            {
                #field(#member(x, #private), 0, #int(32));
                #field(#member(y, #private), 0, #int(32));
            }
        );
    },
    {

    }
);
