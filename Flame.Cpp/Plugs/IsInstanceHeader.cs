using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Plugs
{
    public class IsInstanceHeader : IHeaderDependency
    {
        private IsInstanceHeader()
        {

        }

        private static IsInstanceHeader inst;
        public static IsInstanceHeader Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new IsInstanceHeader();
                }
                return inst;
            }
        }

        private const string Code =
@"#pragma once
#include <memory>

namespace stdx
{
	template<typename TTarget, typename TSource>
	inline bool isinstance(const TSource* Pointer)
	{
		return dynamic_cast<const TTarget*>(Pointer) != nullptr;
	}

	template<typename TTarget, typename TSource>
	inline bool isinstance(const TSource& Reference)
	{
		return isinstance<TTarget, TSource>(&Reference);
	}

	template<typename TTarget, typename TSource>
	inline bool isinstance(std::shared_ptr<TSource> const& Pointer)
	{
		return std::dynamic_pointer_cast<TTarget, TSource>(Pointer) != nullptr;
	}
}";

        public void Include(IOutputProvider OutputProvider)
        {
            if (!OutputProvider.Exists("IsInstance", "h"))
            {
                var cb = new CodeBuilder(Code); // Format code first.
                cb.IndentationString = new string(' ', 4);

                var handle = OutputProvider.Create("IsInstance", "h");
                using (var stream = handle.OpenOutput())
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(cb.ToString());
                }
            }
        }

        public bool IsStandard
        {
            get { return false; }
        }

        public string HeaderName
        {
            get { return "IsInstance.h"; }
        }
    }
}
