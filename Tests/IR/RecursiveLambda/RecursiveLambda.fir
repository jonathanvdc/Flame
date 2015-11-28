#type_table({
	// 0: System.Int32
	#int(32);
	// 1: A
	#type_reference("A");
	// 2: System.Void
	#void;
});
#field_table({});
#assembly(#member(RecursiveLambda), #version(1, 0, 0, 0), @null, {
	#type_definition(#member(A, #public, #reference_type), {}, {}, {}, {
		#method(#member(Herp, #public), {}, @false, #delegate_type(#type_table_reference(0), #type_table_reference(0)), {}, {},
			#return(#lambda(#member(""), #type_table_reference(0), {
				#param(#member(n), #type_table_reference(0));
			}, {}, #select(#binary(#get_arg(0), "<=", #const_int32(1)),
				#return(#const_int32(1)),
				#return(#invoke(#get_rec_lambda_delegate(), #binary(#get_arg(0), "-", #const_int32(1))))))));
		#method(#member(Derp, #public), {}, @false, #delegate_type(#type_table_reference(0), #type_table_reference(0)), {
				#param(#member(n), #type_table_reference(0));
			}, {},
				#return(#lambda(#member(""), #type_table_reference(0), {
					#param(#member(i), #type_table_reference(0));
				}, {
					#get_arg(0);
				}, #select(#binary(#get_arg(0), ">=", #const_int32(42)), #return(#const_int32(1)), #return(#invoke(#get_rec_lambda_delegate(), #binary(#get_arg(0), "+", #lambda_captured_value(0))))))));
		#ctor(#member(this, #public, #const), {}, @false, #type_table_reference(2), {}, {}, #return());
	});
}, {});
