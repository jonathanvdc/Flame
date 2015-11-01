
#assembly(
    #member(ImperativeCode),
    #version(1, 0, 0, 0),
    #null,
    {
        #type_definition(
            #member(ImperativeCode, #public),
            { },
            { },
            { },
            {
            #method(
                #member(one, #public, #const),
                { }, 0, #int(32), { }, { },
                {
                    #return(#def_local(x, #member(x), #int(32),
                    {
                        #set_local(x, #const_int32(1));
                        #get_local(x);
                    }));
                });

            #method(
                #member(danglingPointer, #public),
                { }, 0, #pointer_type(#int(32), "*"), { }, { },
                {
                    #return(#def_local(x, #member(x), #int(32),
                    {
                        #set_local(x, #const_int32(1));
                        #addressof_local(x);
                    }));
                });
            #method(
                #member(releaseLocal, #public),
                { }, 0, #int(32), { }, { },
                {
                    #return(#def_local(x, #member(x), #int(32),
                    {
                        #set_local(x, #const_int32(1));
                        #def_local(y, #member(y), #int(32),
                        {
                            #release_local(x);
                            #set_local(y, #const_int32(1));
                            #get_local(y);
                        });
                    }));
                });
            }
        );
    },
    { }
);
