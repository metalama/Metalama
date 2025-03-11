// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Utilities.Comparers;

/// <summary>
/// Compares declarations, possibly from different compilations.
/// </summary>
internal sealed class StructuralDeclarationComparer : IEqualityComparer<ICompilationElement>, IComparer<ICompilationElement>
{
    // ReSharper disable UnusedMember.Global

    public static readonly StructuralDeclarationComparer Default = new( StructuralSymbolComparer.Default );

    public static readonly StructuralDeclarationComparer IncludeAssembly = new( StructuralSymbolComparer.IncludeAssembly );

    public static readonly StructuralDeclarationComparer IncludeNullability = new( StructuralSymbolComparer.IncludeNullability );

    public static readonly StructuralDeclarationComparer IncludeAssemblyAndNullability = new( StructuralSymbolComparer.IncludeAssemblyAndNullability );

    public static readonly StructuralDeclarationComparer ContainingDeclarationOblivious = new( StructuralSymbolComparer.ContainingDeclarationOblivious );

    public static readonly StructuralDeclarationComparer Signature = new( StructuralSymbolComparer.Signature );

    internal static readonly StructuralDeclarationComparer NameOblivious = new( StructuralSymbolComparer.NameOblivious );

    // ReSharper enable UnusedMember.Global

    // used for testing
    internal static readonly StructuralDeclarationComparer BypassSymbols = new( StructuralSymbolComparer.Default, bypassSymbols: true );

    private static readonly StructuralDeclarationComparer _nonRecursive = new( StructuralSymbolComparer.NonRecursive );

    private readonly StructuralComparerOptions _options;
    private readonly StructuralSymbolComparer? _symbolComparer;

    private StructuralDeclarationComparer( StructuralSymbolComparer symbolComparer, bool bypassSymbols = false )
    {
        var options = symbolComparer.Options;

        // Assumed by the implementation of GetHashCode.
        Invariant.Implies(
            options.HasFlagFast( StructuralComparerOptions.ContainingDeclaration ),
            options.HasFlagFast(
                StructuralComparerOptions.Name | StructuralComparerOptions.GenericParameterCount | StructuralComparerOptions.ParameterTypes ) );

        this._options = options;

        if ( !bypassSymbols )
        {
            this._symbolComparer = symbolComparer;
        }
    }

    public bool Equals( ICompilationElement? x, ICompilationElement? y ) => this.Compare( x, y ) == 0;

