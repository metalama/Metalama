// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Caching;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Templating.Expressions
{
    internal sealed class InterpolatedStringUserExpression : UserExpression
    {
        private readonly InterpolatedStringBuilder _builder;

        public InterpolatedStringUserExpression( InterpolatedStringBuilder builder, ICompilation compilation )
        {
            this._builder = builder;
            this.Type = compilation.GetCompilationModel().Cache.SystemStringType;
        }

        protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
        {
            List<InterpolatedStringContentSyntax> contents = new( this._builder.Items.Count );

            using var stringBuilderHandle = StringBuilderPool.Default.Allocate();
            var textAccumulator = stringBuilderHandle.Value;

            void FlushTextToken()
            {
                if ( textAccumulator.Length > 0 )
                {
                    var text = textAccumulator.ToString()
                        .ReplaceOrdinal( "{", "{{" )
                        .ReplaceOrdinal( "}", "}}" );

                    var literal = SyntaxFactory.Literal( text );
                    var escapedText = literal.Text.Substring( 1, literal.Text.Length - 2 );

                    contents.Add(
                        SyntaxFactory.InterpolatedStringText(
                            SyntaxFactory.Token( default, SyntaxKind.InterpolatedStringTextToken, escapedText, text, default ) ) );

                    textAccumulator.Length = 0;
                }
            }

            foreach ( var content in this._builder.Items )
            {
                switch ( content )
                {
                    case string text:
                        textAccumulator.Append( text );

                        break;

                    case InterpolatedStringBuilder.Token token:

                        FlushTextToken();

                        var tokenSyntax = TypedExpressionSyntaxImpl.FromValue( token.Expression, syntaxSerializationContext ).Syntax;

                        if ( tokenSyntax is LiteralExpressionSyntax literal && literal.Token.IsKind( SyntaxKind.StringLiteralToken ) &&
                             token.Alignment is null && token.Format is null )
                        {
                            textAccumulator.Append( literal.Token.Text.Substring( 1, literal.Token.Text.Length - 2 ) );
                        }
                        else
                        {
                            var alignmentClause = token.Alignment is null
                                ? null
                                : SyntaxFactory.InterpolationAlignmentClause(
                                    SyntaxFactory.Token( SyntaxKind.CommaToken ),
                                    SyntaxFactoryEx.LiteralExpression( token.Alignment.Value ) );

                            var formatClause = token.Format is null
                                ? null
                                : SyntaxFactory.InterpolationFormatClause(
                                    SyntaxFactory.Token( SyntaxKind.ColonToken ),
                                    SyntaxFactory.Token( default, SyntaxKind.InterpolatedStringTextToken, token.Format, token.Format, default ) );

                            var interpolation = InterpolationSyntaxHelper.Fix( SyntaxFactory.Interpolation( tokenSyntax, alignmentClause, formatClause ) );
                            contents.Add( interpolation );
                        }

                        break;

                    default:
                        throw new AssertionFailedException( $"Unexpected content type: {content?.GetType()}." );
                }
            }

            FlushTextToken();

            return syntaxSerializationContext.SyntaxGenerator.RenderInterpolatedString(
                SyntaxFactory.InterpolatedStringExpression(
                    SyntaxFactory.Token( SyntaxKind.InterpolatedStringStartToken ),
                    SyntaxFactory.List( contents ),
                    SyntaxFactory.Token( SyntaxKind.InterpolatedStringEndToken ) ) );
        }

        protected override bool CanBeNull => false;

        public override IType Type { get; }
    }
}