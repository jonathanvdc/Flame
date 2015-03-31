using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Options
{
    public class TransformingOptionParser<TSource, TTransformed> : IOptionParser<TSource>
    {
        public TransformingOptionParser(IOptionParser<TTransformed> Parser, Func<TSource, TTransformed> Transformation)
        {
            this.Parser = Parser;
            this.Transformation = Transformation;
        }

        public IOptionParser<TTransformed> Parser { get; private set; }
        public Func<TSource, TTransformed> Transformation { get; private set; }

        public T ParseValue<T>(TSource Value)
        {
            return Parser.ParseValue<T>(Transformation(Value));
        }

        public bool CanParse<T>()
        {
            return Parser.CanParse<T>();
        }
    }
}
