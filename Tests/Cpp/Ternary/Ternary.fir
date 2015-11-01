
#assembly(#member(Ternary), #version(1, 0, 0, 0), @null, {
	#type_definition(#member(Ternary, #public, #reference_type), {}, {}, {}, {
		#method(#member(Ternary, #public), {}, @false, #int(32), {
			#param(#member(Condition), #bool);
			#param(#member(First), #int(32));
			#param(#member(Second), #int(32));
		}, {}, #return(#select(#get_arg(0), #get_arg(1), #get_arg(2))));
		#ctor(#member(this, #public, #const), {}, @false, #void, {}, {}, #return());
	});
}, {});
