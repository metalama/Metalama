// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Templating.Expressions
{
    /// <summary>
    /// An implementation of <see cref="UserExpression"/> that represents <c>this</c> and allows to access its instance members dynamically.
    /// </summary>
    internal sealed class ThisInstanceUserReceiver : UserReceiver
    {
        private readonly INamedType _type;

        public ThisInstanceUserReceiver( INamedType type, in AspectReferenceSpecification aspectReferenceSpecification ) : base( aspectReferenceSpecification )
        {
            this._type = type;
        }

        protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null ) => ThisExpression();

        protected override bool? IsAssignable => this._type.TypeKind == TypeKind.Struct;

        private protected override bool? IsReferenceable => this.IsAssignable;

        public override IType Type => this._type;

        public override TypedExpressionSyntaxImpl CreateMemberAccessExpression( string member )
            => new(
                MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName( Identifier( member ) ) )
                    .WithAspectReferenceAnnotation( this.AspectReferenceSpecification ),
                this._type,
                TemplateExpansionContext.CurrentSyntaxSerializationContext.CompilationModel,
                canBeNull: false );

        protected override UserReceiver WithAspectReferenceSpecification( AspectReferenceSpecification spec )
            => new ThisInstanceUserReceiver( this._type, spec );

        protected override bool CanBeNull => false;
    }
}