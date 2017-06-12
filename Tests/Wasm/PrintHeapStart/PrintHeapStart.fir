#type_table({
	// 0: spectest
	#type_reference(spectest);
	// 1: System.Int32
	#int(32);
	// 2: Program
	#type_reference(Program);
	// 3: System.Void
	#void;
});
#method_table({
	// 0: static System.Void spectest.print(System.Int32)
	#method_reference(#type_table_reference(0), print, @true, { }, #void, {
		#type_table_reference(1);
	});
	// 1: static System.Void Program.Main()
	#method_reference(#type_table_reference(2), Main, @true, { }, #void, { });
});
#field_table({ });
#assembly(#member(PrintHeapStart), #version(1, 0, 0, 0), #method_table_reference(1), {
	#type_definition(#member(Program, #public, #static_type), { }, { }, { }, {
		#method(#member(Main, #public), { }, @true, #type_table_reference(3), { }, { }, {
			#ignore(#invoke(#get_delegate(#method_table_reference(0), @null), #chunk_address(heap, heap_start)));
			#return();
		});
	});
	#type_definition(#member(spectest, #public, #static_type), { }, { }, { }, {
		#method(#member(print, #public, #import), { }, @true, #type_table_reference(3), {
			#param(#member(Value), #type_table_reference(1));
		}, { });
	});
}, { });