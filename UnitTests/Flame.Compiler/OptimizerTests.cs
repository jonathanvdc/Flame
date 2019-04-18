using System.Linq;
using Flame;
using Flame.Collections;
using Flame.Compiler.Pipeline;
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

        private IAssembly DecodeAssembly(string lesCode)
        {
            return decoder.DecodeAssembly(Les3LanguageService.Value.ParseSingle(lesCode));
        }
    }
}
