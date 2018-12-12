using System;
using Flame;
using Flame.Constants;
using Flame.Ir;
using Flame.TypeSystem;
using Loyc;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Pixie;

namespace UnitTests.Flame.Ir
{
    [TestFixture]
    public class AssemblyCodecTest
    {
        public AssemblyCodecTest(ILog log)
        {
            this.log = log;
            this.decoder = new DecoderState(log, new TypeResolver().ReadOnlyView);
            this.encoder = new EncoderState();
        }

        private ILog log;

        private DecoderState decoder;

        private EncoderState encoder;

        [Test]
        public void RoundTripEmptyAssembly()
        {
            AssertRoundTripAssembly("#assembly(Test, { });");
        }

        [Test]
        public void RoundTripTypeAssemblyWithSingleType()
        {
            AssertRoundTripAssembly("#assembly(Test, { #type(A, #(), #(), { }); });");
        }

        [Test]
        public void RoundTripAssemblyWithMultipleTypes()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(A, #(), #(), { });
                    #type(B, #(), #(), { });
                });");
        }

        [Test]
        public void RoundTripAssemblyWithTypeRefs()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(A, #(), #(), { });
                    #type(B, #(), #(A), { });
                });");
        }

        [Test]
        public void RoundTripAssemblyWithNamespacedType()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(TestNamespace::A, #(), #(), { });
                });");
        }

        [Test]
        public void RoundTripAssemblyWithNestedType()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(A, #(), #(), {
                        #type(B, #(), #(), { });
                    });
                });");
        }

        [Test]
        public void RoundTripAssemblyWithTypeParameter()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(A(1), #(#type_param(T, #(), #(), { })), #(), { });
                    #type(B(1), #(#type_param(T, #(), #(), { })), #(#of(A(1), #type(B(1))->T)), { });
                });");
        }

        [Test]
        public void RoundTripAssemblyWithField()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(A, #(), #(), {
                        #var(Instance, true, A);
                    });
                });");
        }

        [Test]
        public void RoundTripAssemblyWithNestedTypeAndField()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(A, #(), #(), {
                        #type(B, #(), #(), { });
                        #var(Instance, true, A);
                    });
                });");
        }

        [Test]
        public void RoundTripAssemblyWithMethod()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(Float64, #(), #(), {
                        #fn(GetPi, false, #(), Float64, #(), #(), {
                            #entry_point(ep, #(), {
                                result = const(3.1415, Float64)();
                            }, #return(copy(Float64)(result)));
                        });
                    });
                });");
        }

        [Test]
        public void RoundTripAssemblyWithParameterizedMethod()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(Float64, #(), #(), {
                        #fn(Id, true, #(), Float64, #(#param(Float64, value)), #(), {
                            #entry_point(ep, #(#param(Float64, value)), {
                                ptr = alloca(Float64)();
                                value_copy = store(Float64)(ptr, value);
                                result = load(Float64)(ptr);
                            }, #goto(ret_block(result)));

                            #block(ret_block, #(#param(Float64, ret_val)), {

                            }, #return(copy(Float64)(ret_val)));
                        });
                    });
                });");
        }

        [Test]
        public void RoundTripFactorial()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(Int32, #(), #(), {
                        #fn(Factorial, true, #(), Int32, #(#param(Int32, value)), #(), {
                            #entry_point(ep, #(#param(Int32, value)), {

                            }, #switch(
                                copy(Int32)(value),
                                recurse(), {
                                    #case(#(0, 1), base_case());
                                }));

                            #block(base_case, #(), {

                            }, #return(const(1, Int32)()));

                            #block(recurse, #(), {
                                one = const(1, Int32)();
                                value_minus_one = intrinsic(`int.add`, Int32, #(Int32, Int32))(value, one);
                                prev_fac = call(Int32.Factorial(Int32) => Int32, static)(value_minus_one);
                                result = intrinsic(`int.mul`, Int32, #(Int32, Int32))(prev_fac, value);
                            }, #return(copy(Int32)(result)));
                        });
                    });
                });");
        }

        [Test]
        public void RoundTripFieldReference()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(Int32, #(), #(), {
                        #var(Predecessor, false, Int32);
                        #fn(GetPredecessor, false, #(), Int32, #(), #(), {
                            #entry_point(ep, #(#param(#pointer(Int32, box), this_ptr)), {
                                field_ptr = get_field_pointer(Int32.Predecessor)(this_ptr);
                                value = load(Int32)(field_ptr);
                            }, #return(copy(Int32)(value)));
                        });
                    });
                });");
        }

        private void AssertRoundTripAssembly(string lesCode)
        {
            AssertRoundTripAssembly(StripTrivia(Les3LanguageService.Value.ParseSingle(lesCode)));
        }

        private void AssertRoundTripAssembly(LNode node)
        {
            ConstantCodecTest.AssertRoundTrip<LNode, IAssembly>(
                node,
                decoder.DecodeAssembly,
                encoder.EncodeDefinition);
        }

        private LNode StripTrivia(LNode node)
        {
            var strippedNode = node.WithAttrs(
                attr => attr.IsTrivia ? Maybe<LNode>.NoValue : new Maybe<LNode>(attr));

            if (strippedNode.IsCall)
            {
                return strippedNode
                    .WithTarget(StripTrivia(strippedNode.Target))
                    .WithArgs(arg => new Maybe<LNode>(StripTrivia(arg)));
            }
            else
            {
                return strippedNode;
            }
        }
    }
}