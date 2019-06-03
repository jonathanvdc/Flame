using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flame;
using Flame.Collections;
using Flame.Compiler;
using Flame.Compiler.Pipeline;
using Flame.Compiler.Transforms;
using Flame.Ir;
using Flame.TypeSystem;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Pixie;

namespace UnitTests.Flame.Compiler
{
    [TestFixture]
    public class OptimizerTests
    {
        public OptimizerTests(ILog log)
        {
            this.log = log;
            this.decoder = new DecoderState(log, new TypeResolver().ReadOnlyView);
        }

        private ILog log;
        private DecoderState decoder;

        [Test]
        public void OptimizeTrivial()
        {
            var asm = DecodeAssembly(@"
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

            var optimizer = new OnDemandOptimizer(EmptyArray<Optimization>.Value);
            var factorial = asm.Types.Single().Methods.Single();
            Assert.AreEqual(
                OnDemandOptimizer.GetInitialMethodBodyDefault(factorial),
                optimizer.GetBodyAsync(factorial).Result);
        }

        [Test]
        public void OptimizeSimple()
        {
            var asm = DecodeAssembly(@"
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

            var optimizer = new OnDemandOptimizer(new Optimization[]
            {
                AllocaToRegister.Instance,
                new JumpThreading(true)
            });

            var id = asm.Types.Single().Methods.Single();
            Assert.AreNotEqual(
                OnDemandOptimizer.GetInitialMethodBodyDefault(id),
                optimizer.GetBodyAsync(id).Result);
        }

        [Test]
        public void OptimizeRecursiveMethod()
        {
            var asm = DecodeAssembly(@"
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

            var factorial = asm.Types.Single().Methods.Single();
            var optimizer = new OnDemandOptimizer(
                new[]
                {
                    new BlockingOptimization(factorial)
                });

            Assert.AreEqual(
                OnDemandOptimizer.GetInitialMethodBodyDefault(factorial),
                optimizer.GetBodyAsync(factorial).Result);
        }

        [Test]
        public void OptimizeDependency()
        {
            var asm = DecodeAssembly(@"
                #assembly(Test, {
                    #type(Float64, #(), #(), {
                        #fn(Id1, true, #(), Float64, #(#param(Float64, value)), #(), {
                            #entry_point(ep, #(#param(Float64, value)), {

                            }, #return(copy(Float64)(value)));
                        });

                        #fn(Id2, true, #(), Float64, #(#param(Float64, value)), #(), {
                            #entry_point(ep, #(#param(Float64, value)), {
                                ptr = alloca(Float64)();
                                value_copy1 = store(Float64)(ptr, value);
                                value_copy2 = load(Float64)(ptr);
                                ret_val = call(Float64.Id1(Float64) => Float64, static)(value_copy2);
                            }, #return(copy(Float64)(ret_val)));
                        });
                    });
                });");

            var id1 = asm.Types.Single().Methods.Single(m => m.Name.ToString() == "Id1");
            var id2 = asm.Types.Single().Methods.Single(m => m.Name.ToString() == "Id2");

            var optimizer1 = new OnDemandOptimizer(new Optimization[]
            {
                new BlockingOptimization(id1),
                AllocaToRegister.Instance,
                new JumpThreading(true)
            });

            optimizer1.GetBodyAsync(id1).Wait();
            Assert.AreNotEqual(
                OnDemandOptimizer.GetInitialMethodBodyDefault(id2),
                optimizer1.GetBodyAsync(id2).Result);

            var optimizer2 = new OnDemandOptimizer(new Optimization[]
            {
                new BlockingOptimization(id1),
                AllocaToRegister.Instance,
                new JumpThreading(true)
            });

            Assert.AreNotEqual(
                OnDemandOptimizer.GetInitialMethodBodyDefault(id2),
                optimizer2.GetBodyAsync(id2).Result);
        }

        private IAssembly DecodeAssembly(string lesCode)
        {
            return decoder.DecodeAssembly(Les3LanguageService.Value.ParseSingle(lesCode));
        }

        private sealed class BlockingOptimization : Optimization
        {
            public BlockingOptimization(params IMethod[] dependencies)
            {
                this.Dependencies = dependencies;
            }

            public BlockingOptimization(IReadOnlyList<IMethod> dependencies)
            {
                this.Dependencies = dependencies;
            }

            public IReadOnlyList<IMethod> Dependencies { get; private set; }

            public override bool IsCheckpoint => true;

            public override async Task<MethodBody> ApplyAsync(MethodBody body, OptimizationState state)
            {
                await state.GetBodiesAsync(Dependencies);
                return body;
            }
        }
    }
}
