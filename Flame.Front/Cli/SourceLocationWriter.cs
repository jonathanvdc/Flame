using System;
using System.Linq;
using Flame.Front.Cli;
using Pixie;
using System.Text;
using Flame.Compiler;

namespace Flame.Front
{
	public class SourceLocationWriter : INodeWriter
	{
		public SourceLocationWriter(INodeWriter MainWriter)
		{
			this.MainWriter = MainWriter;
		}

		public INodeWriter MainWriter { get; private set; }

		/// <summary>
		/// Writes the markup node to the designated output.
		/// </summary>
		/// <param name="Node"></param>
		public void Write(MarkupNode Node, IConsole Console, IStylePalette Palette)
		{
			// Rewrite source location nodes as simpler nodes.
			var loc = Node.Attributes.Get<SourceLocation>(NodeConstants.SourceLocationAttribute, new SourceLocation(null));

			if (loc.Document == null)
			{
				return;
			}

			var style = Palette.GetNamedStyle(NodeConstants.SourceLocationAttribute);
			Console.PushStyle(style);

			bool msvc = style.Preferences.Contains("msvc");
			bool vi = !msvc && style.Preferences.Contains("vi");

			var pos = loc.GridPosition;
			var text = new StringBuilder();
			text.Append(loc.Document.Identifier);
			if (pos.Line > -1)
			{
				if (vi)
					text.Append(" +");
				else if (msvc)
					text.Append('(');
				else
					text.Append(':');
				
				text.Append(pos.Line + 1);
				if (pos.Offset > -1)
				{
					if (msvc)
						text.Append(',');
					else
						text.Append(':');
					text.Append(pos.Offset + 1);
				}
			}
			if (msvc)
			{
				text.Append(')');
			}
			MainWriter.Write(new MarkupNode(NodeConstants.TextNodeType, text.ToString()), Console, Palette);

			Console.PopStyle();
		}
	}
}

