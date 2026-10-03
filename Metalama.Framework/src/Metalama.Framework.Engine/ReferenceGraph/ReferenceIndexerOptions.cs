// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.ReferenceGraph;

public sealed class ReferenceIndexerOptions
{
    // Reference kinds that do not require descending into members.

    private const ReferenceKinds _typeDeclarationOnlyKinds = ReferenceKinds.BaseType |
                                                             ReferenceKinds.UsingNamespace;

    // Any reference on members (indirectly referencing the type) cannot be detected by identifier filtering.
    private const ReferenceKinds _kindsNotSupportingIdentifierFilteringOnTypes =
        ReferenceKinds.Default | ReferenceKinds.OverrideMember | ReferenceKinds.Assignment
        | ReferenceKinds.Invocation | ReferenceKinds.InterfaceMemberImplementation | ReferenceKinds.NameOf;

    // Reference kinds that do not require descending into implementations. NameOf is not one of them, because a nameof expression
    // is most often in the body of a member.
    private const ReferenceKinds _memberDeclarationOnlyKinds =
        ReferenceKinds.ParameterType | ReferenceKinds.ReturnType | ReferenceKinds.AttributeType | ReferenceKinds.InterfaceMemberImplementation
        | ReferenceKinds.OverrideMember | ReferenceKinds.MemberType | ReferenceKinds.UsingNamespace;

    private readonly bool _mustDescendIntoMembers;
    private readonly ReferenceKinds _kindsRequiringDescentIntoBaseTypes;
    private readonly bool _mustDescendIntoImplementation;
    private readonly ReferenceKinds _kindsRequiringDescentIntoReferencedDeclaringType;
    private readonly ReferenceKinds _kindsRequiringDescentIntoReferencedNamespace;
    private readonly ReferenceKinds _kindsRequiringDescentIntoReferencedAssembly;
    private readonly ReferenceKinds _kindsSupportingIdentifierFiltering = ReferenceKinds.All;
    private readonly ReferenceKinds _allReferenceKinds;

    /// <summary>
    /// The identifiers admitted for each reference kind, indexed by the position of the bit of the kind. A kind that has no element admits no
    /// identifier when it is filtered.
    /// </summary>
    /// <remarks>
    /// The identifiers are kept per kind so that a name that one consumer requests for a kind does not admit references of other kinds, which
    /// matters when the options of several consumers are merged.
    /// </remarks>
    private readonly ImmutableHashSet<string>?[] _filteredIdentifiersByKind;

