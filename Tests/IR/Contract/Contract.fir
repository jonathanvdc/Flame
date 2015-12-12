#type_table({
	// 0: System.Int32
	#int(32);
	// 1: System.Void
	#void;
});
#method_table({});
#field_table({});
#assembly(#member(Contract), #version(1, 0, 0, 0), @null, {
	#type_definition(#member(TestClass, #public, #reference_type), {}, {}, {}, {
		#method(#member(TestMethod, #public), {}, @false, #type_table_reference(0), {
			#param(#member(x), #type_table_reference(0));
			#param(#member(y), #type_table_reference(0));
		}, {}, #contract(#return(#const_int32(50)), {
			#binary(#get_arg(0), ">", #get_arg(1));
			#binary(#get_arg(0), ">", #const_int32(0));
			#binary(#get_arg(1), "<", #const_int32(100));
		}, {
			#binary(#get_retval(), ">", #const_int32(0));
			#binary(#get_arg(0), ">", #const_int32(0));
		}));
		#ctor(#member(this, #public, #const), {}, @false, #type_table_reference(1), {}, {}, #return());
	});
}, {});