    public int Compare( ICompilationElement? x, ICompilationElement? y )
    {
        if ( ReferenceEquals( x, y ) )
        {
            return 0;
        }
        else if ( x == null )
        {
            return -1;
        }
        else if ( y == null )
        {
            return 1;
        }

        int result;

        // PERF: Cast enum to int otherwise it will be boxed on .NET Framework.
        if ( x is IType xType && y is IType yType )
        {
            if ( xType.GetSymbol( false ) is { } xSymbol && yType.GetSymbol( false ) is { } ySymbol && this._symbolComparer != null )
            {
                return this._symbolComparer.Compare( xSymbol, ySymbol );
            }

            result = ((int) xType.TypeKind).CompareTo( (int) yType.TypeKind );
        }
        else if ( x is IDeclaration xDeclaration && y is IDeclaration yDeclaration )
        {
            if ( xDeclaration.GetSymbol( false ) is { } xSymbol && yDeclaration.GetSymbol( false ) is { } ySymbol && this._symbolComparer != null )
            {
                return this._symbolComparer.Compare( xSymbol, ySymbol );
            }

            result = ((int) xDeclaration.DeclarationKind).CompareTo( (int) yDeclaration.DeclarationKind );
        }
        else if ( x is IType && y is IDeclaration )
        {
            return -1;
        }
        else if ( x is IDeclaration && y is IType )
        {
            return 1;
        }
        else
        {
            throw new NotImplementedException( $"Unsupported declarations: {x.GetType()} and {y.GetType()}." );
        }

        if ( result != 0 )
        {
            // Unequal kinds.
            return result;
        }

        switch (x, y)
        {
            case (IMethod methodX, IMethod methodY):
                result = this.CompareMethods( methodX, methodY, this._options );

                if ( result != 0 )
                {
                    return result;
                }

                break;

            case (IConstructor constructorX, IConstructor constructorY):
                result = this.CompareConstructors( constructorX, constructorY, this._options );

                if ( result != 0 )
                {
                    return result;
                }

                break;

            case (IParameter parameterX, IParameter parameterY):
                result = this.Compare( parameterX.ContainingDeclaration, parameterY.ContainingDeclaration );

                if ( result != 0 )
                {
                    return result;
                }

                return parameterX.Index.CompareTo( parameterY.Index );

            case (IProperty propertyX, IProperty propertyY):

                result = CompareProperties( propertyX, propertyY, this._options );

                if ( result != 0 )
                {
                    return result;
                }

                break;

            case (IIndexer indexerX, IIndexer indexerY):
                result = this.CompareIndexers( indexerX, indexerY, this._options );

                if ( result != 0 )
                {
                    return result;
                }

                break;

            case (IEvent eventX, IEvent eventY):
                result = CompareEvents( eventX, eventY, this._options );

                if ( result != 0 )
                {
                    return result;
                }

                break;

            case (IField fieldX, IField fieldY):
                result = CompareFields( fieldX, fieldY, this._options );

                if ( result != 0 )
                {
                    return result;
                }

                break;

            case (IType typeX, IType typeY):
                result = this.CompareTypes( typeX, typeY );

                if ( result != 0 )
                {
                    return result;
                }

                break;

            case (INamespace namespaceX, INamespace namespaceY):
                result = this.CompareNamespaces( namespaceX, namespaceY );

                if ( result != 0 )
                {
                    return result;
                }

                break;

            case (IAssembly assemblyX, IAssembly assemblyY):
                return CompareAssemblies( assemblyX, assemblyY );

            default:
                throw new NotImplementedException( $"Unexpected declarations: {x.GetType()}, {y.GetType()}." );
        }

        if ( this._options.HasFlagFast( StructuralComparerOptions.ContainingDeclaration ) )
        {
            result = this.CompareContainingDeclarations(
                (x as IDeclaration)?.GetContainingDeclarationOrNamespace(),
                (y as IDeclaration)?.GetContainingDeclarationOrNamespace() );

            if ( result != 0 )
            {
                return result;
            }
        }

        if ( this._options.HasFlagFast( StructuralComparerOptions.ContainingAssembly ) )
        {
            result = CompareAssemblies( (x as IDeclaration)?.DeclaringAssembly, (y as IDeclaration)?.DeclaringAssembly );

            if ( result != 0 )
            {
                return result;
            }
        }

        return 0;
    }

    private static int CompareAssemblies( IAssembly? assemblyX, IAssembly? assemblyY )
    {
        if ( ReferenceEquals( assemblyX, assemblyY ) )
        {
            return 0;
        }

        if ( assemblyX == null )
        {
            return -1;
        }

        if ( assemblyY == null )
        {
            return 1;
        }

        var identityX = assemblyX.Identity;
        var identityY = assemblyY.Identity;

        var result = AssemblyIdentityComparer.SimpleNameComparer.Compare( identityX.Name, identityY.Name );

        if ( result != 0 )
        {
            return result;
        }

        return identityX.Version.CompareTo( identityY.Version );

        // Ignore culture and public key, since they shouldn't be relevant.
    }

    private int CompareNamespaces( INamespace nsX, INamespace nsY )
    {
        if ( ReferenceEquals( nsX, nsY ) )
        {
            return 0;
        }

        int result;

        if ( this._options.HasFlagFast( StructuralComparerOptions.ContainingAssembly ) )
        {
            result = CompareAssemblies( nsX.DeclaringAssembly, nsY.DeclaringAssembly );

            if ( result != 0 )
            {
                return result;
            }
        }

        result = Comparer<bool>.Default.Compare( nsX.IsPartial, nsY.IsPartial );

        if ( result != 0 )
        {
            return result;
        }

        result = StringComparer.Ordinal.Compare( nsX.Name, nsY.Name );

        if ( result != 0 )
        {
            return result;
        }

        result = nsX.IsGlobalNamespace.CompareTo( nsY.IsGlobalNamespace );

        if ( result != 0 )
        {
            return result;
        }

        if ( nsX.IsGlobalNamespace )
        {
            return 0;
        }

        return this.CompareNamespaces( nsX.ContainingNamespace!, nsY.ContainingNamespace! );
    }

