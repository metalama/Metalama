// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Templating.Expressions;

internal sealed class SuppressNullableWarningUserExpression : UserExpression
{
    private readonly IUserExpression _underlying;

    public SuppressNullableWarningUserExpression( IUserExpression underlying )
    {
        this._underlying = underlying;
    }

    protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
    {
        var underlyingSyntax = this._underlying.ToTypedExpressionSyntax( syntaxSerializationContext, targetType );

        return syntaxSerializationContext.SyntaxGenerator.SuppressNullableWarningExpression( underlyingSyntax.Syntax, this._underlying.Type );
    }

    // We don't change the type to non-nullable because it would change the behavior of our null-conditional invokers, which rely
    // on the type being nullable. Also, ! does not change the nullability, but just suppresses the warning.
    public override IType Type => this._underlying.Type;
}