    public ReferenceIndexerOptions( IEnumerable<ReferenceIndexerRequirements> requirements )
    {
        var filteredIdentifiers = new ImmutableHashSet<string>.Builder?[_kindBitCount];

        foreach ( var validator in requirements )
        {
            if ( validator.ReferenceKinds == ReferenceKinds.None )
            {
                continue;
            }

            var validatorReferenceKinds = validator.ReferenceKinds;

            this._allReferenceKinds |= validatorReferenceKinds;

            if ( validator is { IncludeDerivedTypes: true, ValidatedDeclarationKind: DeclarationKind.NamedType } )
            {
                this._kindsRequiringDescentIntoBaseTypes |= validatorReferenceKinds;
            }

            if ( (validatorReferenceKinds & ~(_memberDeclarationOnlyKinds | _typeDeclarationOnlyKinds)) != 0 )
            {
                this._mustDescendIntoImplementation = true;
            }

            if ( (validatorReferenceKinds & ~_typeDeclarationOnlyKinds) != 0 )
            {
                this._mustDescendIntoMembers = true;
            }

            var identifierFilteringSupported = false;

            switch ( validator.ValidatedDeclarationKind )
            {
                case DeclarationKind.Namespace:
                    this._kindsRequiringDescentIntoReferencedNamespace |= validatorReferenceKinds;
                    this._kindsSupportingIdentifierFiltering &= ~validatorReferenceKinds;

                    break;

                case { IsAssembly: true }:
                    this._kindsRequiringDescentIntoReferencedAssembly |= validatorReferenceKinds;
                    this._kindsSupportingIdentifierFiltering &= ~validatorReferenceKinds;

                    break;

                // An extension block is treated as a named type. It cannot be named in source, so it enters the
                // index only as the declaring type of one of its members, which requires the descent below.
                case DeclarationKind.NamedType:
                case DeclarationKind.ExtensionBlock:
                    this._kindsRequiringDescentIntoReferencedDeclaringType |= validatorReferenceKinds;

                    if ( validator.IncludeDerivedTypes )
                    {
                        this._kindsRequiringDescentIntoReferencedAssembly |= validatorReferenceKinds;
                        this._kindsSupportingIdentifierFiltering &= ~validatorReferenceKinds;
                    }
                    else
                    {
                        var kindsNotSupportingIdentifierFiltering = validatorReferenceKinds & _kindsNotSupportingIdentifierFilteringOnTypes;
                        var kindsSupportingIdentifierFiltering = validatorReferenceKinds & ~_kindsNotSupportingIdentifierFilteringOnTypes;
                        this._kindsSupportingIdentifierFiltering &= ~kindsNotSupportingIdentifierFiltering;
                        identifierFilteringSupported = kindsSupportingIdentifierFiltering != 0;
                    }

                    break;

                case DeclarationKind.Constructor:
                case DeclarationKind.Event:
                case DeclarationKind.Method:
                case DeclarationKind.Field:
                case DeclarationKind.Property:
                    identifierFilteringSupported = true;

                    break;
            }

            if ( identifierFilteringSupported )
            {
                var identifier = validator.ValidatedIdentifier;

                if ( identifier != null )
                {
                    foreach ( var bit in GetBits( validatorReferenceKinds ) )
                    {
                        (filteredIdentifiers[bit] ??= ImmutableHashSet.CreateBuilder<string>()).Add( identifier );
                    }
                }
            }
        }

        this._filteredIdentifiersByKind = new ImmutableHashSet<string>?[_kindBitCount];

        for ( var bit = 0; bit < _kindBitCount; bit++ )
        {
            this._filteredIdentifiersByKind[bit] = filteredIdentifiers[bit]?.ToImmutable();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceIndexerOptions"/> class that merges the options of several consumers.
    /// </summary>
    /// <remarks>
    /// The identifiers of each kind are the union of the identifiers that the consumers admit for that kind. A kind is filtered only when it is
    /// filtered by every consumer.
    /// </remarks>
    internal ReferenceIndexerOptions( IEnumerable<ReferenceIndexerOptions>? childIndexerOptions )
    {
        this._filteredIdentifiersByKind = new ImmutableHashSet<string>?[_kindBitCount];

        if ( childIndexerOptions != null )
        {
            foreach ( var child in childIndexerOptions )
            {
                this._allReferenceKinds |= child._allReferenceKinds;

                this._mustDescendIntoImplementation |= child._mustDescendIntoImplementation;
                this._mustDescendIntoMembers |= child._mustDescendIntoMembers;
                this._kindsRequiringDescentIntoReferencedAssembly |= child._kindsRequiringDescentIntoReferencedAssembly;
                this._kindsRequiringDescentIntoReferencedNamespace |= child._kindsRequiringDescentIntoReferencedNamespace;
                this._kindsRequiringDescentIntoReferencedDeclaringType |= child._kindsRequiringDescentIntoReferencedDeclaringType;
                this._kindsRequiringDescentIntoBaseTypes |= child._kindsRequiringDescentIntoBaseTypes;
                this._kindsSupportingIdentifierFiltering &= child._kindsSupportingIdentifierFiltering;

                for ( var bit = 0; bit < _kindBitCount; bit++ )
                {
                    var childIdentifiers = child._filteredIdentifiersByKind[bit];

                    if ( childIdentifiers != null )
                    {
                        this._filteredIdentifiersByKind[bit] = this._filteredIdentifiersByKind[bit]?.Union( childIdentifiers ) ?? childIdentifiers;
                    }
                }
            }
        }
    }

    /// <summary>
    /// The number of bits of the underlying type of <see cref="ReferenceKinds"/>, which is also the length of the arrays indexed by the bit of a kind.
    /// </summary>
    private const int _kindBitCount = 64;

    /// <summary>
    /// Returns the zero-based positions of the bits that are set in a combination of <see cref="ReferenceKinds"/>.
    /// </summary>
    private static IEnumerable<int> GetBits( ReferenceKinds kinds )
    {
        var value = (ulong) kinds;

        for ( var bit = 0; value != 0; bit++, value >>= 1 )
        {
            if ( (value & 1) != 0 )
            {
                yield return bit;
            }
        }
    }

    private ReferenceIndexerOptions(
        bool mustDescendIntoMembers,
        ReferenceKinds kindsRequiringDescentIntoBaseTypes,
        bool mustDescendIntoImplementation,
        ReferenceKinds kindsRequiringDescentIntoReferencedDeclaringType,
        ReferenceKinds kindsRequiringDescentIntoReferencedNamespace,
        ReferenceKinds kindsRequiringDescentIntoReferencedAssembly,
        ReferenceKinds kindsSupportingIdentifierFiltering,
        ReferenceKinds allReferenceKinds,
        ImmutableHashSet<string>?[] filteredIdentifiersByKind )
    {
        this._mustDescendIntoMembers = mustDescendIntoMembers;
        this._kindsRequiringDescentIntoBaseTypes = kindsRequiringDescentIntoBaseTypes;
        this._mustDescendIntoImplementation = mustDescendIntoImplementation;
        this._kindsRequiringDescentIntoReferencedDeclaringType = kindsRequiringDescentIntoReferencedDeclaringType;
        this._kindsRequiringDescentIntoReferencedNamespace = kindsRequiringDescentIntoReferencedNamespace;
        this._kindsRequiringDescentIntoReferencedAssembly = kindsRequiringDescentIntoReferencedAssembly;
        this._kindsSupportingIdentifierFiltering = kindsSupportingIdentifierFiltering;
        this._allReferenceKinds = allReferenceKinds;
        this._filteredIdentifiersByKind = filteredIdentifiersByKind;
    }

    internal static ReferenceIndexerOptions All
        => new(
            true,
            ReferenceKinds.All,
            true,
            ReferenceKinds.All,
            ReferenceKinds.All,
            ReferenceKinds.All,
            ReferenceKinds.None,
            ReferenceKinds.All,
            new ImmutableHashSet<string>?[_kindBitCount] );

    internal static ReferenceIndexerOptions Empty { get; } = new( ImmutableArray<ReferenceIndexerRequirements>.Empty );

    internal bool MustIndexReferenceKind( ReferenceKinds kind ) => (this._allReferenceKinds & kind) != 0;

    /// <summary>
    /// Determines whether a reference of a given kind, whose identifier is given, must be indexed.
    /// </summary>
    /// <param name="kind">The kind of the reference. The walker passes a single kind.</param>
    /// <param name="identifier">The identifier of the referenced declaration at the reference, or <c>default</c> when the reference has none.</param>
    internal bool MustIndexReference( ReferenceKinds kind, in SyntaxToken identifier )
    {
        if ( (this._allReferenceKinds & kind) == 0 )
        {
            return false;
        }

        var filteredKinds = this._kindsSupportingIdentifierFiltering & kind;

        if ( !identifier.IsKind( SyntaxKind.None ) && filteredKinds != 0 )
        {
            var identifierText = identifier.ValueText;

            if ( identifierText != "var" )
            {
                // The loop is written without an iterator because this method is called for every identifier of the walked code.
                var bits = (ulong) filteredKinds;

                for ( var bit = 0; bits != 0; bit++, bits >>= 1 )
                {
                    if ( (bits & 1) != 0 && this._filteredIdentifiersByKind[bit]?.Contains( identifierText ) == true )
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        return true;
    }

    internal bool MustDescendIntoMembers() => this._mustDescendIntoMembers;

    internal bool MustDescendIntoImplementation() => this._mustDescendIntoImplementation;

    internal bool MustDescendIntoReferencedBaseTypes( ReferenceKinds referenceKinds ) => (referenceKinds & this._kindsRequiringDescentIntoBaseTypes) != 0;

    internal bool MustDescendIntoReferencedDeclaringType( ReferenceKinds referenceKinds )
        => (referenceKinds & this._kindsRequiringDescentIntoReferencedDeclaringType) != 0;

    internal bool MustDescendIntoReferencedNamespace( ReferenceKinds referenceKinds )
        => (referenceKinds & this._kindsRequiringDescentIntoReferencedNamespace) != 0;

    internal bool MustDescendIntoReferencedAssembly( ReferenceKinds referenceKinds )
        => (referenceKinds & this._kindsRequiringDescentIntoReferencedAssembly) != 0;
}