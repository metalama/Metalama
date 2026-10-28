// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Analyzers
{
    /// <summary>
    /// The execution scope of a type, as far as the scope attributes of Metalama determine it.
    /// </summary>
    internal enum ExecutionScope
    {
        /// <summary>
        /// No scope attribute applies to the type.
        /// </summary>
        None,

        /// <summary>
        /// The type exists only at run time.
        /// </summary>
        RunTime,

        /// <summary>
        /// The type exists only at compile time.
        /// </summary>
        CompileTime,

        /// <summary>
        /// The type exists both at run time and at compile time.
        /// </summary>
        RunTimeOrCompileTime,

        /// <summary>
        /// The scope attributes that apply to the type contradict each other.
        /// </summary>
        Conflict
    }

    /// <summary>
    /// Determines the execution scope of a type from the scope attributes of Metalama: <c>[CompileTime]</c>,
    /// <c>[RunTime]</c> and <c>[RunTimeOrCompileTime]</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a subset of the rules of <c>SymbolClassifier</c> in <c>Metalama.Framework.Engine</c>, which this
    /// assembly cannot reference. The type takes the scope of its own attribute. Otherwise, a nested type takes the
    /// compile-time scope of its containing type, and a top-level type takes the scope of an assembly-level attribute.
    /// Otherwise, the type combines the scopes of its base type and of its interfaces. This is how an aspect, which
    /// implements <c>IAspect</c>, and a fabric, which derives from <c>Fabric</c>, become compile-time code.
    /// </para>
    /// <para>
    /// The classifier of the engine also infers a scope from the types that a type references. This subset does not,
    /// so a type that this subset classifies as <see cref="ExecutionScope.None"/> may still be compile-time code.
    /// </para>
    /// </remarks>
    internal static class ExecutionScopeFacts
    {
        private const string _aspectsNamespace = "Metalama.Framework.Aspects";
        private const int _maxDepth = 32;

        /// <summary>
        /// Gets the execution scope of a type.
        /// </summary>
        public static ExecutionScope GetScope( INamedTypeSymbol type ) => GetScope( type, 0 );

        /// <summary>
        /// Gets the execution scope that a scope attribute on a declaration states, or <see cref="ExecutionScope.None"/>
        /// when the declaration has no scope attribute.
        /// </summary>
        public static ExecutionScope GetScopeFromAttributes( ISymbol symbol )
            => symbol.GetAttributes().Select( GetScope ).FirstOrDefault( s => s != ExecutionScope.None );

        private static ExecutionScope GetScope( INamedTypeSymbol type, int depth )
        {
            if ( depth > _maxDepth )
            {
                return ExecutionScope.None;
            }

            type = type.OriginalDefinition;

            var scope = GetScopeFromAttributes( type );

            if ( scope != ExecutionScope.None )
            {
                return scope;
            }

            if ( type.ContainingType != null )
            {
                var containingTypeScope = GetScope( type.ContainingType, depth + 1 );

                if ( containingTypeScope == ExecutionScope.CompileTime )
                {
                    return ExecutionScope.CompileTime;
                }

                // A compile-time base type, for instance TypeFabric, still takes precedence over a run-time-or-compile-time
                // containing type.
                if ( containingTypeScope == ExecutionScope.RunTimeOrCompileTime )
                {
                    scope = ExecutionScope.RunTimeOrCompileTime;
                }
            }
            else
            {
                var assemblyScope = GetAssemblyScope( type.ContainingAssembly );

                if ( assemblyScope is ExecutionScope.CompileTime or ExecutionScope.RunTime )
                {
                    return assemblyScope;
                }
            }

            if ( type.BaseType != null )
            {
                scope = Combine( scope, GetScope( type.BaseType, depth + 1 ), false );
            }

            foreach ( var @interface in type.AllInterfaces )
            {
                scope = Combine( scope, GetScope( @interface, depth + 1 ), true );
            }

            return scope;
        }

        private static ExecutionScope GetAssemblyScope( IAssemblySymbol? assembly )
        {
            if ( assembly == null )
            {
                return ExecutionScope.None;
            }

            var scope = GetScopeFromAttributes( assembly );

            if ( scope == ExecutionScope.None && assembly.Modules.FirstOrDefault() is { } module )
            {
                scope = GetScopeFromAttributes( module );
            }

            return scope;
        }

        private static ExecutionScope GetScope( AttributeData attribute )
        {
            if ( attribute.AttributeClass is not { } attributeClass
                 || attributeClass.ContainingNamespace?.ToDisplayString() != _aspectsNamespace )
            {
                return ExecutionScope.None;
            }

            return attributeClass.Name switch
            {
                "CompileTimeAttribute" => ExecutionScope.CompileTime,
                "RunTimeAttribute" => ExecutionScope.RunTime,
                "RunTimeOrCompileTimeAttribute" or "ForcedGenericRunTimeOrCompileTimeAttribute" => ExecutionScope.RunTimeOrCompileTime,
                _ => ExecutionScope.None
            };
        }

        /// <summary>
        /// Combines the scope accumulated for a type with the scope of one of its base types or interfaces, with the
        /// rules of <c>SymbolClassifier.CombineBaseTypeScope</c>.
        /// </summary>
        private static ExecutionScope Combine( ExecutionScope combined, ExecutionScope baseTypeScope, bool baseTypeIsInterface )
            => (baseTypeScope, combined) switch
            {
                (ExecutionScope.None, _) => combined,
                (_, ExecutionScope.None) => baseTypeScope,
                _ when baseTypeScope == combined => combined,
                (ExecutionScope.Conflict, _) or (_, ExecutionScope.Conflict) => ExecutionScope.Conflict,

                // A run-time-or-compile-time type can implement interfaces that are compile-time or run-time.
                (_, ExecutionScope.RunTimeOrCompileTime) when baseTypeIsInterface => combined,
                (_, ExecutionScope.RunTimeOrCompileTime) => baseTypeScope,
                (ExecutionScope.RunTimeOrCompileTime, _) => combined,
                _ => ExecutionScope.Conflict
            };
    }
}
