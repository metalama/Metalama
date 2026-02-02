// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SyntaxGeneration;
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
                MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, this.ToSyntax(), SyntaxFactoryEx.SafeIdentifierName( member ) )
                    .WithAspectReferenceAnnotation( this.AspectReferenceSpecification ),
                this.Type,
                TemplateExpansionContext.CurrentSyntaxSerializationContext.CompilationModel,
                canBeNull: false );

        public static bool TryCreate(
            IDeclaration? currentDeclaration,
            in AspectReferenceSpecification aspectReferenceSpecification,
            bool throwOnError,
            out InstanceUserReceiver? receiver )
        {
            switch ( currentDeclaration?.DeclarationKind )
            {
                // Parameters
                case DeclarationKind.Parameter when currentDeclaration is IParameter { DeclaringMember: { } m }:
                    return TryCreate( m, aspectReferenceSpecification, throwOnError, out receiver );

                // Extension member in extension block.
                case DeclarationKind.Method or DeclarationKind.Property or DeclarationKind.Event or DeclarationKind.Field or DeclarationKind.Indexer
                    when currentDeclaration is IMember { IsStatic: false, DeclaringType: { TypeKind: TypeKind.Extension } and IExtensionBlock e } m:
                    {
                        if ( !m.IsStatic )
                        {
                            receiver = new ReceiverParameterUserReceiver( e.ReceiverParameter, aspectReferenceSpecification );

                            return true;
                        }
                        else if ( throwOnError )
                        {
                            throw TemplatingDiagnosticDescriptors.NoReceiverInCurrentContext.CreateException(
                                (currentDeclaration,
                                 currentDeclaration.DeclarationKind, (FormattableString) $"the target {m.DeclarationKind} is static") );
                        }
                        else
                        {
                            receiver = null;

                            return false;
                        }
                    }

                // Instance member.
                case DeclarationKind.Method or DeclarationKind.Property or DeclarationKind.Event or DeclarationKind.Field or DeclarationKind.Indexer or DeclarationKind.Constructor
                    when currentDeclaration is IMember { IsStatic: false, DeclaringType: { } type }:
                    receiver = new ThisInstanceUserReceiver( type, aspectReferenceSpecification );

                    return true;

                // Extension method.
                case DeclarationKind.Method when currentDeclaration is IMethod { IsStatic: true, Parameters.Count: > 0 } m && m.Parameters[0].IsThis:
                    receiver = new ReceiverParameterUserReceiver( m.Parameters[0], aspectReferenceSpecification );

                    return true;

                // No current declaration. This should happen only in unit tests.
                case null:
                    throw new AssertionFailedException( $"Cannot get a receiver reference because there is no advising context." );

                default:
                    if ( throwOnError )
                    {
                        var member = currentDeclaration.GetClosestMemberOrNamedType();

                        if ( member != null )
                        {
                            throw TemplatingDiagnosticDescriptors.NoReceiverInCurrentContext.CreateException(
                                (currentDeclaration,
                                 currentDeclaration.DeclarationKind,
                                 (FormattableString) $"the target {member.DeclarationKind} is static and is not an extension method") );
                        }
                        else
                        {
                            throw TemplatingDiagnosticDescriptors.NoReceiverInCurrentContext.CreateException(
                                (currentDeclaration,
                                 currentDeclaration.DeclarationKind,
                                 (FormattableString) $"the target {currentDeclaration.DeclarationKind} is neither a member nor a parameter") );
                        }
                    }
                    else
                    {
                        receiver = null;

                        return false;
                    }
            }
        }
    }
}