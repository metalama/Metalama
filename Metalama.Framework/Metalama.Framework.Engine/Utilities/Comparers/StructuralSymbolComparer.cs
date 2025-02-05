// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Utilities.Comparers;

/// <summary>
/// Compares symbols, possibly from different compilations.
/// </summary>
internal sealed class StructuralSymbolComparer : IEqualityComparer<ISymbol?>, IComparer<ISymbol?>
{
    // ReSharper disable UnusedMember.Global

    public static readonly StructuralSymbolComparer Default =
        new(
            StructuralComparerOptions.ContainingDeclaration |
            StructuralComparerOptions.Name |
            StructuralComparerOptions.GenericParameterCount |
            StructuralComparerOptions.GenericArguments |
            StructuralComparerOptions.ParameterTypes |
            StructuralComparerOptions.ParameterModifiers );

    public static readonly StructuralSymbolComparer IncludeAssembly =
        new(
            StructuralComparerOptions.ContainingDeclaration |
            StructuralComparerOptions.Name |
            StructuralComparerOptions.GenericParameterCount |
            StructuralComparerOptions.GenericArguments |
            StructuralComparerOptions.ParameterTypes |
            StructuralComparerOptions.ParameterModifiers |
            StructuralComparerOptions.ContainingAssembly );

    public static readonly StructuralSymbolComparer IncludeNullability =
        new(
            StructuralComparerOptions.ContainingDeclaration |
            StructuralComparerOptions.Name |
            StructuralComparerOptions.GenericParameterCount |
            StructuralComparerOptions.GenericArguments |
            StructuralComparerOptions.ParameterTypes |
            StructuralComparerOptions.ParameterModifiers |
            StructuralComparerOptions.Nullability );

    public static readonly StructuralSymbolComparer IncludeAssemblyAndNullability =
        new(
            StructuralComparerOptions.ContainingDeclaration |
            StructuralComparerOptions.Name |
            StructuralComparerOptions.GenericParameterCount |
            StructuralComparerOptions.GenericArguments |
            StructuralComparerOptions.ParameterTypes |
            StructuralComparerOptions.ParameterModifiers |
            StructuralComparerOptions.Nullability |
            StructuralComparerOptions.ContainingAssembly );

    public static readonly StructuralSymbolComparer ContainingDeclarationOblivious =
        new(
            StructuralComparerOptions.Name |
            StructuralComparerOptions.GenericParameterCount |
            StructuralComparerOptions.GenericArguments |
            StructuralComparerOptions.ParameterTypes |
            StructuralComparerOptions.ParameterModifiers );

    public static readonly StructuralSymbolComparer Signature =
        new(
            StructuralComparerOptions.Name |
            StructuralComparerOptions.GenericParameterCount |
            StructuralComparerOptions.ParameterTypes |
            StructuralComparerOptions.ParameterModifiers );

    internal static readonly StructuralSymbolComparer NameOblivious = new(
        StructuralComparerOptions.GenericArguments |
        StructuralComparerOptions.GenericParameterCount |
        StructuralComparerOptions.ParameterModifiers |
        StructuralComparerOptions.ParameterTypes );

    // ReSharper enable UnusedMember.Global

    internal static readonly StructuralSymbolComparer NonRecursive = new(
        StructuralComparerOptions.Name |
        StructuralComparerOptions.GenericParameterCount |
        StructuralComparerOptions.ParameterModifiers );

    internal StructuralComparerOptions Options { get; }

    private StructuralSymbolComparer( StructuralComparerOptions options )
    {
        // Assumed by the implementation of GetHashCode.
        Invariant.Implies(
            options.HasFlagFast( StructuralComparerOptions.ContainingDeclaration ),
            options.HasFlagFast(
                StructuralComparerOptions.Name | StructuralComparerOptions.GenericParameterCount | StructuralComparerOptions.ParameterTypes ) );

        this.Options = options;
    }

    public bool Equals( ISymbol? x, ISymbol? y ) => this.Compare( x, y ) == 0;

    public int Compare( ISymbol? x, ISymbol? y )
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

        // PERF: Cast enum to int otherwise it will be boxed on .NET Framework.
        var result = Comparer<int>.Default.Compare( (int) x.Kind, (int) y.Kind );

