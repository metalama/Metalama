// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using SpecialType = Microsoft.CodeAnalysis.SpecialType;

namespace Metalama.Framework.Engine.CodeModel.Invokers;

internal sealed class ValueArrayExpression : UserExpression
{
    private readonly ParameterList _parent;

    public ValueArrayExpression( IParameterList parent )
    {
        this._parent = (ParameterList) parent;
    }

    protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
    {
        var syntaxGenerator = syntaxSerializationContext.SyntaxGenerator;

        return syntaxGenerator.ArrayCreationExpression(
            syntaxGenerator.TypeSyntax( SpecialType.System_Object ),
            this._parent.SelectAsReadOnlyList(
                p =>
                    p.RefKind.IsReadable()
                        ? SyntaxFactory.IdentifierName( p.Name )
                        : (SyntaxNode) syntaxGenerator.DefaultExpression( p.Type ) ) );
    }

    public override IType Type => this._parent.Compilation.Factory.GetTypeByReflectionType( typeof(object[]) );
}