using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class StandardDependency : IHeaderDependency
    {
        public StandardDependency(string HeaderName)
        {
            this.HeaderName = HeaderName;
        }

        public bool IsStandard
        {
            get { return true; }
        }

        public string HeaderName { get; private set; }

        public void Include(IOutputProvider OutputProvider)
        {
        }

        #region Static

        public static IHeaderDependency Functional
        {
            get
            {
                return new StandardDependency("functional");
            }
        }

        public static IHeaderDependency Memory
        {
            get
            {
                return new StandardDependency("memory");
            }
        }

        public static IHeaderDependency Vector
        {
            get
            {
                return new StandardDependency("vector");
            }
        }

        public static IHeaderDependency String
        {
            get
            {
                return new StandardDependency("string");
            }
        }

        public static IHeaderDependency InitializerList
        {
            get
            {
                return new StandardDependency("initializer_list");
            }
        }

        public static IHeaderDependency CAssert
        {
            get
            {
                return new StandardDependency("cassert");
            }
        }

        public static IHeaderDependency Limits
        {
            get
            {
                return new StandardDependency("limits");
            }
        }

        #endregion
    }
}
