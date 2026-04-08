// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.RunTime.Initialization;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Lazily resolves and caches <see cref="InitializableTypeInfo"/> for types that implement
/// <c>IInitializable</c> (directly or inherited). Works with types from both
/// the current compilation and referenced assemblies.
/// </summary>
internal sealed class InitializableTypeRegistry
{
    private readonly CompilationContext _compilationContext;
    private readonly INamedTypeSymbol _initializableInterfaceType;
    private readonly INamedTypeSymbol _initializationContextType;

    /// <summary>
    /// Cache for resolved type info (includes inheritance lookups).
    /// A null value means the type was checked and does not implement <c>IInitializable</c>.
    /// </summary>
    private readonly ConcurrentDictionary<INamedTypeSymbol, InitializableTypeInfo?> _cache;

    public InitializableTypeRegistry( CompilationContext compilationContext )
    {
        this._compilationContext = compilationContext;

        this._initializableInterfaceType =
            (INamedTypeSymbol) compilationContext.ReflectionMapper.GetTypeSymbol( typeof(IInitializable) );

        this._initializationContextType =
            (INamedTypeSymbol) compilationContext.ReflectionMapper.GetTypeSymbol( typeof(InitializationContext) );

        this._cache = new ConcurrentDictionary<INamedTypeSymbol, InitializableTypeInfo?>( compilationContext.SymbolComparer );
    }

    /// <summary>
    /// Looks up whether the given type (or any base) implements <c>IInitializable</c>.
    /// Results are lazily resolved and cached for repeated lookups of the same type.
    /// </summary>
    public bool TryGetTypeInfo( INamedTypeSymbol type, out InitializableTypeInfo info )
    {
        var result = this._cache.GetOrAdd( type, this.ResolveTypeInfo );

        if ( result != null )
        {
            info = result;

            return true;
        }

        info = null!;

        return false;
    }

    private InitializableTypeInfo? ResolveTypeInfo( INamedTypeSymbol type )
    {
        // Check if the type implements IInitializable.
        if ( !this.ImplementsIInitializable( type ) )
        {
            return null;
        }

        // Find the Initialize method in the hierarchy.
        var initializeMethod = FindInitializeMethodInHierarchy( type );

        if ( initializeMethod == null )
        {
            return null;
        }

        // Build the per-constructor context param name dictionary.
        var constructorContextParamName = this.ResolveConstructorContextParams( type );

        return new InitializableTypeInfo( type, initializeMethod, constructorContextParamName );
    }

    /// <summary>
    /// For each constructor, determines the <c>InitializationContext</c> parameter name to use
    /// as a named argument, or <c>null</c> if no context argument should be appended.
    /// Handles two cases:
    /// <list type="bullet">
    /// <item>Case 1: The constructor itself has an <c>InitializationContext</c> parameter (typically optional).</item>
    /// <item>Case 2: A sibling overload exists with the same leading parameters plus an
    /// <c>InitializationContext</c> parameter — appending a named argument triggers overload resolution.</item>
    /// </list>
    /// </summary>
    private Dictionary<IMethodSymbol, string?> ResolveConstructorContextParams( INamedTypeSymbol type )
    {
        var result = new Dictionary<IMethodSymbol, string?>( this._compilationContext.SymbolComparer );
        var constructors = type.InstanceConstructors;

        foreach ( var ctor in constructors )
        {
            // Case 1: The constructor itself has an InitializationContext parameter.
            var contextParam = this.FindInitializationContextParameter( ctor );

            if ( contextParam != null )
            {
                result[ctor] = contextParam.Name;

                continue;
            }

            // Case 2: Find a sibling overload that has the same leading parameters
            // plus an InitializationContext parameter.
            var overloadParamName = this.FindOverloadContextParamName( ctor, constructors );
            result[ctor] = overloadParamName;
        }

        return result;
    }

    /// <summary>
    /// Searches for a sibling constructor overload that has all the same required parameters
    /// as <paramref name="constructor"/> plus an <c>InitializationContext</c> parameter.
    /// Returns the parameter name, or <c>null</c> if no matching overload exists.
    /// </summary>
    private string? FindOverloadContextParamName(
        IMethodSymbol constructor,
        IReadOnlyList<IMethodSymbol> allConstructors )
    {
        var requiredParamCount = constructor.Parameters.Count( p => !p.IsOptional );

        foreach ( var candidate in allConstructors )
        {
            if ( this._compilationContext.SymbolComparer.Equals( candidate, constructor ) )
            {
                continue;
            }

            // The candidate must have at least the same required parameters as the original,
            // plus the InitializationContext parameter.
            if ( candidate.Parameters.Length < requiredParamCount + 1 )
            {
                continue;
            }

            var contextParam = this.FindInitializationContextParameter( candidate );

            if ( contextParam == null )
            {
                continue;
            }

            // Check that all required parameters of the original constructor match the candidate's
            // leading parameters by type.
            if ( Enumerable.Range( 0, requiredParamCount )
                .All(
                    i => this._compilationContext.SymbolComparer.Equals(
                        constructor.Parameters[i].Type,
                        candidate.Parameters[i].Type ) ) )
            {
                return contextParam.Name;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks whether the given type implements <c>IInitializable</c>.
    /// </summary>
    private bool ImplementsIInitializable( INamedTypeSymbol type )
        => this._compilationContext.Compilation.HasImplicitConversion( type, this._initializableInterfaceType );

    private static IMethodSymbol? FindInitializeMethodInHierarchy( INamedTypeSymbol type )
    {
        var current = type;

        while ( current != null )
        {
            var method = FindInitializeMethod( current );

            if ( method != null )
            {
                return method;
            }

            current = current.BaseType;
        }

        return null;
    }

    private static IMethodSymbol? FindInitializeMethod( INamedTypeSymbol type )
        => type.GetMembers( nameof(IInitializable.Initialize) )
            .OfType<IMethodSymbol>()
            .FirstOrDefault( m => m.Parameters.Length == 1 );

    private IParameterSymbol? FindInitializationContextParameter( IMethodSymbol constructor )
        => constructor.Parameters.FirstOrDefault(
            p => this._compilationContext.SymbolComparer.Equals( p.Type, this._initializationContextType ) );
}
