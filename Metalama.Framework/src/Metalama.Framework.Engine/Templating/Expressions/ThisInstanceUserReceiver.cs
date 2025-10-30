// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Templating.Expressions;

internal class ThisInstanceUserReceiver : InstanceUserReceiver
{
    private readonly INamedType _type;

    public ThisInstanceUserReceiver( INamedType type, in AspectReferenceSpecification aspectReferenceSpecification ) : base( aspectReferenceSpecification )
    {
        this._type = type;
    }

    protected override ExpressionSyntax ToSyntax() => SyntaxFactory.ThisExpression();

    protected override bool? IsAssignable => this._type.TypeKind is TypeKind.Struct or TypeKind.Extension;

    private protected override bool? IsReferenceable => this.IsAssignable;

    public override IType Type => this._type;

    protected override UserReceiver WithAspectReferenceSpecification( AspectReferenceSpecification spec ) => new ThisInstanceUserReceiver( this._type, spec );

    protected override bool CanBeNull => false;
}