    private int CompareNamedTypes( INamedType namedTypeX, INamedType namedTypeY, StructuralComparerOptions options )
    {
        if ( ReferenceEquals( namedTypeX, namedTypeY ) )
        {
            return 0;
        }

        int result;

        if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
        {
            result = StringComparer.Ordinal.Compare( namedTypeX.Name, namedTypeY.Name );

            if ( result != 0 )
            {
                return result;
            }

            if ( namedTypeX.ContainingDeclaration is not INamedType && namedTypeY.ContainingDeclaration is not INamedType )
            {
                result = this.CompareNamespaces( namedTypeX.ContainingNamespace, namedTypeY.ContainingNamespace );

                if ( result != 0 )
                {
                    return result;
                }
            }
        }

        if ( options.HasFlagFast( StructuralComparerOptions.Nullability ) )
        {
            result = Comparer<bool?>.Default.Compare( namedTypeX.IsNullable, namedTypeY.IsNullable );

            if ( result != 0 )
            {
                return result;
            }
        }

        if ( options.HasFlagFast( StructuralComparerOptions.GenericParameterCount ) )
        {
            result = namedTypeX.TypeParameters.Count.CompareTo( namedTypeY.TypeParameters.Count );

            if ( result != 0 )
            {
                return result;
            }
        }

        if ( options.HasFlagFast( StructuralComparerOptions.GenericArguments ) )
        {
            Invariant.Assert( options.HasFlagFast( StructuralComparerOptions.GenericParameterCount ) );

            for ( var i = 0; i < namedTypeX.TypeArguments.Count; i++ )
            {
                var typeArgumentX = namedTypeX.TypeArguments[i];
                var typeArgumentY = namedTypeY.TypeArguments[i];

                result = this.CompareTypes( typeArgumentX, typeArgumentY );

                if ( result != 0 )
                {
                    return result;
                }
            }
        }

        return this.CompareTypes( namedTypeX.DeclaringType, namedTypeY.DeclaringType );
    }

    private int CompareMethods( IMethod methodX, IMethod methodY, StructuralComparerOptions options )
    {
        if ( ReferenceEquals( methodX, methodY ) )
        {
            return 0;
        }

        int result;

        if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
        {
            result = StringComparer.Ordinal.Compare( methodX.Name, methodY.Name );

            if ( result != 0 )
            {
                return result;
            }
        }

        if ( options.HasFlagFast( StructuralComparerOptions.GenericParameterCount ) )
        {
            result = methodX.TypeParameters.Count.CompareTo( methodY.TypeParameters.Count );

            if ( result != 0 )
            {
                return result;
            }
        }

        if ( options.HasFlagFast( StructuralComparerOptions.GenericArguments ) )
        {
            Invariant.Assert( options.HasFlagFast( StructuralComparerOptions.GenericParameterCount ) );

            for ( var i = 0; i < methodX.TypeArguments.Count; i++ )
            {
                var typeArgumentX = methodX.TypeArguments[i];
                var typeArgumentY = methodY.TypeArguments[i];

                result = this.CompareTypes( typeArgumentX, typeArgumentY );

                if ( result != 0 )
                {
                    return result;
                }
            }
        }

        return this.CompareParameters( methodX.Parameters, methodY.Parameters, methodX.ReturnType, methodY.ReturnType, options );
    }

    private int CompareConstructors( IConstructor constructorX, IConstructor constructorY, StructuralComparerOptions options )
    {
        if ( ReferenceEquals( constructorX, constructorY ) )
        {
            return 0;
        }

        return this.CompareParameters( constructorX.Parameters, constructorY.Parameters, null, null, options );
    }

    private static int CompareProperties( IProperty propertyX, IProperty propertyY, StructuralComparerOptions options )
    {
        if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
        {
            var result = StringComparer.Ordinal.Compare( propertyX.Name, propertyY.Name );

            if ( result != 0 )
            {
                return result;
            }
        }

        return 0;
    }

    private int CompareIndexers( IIndexer indexerX, IIndexer indexerY, StructuralComparerOptions options )
    {
        if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
        {
            var result = StringComparer.Ordinal.Compare( indexerX.Name, indexerY.Name );

            if ( result != 0 )
            {
                return result;
            }
        }

        return this.CompareParameters( indexerX.Parameters, indexerY.Parameters, null, null, options );
    }

