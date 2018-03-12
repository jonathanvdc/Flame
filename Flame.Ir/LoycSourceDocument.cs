using System.IO;
using Loyc.Syntax;
using Pixie.Code;

namespace Flame.Ir
{
    internal sealed class LoycSourceDocument : SourceDocument
    {
        public LoycSourceDocument(ISourceFile source)
        {
            this.source = source;
        }

        private ISourceFile source;

        public override string Identifier => source.FileName;

        public override int Length => source.Text.Count;

        /// <inheritdoc/>
        public override GridPosition GetGridPosition(int offset)
        {
            var linePos = source.IndexToLine(offset);
            return new GridPosition(linePos.Line - 1, linePos.PosInLine - 1);
        }

        /// <inheritdoc/>
        public override int GetLineOffset(int lineIndex)
        {
            return source.LineToIndex(lineIndex);
        }

        /// <inheritdoc/>
        public override string GetText(int offset, int length)
        {
            return source.Text.Slice(offset, length).ToString();
        }

        /// <inheritdoc/>
        public override TextReader Open(int offset)
        {
            // TODO: maybe implement this more efficiently?
            return new StringReader(GetText(offset, Length - offset));
        }
    }
}
