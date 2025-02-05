// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Code.SyntaxBuilders
{
    public sealed partial class InterpolatedStringBuilder
    {
        internal sealed class Token
        {
            public object? Expression { get; }

            public int? Alignment { get; }

            public string? Format { get; }

            public Token( object? expression, int? alignment, string? format )
            {
                this.Expression = expression;
                this.Alignment = alignment;
                this.Format = format;
            }
        }
    }
}