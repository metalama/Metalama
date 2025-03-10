// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.ReferenceGraph;

/// <summary>
/// Exposes the requirements for reference indexing. Requirements are aggregated by the <see cref="ReferenceIndexerOptions"/>
/// class.
/// </summary>
public sealed record ReferenceIndexerRequirements(
    ReferenceKinds ReferenceKinds,
    bool IncludeDerivedTypes,
    DeclarationKind ValidatedDeclarationKind,
    string? ValidatedIdentifier )
{
    public static ReferenceIndexerRequirements Create(
        IDeclaration validatedDeclaration,
        ReferenceKinds referenceKinds,
        bool includeDerivedTypes )
    {
        var validatedDeclarationKind = validatedDeclaration.DeclarationKind;

        var validatedIdentifier = validatedDeclaration switch
        {
            IConstructor constructor => constructor.DeclaringType.Name,
            INamedDeclaration namedDeclaration => namedDeclaration.Name,
            _ => null
        };

        if ( referenceKinds.IsDefined( ReferenceKinds.BaseType ) && validatedDeclaration is INamedType { IsSealed: true } )
        {
            referenceKinds &= ~ReferenceKinds.BaseType;
        }

        referenceKinds &= GetReferenceKindsSupportedByDeclarationKind( validatedDeclarationKind );

        if ( includeDerivedTypes )
        {
            includeDerivedTypes = validatedDeclaration switch
            {
                INamedType namedType => !namedType.IsSealed,
                INamespace or ICompilation => true,
                _ => includeDerivedTypes
            };
        }

        return new ReferenceIndexerRequirements( referenceKinds, includeDerivedTypes, validatedDeclarationKind, validatedIdentifier );
    }

    private static ReferenceKinds GetReferenceKindsSupportedByDeclarationKind( DeclarationKind declarationKind )
        => declarationKind switch
        {
            DeclarationKind.Compilation or DeclarationKind.Namespace or DeclarationKind.NamedType or DeclarationKind.AssemblyReference => ReferenceKinds.All,
            DeclarationKind.Constructor => ReferenceKinds.BaseConstructor | ReferenceKinds.ObjectCreation,
            DeclarationKind.Event or DeclarationKind.Method => ReferenceKinds.Default | ReferenceKinds.Invocation | ReferenceKinds.NameOf
                                                               | ReferenceKinds.InterfaceMemberImplementation | ReferenceKinds.OverrideMember
                                                               | ReferenceKinds.Assignment,
            DeclarationKind.Property => ReferenceKinds.Default | ReferenceKinds.Assignment | ReferenceKinds.NameOf
                                        | ReferenceKinds.InterfaceMemberImplementation | ReferenceKinds.OverrideMember,
            DeclarationKind.Field => ReferenceKinds.Default | ReferenceKinds.Assignment | ReferenceKinds.NameOf,
            DeclarationKind.Finalizer => ReferenceKinds.None,
            DeclarationKind.Indexer => ReferenceKinds.Default | ReferenceKinds.Assignment | ReferenceKinds.InterfaceMemberImplementation
                                       | ReferenceKinds.OverrideMember,
            DeclarationKind.Operator => ReferenceKinds.Invocation,
            _ => ReferenceKinds.None
        };
}