    private int CompareParameters(
        IParameterList methodXParameters,
        IParameterList methodYParameters,
        IType? methodXReturnType,
        IType? methodYReturnType,
        StructuralComparerOptions options )
    {
        int CompareParameterTypes( IType? parameterTypeX, IType? parameterTypeY )
        {
            // Prevent infinite recursion.
            var comparer = parameterTypeX is ITypeParameter { ContainingDeclaration: IMethod }
                           && parameterTypeY is ITypeParameter { ContainingDeclaration: IMethod }
                ? _nonRecursive
                : this;

            return comparer.Compare( parameterTypeX, parameterTypeY );
        }

        if ( options.HasFlagFast( StructuralComparerOptions.ParameterTypes )
             || options.HasFlagFast( StructuralComparerOptions.ParameterModifiers ) )
        {
            var result = methodXParameters.Count.CompareTo( methodYParameters.Count );

            if ( result != 0 )
            {
                return result;
            }

            for ( var i = 0; i < methodXParameters.Count; i++ )
            {
                var parameterX = methodXParameters[i];
                var parameterY = methodYParameters[i];

                if ( options.HasFlagFast( StructuralComparerOptions.ParameterTypes ) )
                {
                    result = CompareParameterTypes( parameterX.Type, parameterY.Type );

                    if ( result != 0 )
                    {
                        return result;
                    }
                }

                if ( options.HasFlagFast( StructuralComparerOptions.ParameterModifiers ) )
                {
                    // PERF: Cast enum to byte otherwise it will be boxed on .NET Framework.
                    result = Comparer<byte>.Default.Compare( (byte) parameterX.RefKind, (byte) parameterY.RefKind );

                    if ( result != 0 )
                    {
                        return result;
                    }
                }
            }

            // Conversion operators have the same parameters but a different return type.
            result = CompareParameterTypes( methodXReturnType, methodYReturnType );

            if ( result != 0 )
            {
                return result;
            }
        }

        return 0;
    }

    private static int CompareEvents( IEvent eventX, IEvent eventY, StructuralComparerOptions options )
    {
        if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
        {
            return StringComparer.Ordinal.Compare( eventX.Name, eventY.Name );
        }

        return 0;
    }

    private static int CompareFields( IField fieldX, IField fieldY, StructuralComparerOptions options )
    {
        if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
        {
            return StringComparer.Ordinal.Compare( fieldX.Name, fieldY.Name );
        }

        return 0;
    }

    private int CompareTypes( IType? typeX, IType? typeY )
    {
        if ( ReferenceEquals( typeX, typeY ) )
        {
            return 0;
        }

        if ( typeX == null )
        {
            return -1;
        }

        if ( typeY == null )
        {
            return 1;
        }

        // PERF: Cast enum to byte otherwise it will be boxed on .NET Framework.
        var result = Comparer<byte>.Default.Compare( (byte) typeX.TypeKind, (byte) typeY.TypeKind );

        if ( result != 0 )
        {
            // Unequal kinds.
            return result;
        }

        if ( this._options.HasFlagFast( StructuralComparerOptions.Nullability ) )
        {
            result = Comparer<bool?>.Default.Compare( typeX.IsNullable, typeY.IsNullable );

            if ( result != 0 )
            {
                return result;
            }
        }

        switch (typeX, typeY)
        {
            case (ITypeParameter typeParamX, ITypeParameter typeParamY):
                result = StringComparer.Ordinal.Compare( typeParamX.Name, typeParamY.Name );

                if ( result != 0 )
                {
                    return result;
                }

                if ( this._options.HasFlagFast( StructuralComparerOptions.ContainingDeclaration ) )
                {
                    result = _nonRecursive.Compare( typeParamX.ContainingDeclaration, typeParamY.ContainingDeclaration );
                }

                return result;

            case (INamedType namedTypeX, INamedType namedTypeY):
                return this.CompareNamedTypes( namedTypeX, namedTypeY, this._options );

            case (IArrayType arrayTypeX, IArrayType arrayTypeY):
                result = arrayTypeX.Rank.CompareTo( arrayTypeY.Rank );

                if ( result != 0 )
                {
                    return result;
                }

                return this.CompareTypes( arrayTypeX.ElementType, arrayTypeY.ElementType );

            case (IDynamicType, IDynamicType):
                return 0;

            case (IPointerType xPointerType, IPointerType yPointerType):
                return this.CompareTypes( xPointerType.PointedAtType, yPointerType.PointedAtType );

            default:
                throw new NotImplementedException( $"Unexpected type kind {typeX.TypeKind}." );
        }
    }

