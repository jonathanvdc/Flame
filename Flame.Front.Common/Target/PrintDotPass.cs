using System;
using Flame.Optimization;
using System.IO;
using Flame.Compiler;
using Flame.Front.Plugs;

namespace Flame.Front.Target
{
	/// <summary>
	/// The default -dot-cfg pass.
	/// </summary>
	public class PrintDotPass : PrintDotPassBase
	{
		private PrintDotPass() 
		{ }

		public static readonly PrintDotPass Instance = new PrintDotPass();

		public const string PrintDotTreePassName = "dot-tree";
		public const string PrintDotOptimizedPassName = "dot-opt-cfg";

		protected override TextWriter TryOpen(IMethod Method, ICompilerLog Log)
		{
            string name = "cfg." + PlugHandler.ToValidPath(Method.FullName.ToString()) + ".dot";
			var stream = new FileStream(name, FileMode.Create, FileAccess.Write);
			return new StreamWriter(stream);
		}

		protected override void Close(TextWriter Writer, IMethod Method, ICompilerLog Log)
		{
			Writer.Dispose();
		}
	}
}