        if ( result != 0 )
        {
            // Unequal kinds.
            return result;
        }

        switch (x, y)
        {
            case (IMethodSymbol methodX, IMethodSymbol methodY):
                result = this.CompareMethods( methodX, methodY, this.Options );

                if ( result != 0 )
                {
                    return result;
                }

                break;

            case (IParameterSymbol parameterX, IParameterSymbol parameterY):
                result = this.Compare( parameterX.ContainingSymbol, parameterY.ContainingSymbol );

                if ( result != 0 )
                {
                    return result;
                }

                return parameterX.Ordinal.CompareTo( parameterY.Ordinal );

            case (IPropertySymbol propertyX, IPropertySymbol propertyY):
                result = this.CompareProperties( propertyX, propertyY, this.Options );

                if ( result != 0 )
                {
                    return result;
                }

                break;

            case (IEventSymbol eventX, IEventSymbol eventY):
                result = CompareEvents( eventX, eventY, this.Options );

                if ( result != 0 )
                {
                    return result;
                }

                break;

            case (IFieldSymbol fieldX, IFieldSymbol fieldY):
                result = CompareFields( fieldX, fieldY, this.Options );

                if ( result != 0 )
                {
                    return result;
                }

                break;

            case (ITypeSymbol typeX, ITypeSymbol typeY):
                result = this.CompareTypes( typeX, typeY );

                if ( result != 0 )
                {
                    return result;
                }

                break;

            case (INamespaceSymbol namespaceX, INamespaceSymbol namespaceY):
                result = this.CompareNamespaces( namespaceX, namespaceY );

                if ( result != 0 )
                {
                    return result;
                }

                break;

            case (IAssemblySymbol assemblyX, IAssemblySymbol assemblyY):
                return CompareAssemblies( assemblyX, assemblyY );

            case (ILocalSymbol localSymbolX, ILocalSymbol localSymbolY):
                // TODO: Is this correct in all options?
                result = StringComparer.Ordinal.Compare( localSymbolX.Name, localSymbolY.Name );

                if ( result != 0 )
                {
                    return result;
                }

                break;

            default:
                throw new NotImplementedException( $"{x.Kind}" );
        }

        if ( this.Options.HasFlagFast( StructuralComparerOptions.ContainingDeclaration ) )
        {
            result = this.CompareContainingDeclarations( x.ContainingSymbol, y.ContainingSymbol );

            if ( result != 0 )
            {
                return result;
            }
        }

        if ( this.Options.HasFlagFast( StructuralComparerOptions.ContainingAssembly ) )
        {
            result = CompareContainingModules( x.ContainingModule, y.ContainingModule );

            if ( result != 0 )
            {
                return result;
            }
        }