    private int CompareContainingDeclarations( IDeclaration? x, IDeclaration? y )
    {
        var currentX = x;
        var currentY = y;

        while ( true )
        {
            if ( ReferenceEquals( currentX, currentY ) )
            {
                return 0;
            }

            if ( currentX == null )
            {
                return -1;
            }

            if ( currentY == null )
            {
                return 1;
            }

            // PERF: Cast enum to int otherwise it will be boxed on .NET Framework.
            var result = Comparer<int>.Default.Compare( (int) currentX.DeclarationKind, (int) currentY.DeclarationKind );

            if ( result != 0 )
            {
                return result;
            }

            switch (currentX, currentY)
            {
                case (IMethod methodX, IMethod methodY):
                    result = this.CompareMethods( methodX, methodY, StructuralComparerOptions.MethodSignature );

                    if ( result != 0 )
                    {
                        return result;
                    }

                    break;

                case (INamedType namedTypeX, INamedType namedTypeY):
                    result = this.CompareNamedTypes( namedTypeX, namedTypeY, StructuralComparerOptions.Type );

                    if ( result != 0 )
                    {
                        return result;
                    }

                    break;

                case (INamespace namespaceX, INamespace namespaceY):
                    result = StringComparer.Ordinal.Compare( namespaceX.Name, namespaceY.Name );

                    if ( result != 0 )
                    {
                        return result;
                    }

                    break;

                case (IAssembly, IAssembly):
                    return 0;

                default:
                    throw new NotImplementedException( $"Unexpected declaration kind {currentX.DeclarationKind}." );
            }

            currentX = currentX.GetContainingDeclarationOrNamespace();
            currentY = currentY.GetContainingDeclarationOrNamespace();
        }
    }

    public int GetHashCode( ICompilationElement compilationElement ) => GetHashCode( compilationElement, this._options );

