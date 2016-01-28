using System;
using System.Linq;
using Pixie;
using Flame.Compiler;

namespace Flame.Front
{
	/// <summary>
	/// A markup node visitor that finds the first 'source-location' attribute that
	/// belongs to a 'diagnostics' node. All other 'source-location' that meet the
	/// same criteria are transformed into 'remark' nodes.
	/// </summary>
	public class SourceLocationFinder : MarkupNodeVisitorBase
	{
		public SourceLocationFinder()
		{
			this.FirstSourceLocation = null;
		}

		/// <summary>
		/// Gets the first source location attached to a diagnostics node. 
		/// Null is returned if no source location was found.
		/// </summary>
		/// <value>The first source location. Null if no source location was found.</value>
		public SourceLocation FirstSourceLocation { get; private set; }

		protected override bool Matches(MarkupNode Node)
		{
			return Node.Type == NodeConstants.DiagnosticsNodeType;
		}

		protected override MarkupNode Transform(MarkupNode Node)
		{
			var srcLoc = Node.Attributes.Get<SourceLocation>(
				NodeConstants.SourceLocationAttribute, null);
			
			if (srcLoc == null || srcLoc.Document == null)
				// Not interesting.
				return Accept(Node);

			if (FirstSourceLocation == null)
			{
				// We found the first source location.
				FirstSourceLocation = srcLoc;
				return Accept(Node);
			}
			else
			{
				// This may be interesting, but it is not the first source location.
				// Rewrite it in a human-friendly format.
				var newChildren = Visit(Node.Children).Concat(new MarkupNode[] 
				{ 
					new MarkupNode(NodeConstants.RemarksNodeType, new MarkupNode[]
					{
						new MarkupNode(NodeConstants.TextNodeType, "In "),
						CompilerLogExtensions.CreateLineNumberNode(srcLoc),
						new MarkupNode(NodeConstants.TextNodeType, ".")
					}) 
				});
				return new MarkupNode(Node.Type, Node.Attributes, newChildren);
			}
		}
	}
}

