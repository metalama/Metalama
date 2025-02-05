// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Utilities.Comparers;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using RoslynTypeKind = Microsoft.CodeAnalysis.TypeKind;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Comparers;

internal sealed partial class DeclarationEqualityComparer : IDeclarationComparer
{
    private readonly Compilation _compilation;
    private readonly Conversions _conversions;
    private readonly StructuralDeclarationComparer _structuralDeclarationComparer;

    public DeclarationEqualityComparer( Compilation compilation, bool includeNullability )
    {
        this._compilation = compilation;
        this._conversions = new Conversions( this );
        this._structuralDeclarationComparer = includeNullability ? StructuralDeclarationComparer.IncludeNullability : StructuralDeclarationComparer.Default;
    }

    public bool Equals( IDeclaration? x, IDeclaration? y )
    {
        if ( x == null && y == null )
        {
            return true;
        }

        if ( x == null || y == null )
        {
            return false;
        }

        var xAttribute = x as IAttribute;
        var yAttribute = y as IAttribute;

        if ( xAttribute != null || yAttribute != null )
        {
            // For now, use reference equality for attributes. If proper structural equality is needed, that can be added later.
            return ReferenceEquals( xAttribute, yAttribute );
        }

        return x.ToRef().Equals( y.ToRef(), RefComparison.Structural );
    }

    public int GetHashCode( IDeclaration obj )
    {
        if ( obj is IAttribute attribute )
        {
            return RuntimeHelpers.GetHashCode( attribute );
        }

        return obj.ToRef().GetHashCode( RefComparison.Default );
    }

    public bool Equals( IType? x, IType? y ) => (x == null && y == null) || (x != null && y != null && this._structuralDeclarationComparer.Equals( x, y ));

    public bool Equals( INamedType? x, INamedType? y )
        => (x == null && y == null) || (x != null && y != null && this._structuralDeclarationComparer.Equals( x, y ));

    public int GetHashCode( IType obj ) => this._structuralDeclarationComparer.GetHashCode( obj );

    public int GetHashCode( INamedType obj ) => this._structuralDeclarationComparer.GetHashCode( obj );

    bool ITypeComparer.Is( IType left, IType right, ConversionKind kind ) => this.IsConvertibleTo( left, right, kind );

    bool ITypeComparer.IsConvertibleTo( IType left, IType right, ConversionKind kind ) => this.IsConvertibleTo( left, right, kind );

    private bool IsConvertibleTo( IType left, IType right, ConversionKind kind ) => this.IsConvertibleTo( left, right, kind, bypassSymbols: false );

    bool ITypeComparer.IsConvertibleTo( IType left, Type right, ConversionKind kind ) => this.IsConvertibleTo( left, right, kind );

    /// <param name="bypassSymbols">
    /// Does not use the symbol-based implementation, even when available. Used for testing.
    /// Note that this mode is not fully implemented for conversions that only apply to built-in type (like built-in implicit numeric conversions),
    /// because the types in question are guaranteed to be symbol-based and without introductions.
    /// </param>
    internal bool IsConvertibleTo( IType left, IType right, ConversionKind kind, bool bypassSymbols )
    {
        if ( kind != ConversionKind.TypeDefinition )
        {
            if ( ReferenceEquals( left, right ) )
            {
                return true;
            }
        }

        if ( right.TryForCompilation( left.Compilation, out var translatedRight ) )
        {
            right = translatedRight;
        }
        else if ( left.TryForCompilation( right.Compilation, out var translatedLeft ) )
        {
            left = translatedLeft;
        }

        if ( left.GetSymbol() is { } leftSymbol && right.GetSymbol() is { } rightSymbol && !bypassSymbols )
        {
            // If there is conversion between the original symbols, there should be conversion between the modified types.
            // If there is no conversion between symbols, we have to check the modified types because of introductions.
            if ( this.IsConvertibleTo( leftSymbol, rightSymbol, kind ) )
            {
                return true;
            }
        }

        if ( kind == ConversionKind.TypeDefinition )
        {
            // Cannot use code based on Roslyn for this kind of conversion.

            if ( right is not INamedType { IsCanonicalGenericInstance: true } rightNamedType )
            {
                throw new ArgumentException(
                    $"{nameof(ConversionKind)}.{nameof(ConversionKind.TypeDefinition)} can only be used with canonical generic instance on the right side." );
            }

            return this.IsOfTypeDefinition( left, rightNamedType );
        }

        return this._conversions.HasConversion( left, right, kind );
    }