    // For performance reasons, GetHashCode should use a limited subset of properties where collisions are likely.
    private static int GetHashCode( ICompilationElement compilationElement, StructuralComparerOptions options )
    {
        var h = 701_142_619; // Random prime.

        switch ( compilationElement )
        {
            case IDeclaration declaration:
                h = HashCode.Combine( h, true );

                // PERF: Cast enum to int otherwise it will be boxed on .NET Framework.
                h = HashCode.Combine( h, (int) declaration.DeclarationKind );

                break;

            case IType type:
                h = HashCode.Combine( h, false );

                // PERF: Cast enum to int otherwise it will be boxed on .NET Framework.
                h = HashCode.Combine( h, (int) type.TypeKind );

                break;
        }

        switch ( compilationElement )
        {
            case null:
                throw new ArgumentNullException( nameof(compilationElement) );

            case IParameter parameter:
                h = HashCode.Combine( h, GetHashCode( parameter.ContainingDeclaration!, options ), parameter.Index );

                break;

            case INamedType type:
                if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
                {
                    h = HashCode.Combine( h, type.Name );
                }

                if ( options.HasFlagFast( StructuralComparerOptions.GenericParameterCount ) )
                {
                    h = HashCode.Combine( h, type.TypeParameters.Count );
                }

                break;

            case IMethodBase method:
                if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
                {
                    h = HashCode.Combine( h, method.Name );
                }

                if ( options.HasFlagFast( StructuralComparerOptions.ParameterTypes )
                     || options.HasFlagFast( StructuralComparerOptions.ParameterModifiers ) )
                {
                    h = HashCode.Combine( h, method.Parameters.Count );

                    foreach ( var parameter in method.Parameters )
                    {
                        if ( options.HasFlagFast( StructuralComparerOptions.ParameterTypes ) )
                        {
                            h = HashCode.Combine( h, GetHashCode( parameter.Type, StructuralComparerOptions.Type ) );
                        }

                        if ( options.HasFlagFast( StructuralComparerOptions.ParameterModifiers ) )
                        {
                            // PERF: Cast enum to byte otherwise it will be boxed on .NET Framework.
                            h = HashCode.Combine( h, (byte) parameter.RefKind );
                        }
                    }
                }

                break;

            case IProperty property:
                if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
                {
                    h = HashCode.Combine( h, property.Name );
                }

                break;

            case IIndexer indexer:
                if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
                {
                    h = HashCode.Combine( h, indexer.Name );
                }

                if ( options.HasFlagFast( StructuralComparerOptions.ParameterTypes )
                     || options.HasFlagFast( StructuralComparerOptions.ParameterModifiers ) )
                {
                    h = HashCode.Combine( h, indexer.Parameters.Count );

                    foreach ( var parameter in indexer.Parameters )
                    {
                        if ( options.HasFlagFast( StructuralComparerOptions.ParameterTypes ) )
                        {
                            h = HashCode.Combine( h, GetHashCode( parameter.Type, StructuralComparerOptions.Type ) );
                        }

                        if ( options.HasFlagFast( StructuralComparerOptions.ParameterModifiers ) )
                        {
                            h = HashCode.Combine( h, (byte) parameter.RefKind );
                        }
                    }
                }

                break;

            case IField field:
                if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
                {
                    h = HashCode.Combine( h, field.Name );
                }

                break;

            case IEvent @event:
                if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
                {
                    h = HashCode.Combine( h, @event.Name );
                }

                break;

            case INamespace @namespace:
                if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
                {
                    h = HashCode.Combine( h, @namespace.Name );
                }

                break;

            case ITypeParameter typeParameter:
                h = HashCode.Combine( h, typeParameter.Index );

                break;

            case IArrayType arrayType:
                h = HashCode.Combine( h, arrayType.Rank, GetHashCode( arrayType.ElementType, StructuralComparerOptions.Type ) );

                break;

            case IDynamicType:
                h = 41574;

                break;

            case IAssembly assembly:
                return HashCode.Combine( h, AssemblyIdentityComparer.SimpleNameComparer.GetHashCode( assembly.Identity.Name ), assembly.Identity.Version );

            case IPointerType pointerType:
                return GetHashCode( pointerType.PointedAtType, StructuralComparerOptions.Type );

            case IFunctionPointerType:
                h = 173808215;

                break;

            default:
                throw new NotImplementedException( $"Unexpected type {compilationElement.GetType()}." );
        }

        if ( options.HasFlagFast( StructuralComparerOptions.ContainingDeclaration ) )
        {
            var current = (compilationElement as IDeclaration)?.GetContainingDeclarationOrNamespace();

            while ( current != null )
            {
                switch ( current )
                {
                    case INamedType namedType:
                        h = HashCode.Combine( h, namedType.Name, namedType.TypeParameters.Count );

                        break;

                    case INamespace @namespace:
                        h = HashCode.Combine( h, @namespace.Name );

                        break;

                    case IMethod method:
                        h = HashCode.Combine( h, method.Name, method.TypeParameters.Count, method.Parameters.Count );

                        // This runs only if the original declaration was a local function.
                        foreach ( var parameter in method.Parameters )
                        {
                            h = HashCode.Combine( h, GetHashCode( parameter.Type, StructuralComparerOptions.Type ) );
                        }

                        break;

                    case IProperty property:
                        h = HashCode.Combine( h, property.Name );

                        break;

                    case IIndexer indexer:
                        h = HashCode.Combine( h, indexer.Name, indexer.Parameters.Count );

                        // This runs only if the original declaration was a local function.
                        foreach ( var parameter in indexer.Parameters )
                        {
                            h = HashCode.Combine( h, GetHashCode( parameter.Type, StructuralComparerOptions.Type ) );
                        }

                        break;

                    case IAssembly:
                        // This is included below if required.
                        break;

                    default:
                        throw new NotImplementedException( $"Unexpected declaration kind {current.DeclarationKind}." );
                }

                current = current.GetContainingDeclarationOrNamespace();
            }
        }

        if ( options.HasFlagFast( StructuralComparerOptions.ContainingAssembly ) )
        {
            // Version should not differ often.
            h = HashCode.Combine( h, (compilationElement as IDeclaration)?.DeclaringAssembly.Identity.Name );
        }

        return h;
    }
}