// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Templating.Expressions
{
    /// <summary>
    /// An implementation of <see cref="UserExpression"/> that represents a <see cref="INamedType"/> and allows to access
    /// its static members dynamically.
    /// </summary>
    internal sealed class ThisTypeUserReceiver : UserReceiver
    {
        private readonly INamedType _type;

        public ThisTypeUserReceiver( INamedType type, in AspectReferenceSpecification linkerAnnotation ) : base( linkerAnnotation )
        {
            this._type = type;
        }

        protected override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
            => syntaxSerializationContext.SyntaxGenerator.TypeSyntax( this._type );

        public override IType Type => this._type;

        public override TypedExpressionSyntaxImpl CreateMemberAccessExpression( string member )
            => new(
                SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        TemplateExpansionContext.CurrentSyntaxGenerationContext.SyntaxGenerator.TypeExpression( this._type ),
                        SyntaxFactory.IdentifierName( SyntaxFactory.Identifier( member ) ) )
                    .WithAspectReferenceAnnotation( this.AspectReferenceSpecification ),
                TemplateExpansionContext.CurrentSyntaxSerializationContext.CompilationModel );

        protected override UserReceiver WithAspectReferenceSpecification( AspectReferenceSpecification spec ) => new ThisTypeUserReceiver( this._type, spec );

        protected override bool CanBeNull => false;
    }
}