    bool ITypeComparer.Is( IType left, Type right, ConversionKind kind ) => this.IsConvertibleTo( left, right, kind );

    public bool IsConvertibleTo( IType left, Type right, ConversionKind kind ) => this.IsConvertibleTo( left, right, kind, bypassSymbols: false );

    internal bool IsConvertibleTo( IType left, Type right, ConversionKind kind, bool bypassSymbols )
        => this.IsConvertibleTo(
            left,
            left.GetCompilationModel().Factory.GetTypeByReflectionType( right ),
            kind,
            bypassSymbols );

    internal bool IsConvertibleTo( ITypeSymbol left, ITypeSymbol right, ConversionKind kind )
    {
        // TODO: Does not take introduced interfaces into account (requires a lot of changes).

        if ( ReferenceEquals( left, right ) )
        {
            return true;
        }

        left.ThrowIfBelongsToDifferentCompilationThan( right );

        if ( kind == ConversionKind.TypeDefinition )
        {
            // Cannot use Roslyn for this kind of conversion.

            if ( right is not INamedTypeSymbol rightNamedType || !SymbolEqualityComparer.Default.Equals( rightNamedType, rightNamedType.ConstructedFrom ) )
            {
                throw new ArgumentException(
                    $"{nameof(ConversionKind)}.{nameof(ConversionKind.TypeDefinition)} can only be used with unbound generic type on the right side." );
            }

            return IsOfTypeDefinition( left, rightNamedType );
        }

        var conversion = this._compilation.ClassifyConversion( left, right );

        switch ( kind )
        {
            case ConversionKind.Implicit:
                return conversion is { IsIdentity: true } or { IsImplicit: true };

            case ConversionKind.Reference:
                return conversion is { IsIdentity: true } or { IsImplicit: true, IsReference: true };

            case ConversionKind.Default:
                return conversion is { IsIdentity: true } or ({ IsImplicit: true } and ({ IsReference: true } or { IsBoxing: true } or { IsNullable: true }));

            default:
                throw new ArgumentOutOfRangeException( nameof(kind) );
        }
    }

    private static bool IsOfTypeDefinition( ITypeSymbol type, INamedTypeSymbol typeDefinition )
    {
        // TODO: This can be optimized (e.g. when searching for interface definition, classes don't have to be checked for symbol equality).

        // Evaluate the current type.

        if ( SymbolEqualityComparer.Default.Equals( (type as INamedTypeSymbol)?.ConstructedFrom, typeDefinition ) )
        {
            return true;
        }

        if ( typeDefinition.TypeKind == RoslynTypeKind.Interface )
        {
            // When searching for an interface, we should consider interfaces defined by the evaluated type.
            if ( type.Interfaces.Any( i => IsOfTypeDefinition( i, typeDefinition ) ) )
            {
                // The type implements interface that has the same definition.
                return true;
            }
        }

        return type.BaseType != null && IsOfTypeDefinition( type.BaseType, typeDefinition );
    }

    private bool IsOfTypeDefinition( IType type, INamedType typeDefinition )
    {
        // TODO: This can be optimized (e.g. when searching for interface definition, classes don't have to be checked for symbol equality).

        // Evaluate the current type.

        if ( this.Equals( (type as INamedType)?.Definition, typeDefinition ) )
        {
            return true;
        }

        if ( typeDefinition.TypeKind == TypeKind.Interface )
        {
            // When searching for an interface, we should consider interfaces defined by the evaluated type.
            if ( type.GetImplementedInterfaces().Any( i => this.IsOfTypeDefinition( i, typeDefinition ) ) )
            {
                // The type implements interface that has the same definition.
                return true;
            }
        }

        return type.GetBaseType() != null && this.IsOfTypeDefinition( type.GetBaseType()!, typeDefinition );
    }
}