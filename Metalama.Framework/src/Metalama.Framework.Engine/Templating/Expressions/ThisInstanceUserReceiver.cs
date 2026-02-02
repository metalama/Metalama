// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Templating.Expressions;

internal sealed class ThisInstanceUserReceiver : InstanceUserReceiver
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

    public static bool TryCreate(
        IDeclaration? currentDeclaration,
        in AspectReferenceSpecification aspectReferenceSpecification,
        bool throwOnError,
        [NotNullWhen( true )] out ThisInstanceUserReceiver? receiver )
    {
        switch ( currentDeclaration?.DeclarationKind )
        {
            // Parameters
            case DeclarationKind.Parameter when currentDeclaration is IParameter { DeclaringMember: { } m }:
                return TryCreate( m, aspectReferenceSpecification, throwOnError, out receiver );

            // Instance member.
            case DeclarationKind.Method or DeclarationKind.Property or DeclarationKind.Event or DeclarationKind.Field or DeclarationKind.Indexer or DeclarationKind.Constructor
                when currentDeclaration is IMember { IsStatic: false, DeclaringType: { } type }:
                receiver = new ThisInstanceUserReceiver( type, aspectReferenceSpecification );

                return true;

            // No current declaration. This should happen only in unit tests.
            case null:
                throw new AssertionFailedException( $"Cannot create use 'this'  because there is no advising context." );

            default:
                if ( throwOnError )
                {
                    var member = currentDeclaration.GetClosestMemberOrNamedType();

                    if ( member != null )
                    {
                        throw TemplatingDiagnosticDescriptors.CannotUseThisInStaticContext.CreateException(
                            (currentDeclaration,
                             currentDeclaration.DeclarationKind,
                             (FormattableString) $"the target {member.DeclarationKind} is static") );
                    }
                    else
                    {
                        throw TemplatingDiagnosticDescriptors.CannotUseThisInStaticContext.CreateException(
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