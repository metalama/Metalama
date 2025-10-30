// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SyntaxSerialization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Templating.Expressions
{
    /// <summary>
    /// An implementation of <see cref="UserExpression"/> that represents <c>this</c> and allows to access its instance members dynamically.
    /// </summary>
    internal abstract class InstanceUserReceiver : UserReceiver
    {
        protected InstanceUserReceiver( in AspectReferenceSpecification aspectReferenceSpecification ) : base( in aspectReferenceSpecification ) { }

        protected abstract ExpressionSyntax ToSyntax();

        protected sealed override ExpressionSyntax ToSyntax( SyntaxSerializationContext syntaxSerializationContext, IType? targetType = null )
            => this.ToSyntax();

        public override TypedExpressionSyntaxImpl CreateMemberAccessExpression( string member )
            => new(
                MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, this.ToSyntax(), IdentifierName( Identifier( member ) ) )
                    .WithAspectReferenceAnnotation( this.AspectReferenceSpecification ),
                this.Type,
                TemplateExpansionContext.CurrentSyntaxSerializationContext.CompilationModel,
                canBeNull: false );

        public static InstanceUserReceiver Create(
            IDeclaration? currentDeclaration,
            in AspectReferenceSpecification aspectReferenceSpecification,
            string expressionName )
        {
            switch ( currentDeclaration )
            {
                // Parameters
                case IParameter { DeclaringMember: { } m }:
                    return Create( m, aspectReferenceSpecification, expressionName );

                // Extension member in extension block.
                case IMember { IsStatic: false, DeclaringType: { TypeKind: TypeKind.Extension } and IExtensionBlock e }:
                    return new ReceiverParameterUserReceiver( e.ReceiverParameter, aspectReferenceSpecification );

                // Instance member.
                case IMember { IsStatic: false, DeclaringType: { } type }:
                    return new ThisInstanceUserReceiver( type, aspectReferenceSpecification );

                // Extension method.
                case IMethod { IsStatic: true, Parameters.Count: > 0 } m when m.Parameters[0].IsThis:
                    return new ReceiverParameterUserReceiver( m.Parameters[0], aspectReferenceSpecification );

                // No current declaration. This should happen only in unit tests.
                case null:
                    throw new AssertionFailedException( $"Cannot create use '{expressionName}' expression because there is no advising context." );

                default:
                    var explanation = currentDeclaration is IParameter { DeclaringMember: not null } parameter
                        ? (FormattableString) $"the target parameter is contained in a static {parameter.DeclaringMember.DeclarationKind}"
                        : $"the target {currentDeclaration.DeclarationKind} is static";

                    throw TemplatingDiagnosticDescriptors.CannotUseThisInStaticContext.CreateException(
                        (expressionName, currentDeclaration,
                         currentDeclaration.DeclarationKind, explanation) );
            }
        }
    }
}