        return 0;
    }

    private static int CompareAssemblies( IAssemblySymbol assemblyX, IAssemblySymbol assemblyY )
    {
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

    private int CompareNamespaces( INamespaceSymbol nsX, INamespaceSymbol nsY )
    {
        int result;

        if ( this.Options.HasFlagFast( StructuralComparerOptions.ContainingAssembly ) )
        {
            result = CompareContainingModules( nsX.ContainingModule, nsY.ContainingModule );

            if ( result != 0 )
            {
                return result;
            }
        }

        // PERF: Cast enum to int otherwise it will be boxed on .NET Framework.
        result = Comparer<int>.Default.Compare( (int) nsX.NamespaceKind, (int) nsY.NamespaceKind );

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

        return this.CompareNamespaces( nsX.ContainingNamespace, nsY.ContainingNamespace );
    }

    private int CompareNamedTypes( INamedTypeSymbol namedTypeX, INamedTypeSymbol namedTypeY, StructuralComparerOptions options )
    {
        int result;

        if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
        {
            result = StringComparer.Ordinal.Compare( namedTypeX.Name, namedTypeY.Name );

            if ( result != 0 )
            {
                return result;
            }

            if ( namedTypeX.ContainingType == null && namedTypeY.ContainingType == null )
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
            // PERF: Cast enum to byte otherwise it will be boxed on .NET Framework.
            result = Comparer<byte>.Default.Compare( (byte) namedTypeX.NullableAnnotation, (byte) namedTypeY.NullableAnnotation );

            if ( result != 0 )
            {
                return result;
            }
        }

        if ( options.HasFlagFast( StructuralComparerOptions.GenericParameterCount ) )
        {
            result = namedTypeX.Arity.CompareTo( namedTypeY.Arity );

            if ( result != 0 )
            {
                return result;
            }
        }

        if ( options.HasFlagFast( StructuralComparerOptions.GenericArguments ) )
        {
            Invariant.Assert( options.HasFlagFast( StructuralComparerOptions.GenericParameterCount ) );

            for ( var i = 0; i < namedTypeX.TypeArguments.Length; i++ )
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

        return this.CompareTypes( namedTypeX.ContainingType, namedTypeY.ContainingType );
    }

    private int CompareMethods( IMethodSymbol methodX, IMethodSymbol methodY, StructuralComparerOptions options )
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
            result = methodX.Arity.CompareTo( methodY.Arity );

            if ( result != 0 )
            {
                return result;
            }
        }

        if ( options.HasFlagFast( StructuralComparerOptions.GenericArguments ) )
        {
            Invariant.Assert( options.HasFlagFast( StructuralComparerOptions.GenericParameterCount ) );

            for ( var i = 0; i < methodX.TypeArguments.Length; i++ )
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

    private int CompareProperties( IPropertySymbol propertyX, IPropertySymbol propertyY, StructuralComparerOptions options )
    {
        if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
        {
            var result = StringComparer.Ordinal.Compare( propertyX.Name, propertyY.Name );

            if ( result != 0 )
            {
                return result;
            }
        }

        return this.CompareParameters( propertyX.Parameters, propertyY.Parameters, null, null, options );
    }

    private int CompareParameters(
        ImmutableArray<IParameterSymbol> methodXParameters,
        ImmutableArray<IParameterSymbol> methodYParameters,
        ITypeSymbol? methodXReturnType,
        ITypeSymbol? methodYReturnType,
        StructuralComparerOptions options )
    {
        int CompareParameterTypes( ITypeSymbol? parameterTypeX, ITypeSymbol? parameterTypeY )
        {
            // Prevent infinite recursion.
            var comparer = parameterTypeX?.ContainingSymbol is IMethodSymbol && parameterTypeY?.ContainingSymbol is IMethodSymbol ? NonRecursive : this;

            return comparer.Compare( parameterTypeX, parameterTypeY );
        }

        if ( options.HasFlagFast( StructuralComparerOptions.ParameterTypes )
             || options.HasFlagFast( StructuralComparerOptions.ParameterModifiers ) )
        {
            var result = methodXParameters.Length.CompareTo( methodYParameters.Length );

            if ( result != 0 )
            {
                return result;
            }

            for ( var i = 0; i < methodXParameters.Length; i++ )
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

    private static int CompareEvents( IEventSymbol eventX, IEventSymbol eventY, StructuralComparerOptions options )
    {
        if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
        {
            return StringComparer.Ordinal.Compare( eventX.Name, eventY.Name );
        }

        return 0;
    }

    private static int CompareFields( IFieldSymbol fieldX, IFieldSymbol fieldY, StructuralComparerOptions options )
    {
        if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
        {
            return StringComparer.Ordinal.Compare( fieldX.Name, fieldY.Name );
        }

        return 0;
    }

    private int CompareTypes( ITypeSymbol? typeX, ITypeSymbol? typeY )
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

        if ( this.Options.HasFlagFast( StructuralComparerOptions.Nullability ) )
        {
            // PERF: Cast enum to byte otherwise it will be boxed on .NET Framework.
            result = Comparer<byte>.Default.Compare( (byte) typeX.NullableAnnotation, (byte) typeY.NullableAnnotation );

            if ( result != 0 )
            {
                return result;
            }
        }

        switch (typeX, typeY)
        {
            case (ITypeParameterSymbol typeParamX, ITypeParameterSymbol typeParamY):
                result = StringComparer.Ordinal.Compare( typeParamX.Name, typeParamY.Name );

                if ( result != 0 )
                {
                    return result;
                }

                if ( this.Options.HasFlagFast( StructuralComparerOptions.ContainingDeclaration ) )
                {
                    result = NonRecursive.Compare( typeParamX.ContainingSymbol, typeParamY.ContainingSymbol );
                }

                return result;

            case (INamedTypeSymbol namedTypeX, INamedTypeSymbol namedTypeY):
                return this.CompareNamedTypes( namedTypeX, namedTypeY, this.Options );

            case (IArrayTypeSymbol arrayTypeX, IArrayTypeSymbol arrayTypeY):
                result = arrayTypeX.Rank.CompareTo( arrayTypeY.Rank );

                if ( result != 0 )
                {
                    return result;
                }

                return this.CompareTypes( arrayTypeX.ElementType, arrayTypeY.ElementType );

            case (IDynamicTypeSymbol, IDynamicTypeSymbol):
                return 0;

            case (IPointerTypeSymbol xPointerType, IPointerTypeSymbol yPointerType):
                return this.CompareTypes( xPointerType.PointedAtType, yPointerType.PointedAtType );

            case (IFunctionPointerTypeSymbol xFunctionPointerType, IFunctionPointerTypeSymbol yFunctionPointerType):
                return this.CompareMethods(
                    xFunctionPointerType.Signature,
                    yFunctionPointerType.Signature,
                    StructuralComparerOptions.FunctionPointer );

            default:
                throw new NotImplementedException( $"{typeX.Kind}" );
        }
    }

    private int CompareContainingDeclarations( ISymbol? x, ISymbol? y )
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
            var result = Comparer<int>.Default.Compare( (int) currentX.Kind, (int) currentY.Kind );

            if ( result != 0 )
            {
                return result;
            }

            switch (currentX, currentY)
            {
                case (IMethodSymbol methodX, IMethodSymbol methodY):
                    result = this.CompareMethods( methodX, methodY, StructuralComparerOptions.MethodSignature );

                    if ( result != 0 )
                    {
                        return result;
                    }

                    break;

                case (INamedTypeSymbol namedTypeX, INamedTypeSymbol namedTypeY):
                    result = this.CompareNamedTypes( namedTypeX, namedTypeY, StructuralComparerOptions.Type );

                    if ( result != 0 )
                    {
                        return result;
                    }

                    break;

                case (INamespaceSymbol namespaceX, INamespaceSymbol namespaceY):
                    result = StringComparer.Ordinal.Compare( namespaceX.Name, namespaceY.Name );

                    if ( result != 0 )
                    {
                        return result;
                    }

                    break;

                case (IModuleSymbol _, IModuleSymbol _):
                    return 0;

                default:
                    throw new NotImplementedException( $"{currentX.Kind}" );
            }

            currentX = currentX.ContainingSymbol;
            currentY = currentY.ContainingSymbol;
        }
    }

    private static int CompareContainingModules( IModuleSymbol? moduleX, IModuleSymbol? moduleY )
    {
        if ( ReferenceEquals( moduleX, moduleY ) )
        {
            return 0;
        }

        if ( moduleX == null )
        {
            return -1;
        }

        if ( moduleY == null )
        {
            return 1;
        }

        var result = StringComparer.Ordinal.Compare( moduleX.Name, moduleY.Name );

        if ( result != 0 )
        {
            return result;
        }

        return CompareAssemblies( moduleX.ContainingAssembly, moduleY.ContainingAssembly );
    }

    public int GetHashCode( ISymbol symbol ) => GetHashCode( symbol, this.Options );

    // For performance reasons, GetHashCode should use a limited subset of properties where collisions are likely.
    private static int GetHashCode( ISymbol symbol, StructuralComparerOptions options )
    {
        var h = 701_142_619; // Random prime.

        // PERF: Cast enum to int otherwise it will be boxed on .NET Framework.
        h = HashCode.Combine( h, (int) symbol.Kind );

        switch ( symbol )
        {
            case null:
                throw new ArgumentNullException( nameof(symbol) );

            case IParameterSymbol parameter:
                h = HashCode.Combine( h, GetHashCode( symbol.ContainingSymbol, options ), parameter.Ordinal );

                break;

            case INamedTypeSymbol type:
                if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
                {
                    h = HashCode.Combine( h, type.Name );
                }

                if ( options.HasFlagFast( StructuralComparerOptions.GenericParameterCount ) )
                {
                    h = HashCode.Combine( h, type.Arity );
                }

                break;

            case IMethodSymbol method:
                if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
                {
                    h = HashCode.Combine( h, method.Name );
                }

                if ( options.HasFlagFast( StructuralComparerOptions.ParameterTypes )
                     || options.HasFlagFast( StructuralComparerOptions.ParameterModifiers ) )
                {
                    h = HashCode.Combine( h, method.Parameters.Length );

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

            case IPropertySymbol property:
                if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
                {
                    h = HashCode.Combine( h, property.Name );
                }

                if ( options.HasFlagFast( StructuralComparerOptions.ParameterTypes )
                     || options.HasFlagFast( StructuralComparerOptions.ParameterModifiers ) )
                {
                    h = HashCode.Combine( h, property.Parameters.Length );

                    foreach ( var parameter in property.Parameters )
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

            case IFieldSymbol field:
                if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
                {
                    h = HashCode.Combine( h, field.Name );
                }

                break;

            case IEventSymbol @event:
                if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
                {
                    h = HashCode.Combine( h, @event.Name );
                }

                break;

            case INamespaceSymbol @namespace:
                if ( options.HasFlagFast( StructuralComparerOptions.Name ) )
                {
                    h = HashCode.Combine( h, @namespace.Name );
                }

                break;

            case IModuleSymbol _:
                break;

            case ITypeParameterSymbol typeParameter:
                h = HashCode.Combine( h, typeParameter.Ordinal );

                break;

            case IArrayTypeSymbol arrayType:
                h = HashCode.Combine( h, arrayType.Rank, GetHashCode( arrayType.ElementType, StructuralComparerOptions.Type ) );

                break;

            case IDynamicTypeSymbol:
                h = 41574;

                break;

            case IAssemblySymbol assembly:
                return HashCode.Combine( h, AssemblyIdentityComparer.SimpleNameComparer.GetHashCode( assembly.Identity.Name ), assembly.Identity.Version );

            case IPointerTypeSymbol pointerType:
                return GetHashCode( pointerType.PointedAtType, StructuralComparerOptions.Type );

            case IFunctionPointerTypeSymbol functionPointerType:
                return GetHashCode( functionPointerType.Signature, StructuralComparerOptions.FunctionPointer );

            case ILocalSymbol local:
                // TODO: Is this correct in all options?
                h = HashCode.Combine( h, local.Name );

                break;

            default:
                throw new NotImplementedException( $"Not implemented: '{symbol}' of kind {symbol.Kind}." );
        }

        if ( options.HasFlagFast( StructuralComparerOptions.ContainingDeclaration ) )
        {
            var current = symbol.ContainingSymbol;

            while ( current != null )
            {
                switch ( current )
                {
                    case INamedTypeSymbol namedType:
                        h = HashCode.Combine( h, namedType.Name, namedType.Arity );

                        break;

                    case INamespaceSymbol @namespace:
                        h = HashCode.Combine( h, @namespace.Name );

                        break;

                    case IMethodSymbol method:
                        h = HashCode.Combine( h, method.Name, method.Arity, method.Parameters.Length );

                        // This runs only if the original symbol was a local function.
                        foreach ( var parameter in method.Parameters )
                        {
                            h = HashCode.Combine( h, GetHashCode( parameter.Type, StructuralComparerOptions.Type ) );
                        }

                        break;

                    case IPropertySymbol property:
                        h = HashCode.Combine( h, property.Name, property.Parameters.Length );

                        // This runs only if the original symbol was a local function.
                        foreach ( var parameter in property.Parameters )
                        {
                            h = HashCode.Combine( h, GetHashCode( parameter.Type, StructuralComparerOptions.Type ) );
                        }

                        break;

                    case IFieldSymbol:
                    case IEventSymbol:
                        h = HashCode.Combine( h, current.Name );

                        break;

                    case IAssemblySymbol:
                    case IModuleSymbol:
                        // These are included below if required.
                        break;

                    default:
                        throw new NotImplementedException( $"{current.Kind}" );
                }

                current = current.ContainingSymbol;
            }
        }

        if ( options.HasFlagFast( StructuralComparerOptions.ContainingAssembly ) )
        {
            // Version should not differ often.
            h = HashCode.Combine( h, symbol.ContainingModule?.Name, symbol.ContainingAssembly?.Name );
        }

        return h;
    }
}