// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;

namespace Metalama.Framework.Analyzers.Immutability
{
    internal sealed partial class ImmutabilityContext
    {
        /// <summary>
        /// The namespace of the attributes that declare an execution scope.
        /// </summary>
        private const string _scopeAttributeNamespace = "Metalama.Framework.Aspects";

        private readonly ConcurrentDictionary<INamedTypeSymbol, ExecutionScope?> _executionScopes =
            new( SymbolEqualityComparer.Default );

        /// <summary>
        /// Determines the execution scope of a type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The rules are the subset of <c>SymbolClassifier</c> in the engine that applies to a type declared in source,
        /// evaluated in the same order:
        /// </para>
        /// <list type="number">
        /// <item>A scope attribute on the type itself decides.</item>
        /// <item>A nested type of a compile-time type is compile-time. A nested type of a run-time-or-compile-time type
        /// starts from that scope.</item>
        /// <item>A top-level type takes the scope of an <c>[assembly: CompileTime]</c> or <c>[assembly: RunTime]</c>
        /// attribute, when there is one.</item>
        /// <item>Otherwise, the scopes of the base type and of every implemented interface are combined. An interface
        /// does not change the scope of a run-time-or-compile-time type, which is why an aspect remains
        /// run-time-or-compile-time although <c>IAspect</c> derives from the compile-time
        /// <c>ICompileTimeSerializable</c>.</item>
        /// <item>A type that none of these rules classifies is run-time.</item>
        /// </list>
        /// <para>
        /// The engine also classifies the types of the <c>System</c> namespaces by looking them up in the .NET Standard
        /// reference assemblies. That lookup is not reproduced here. A type of a <c>System</c> namespace is treated as
        /// unclassified, which is what the engine does for such a type when it classifies a derived type.
        /// </para>
        /// </remarks>
        public ExecutionScope GetExecutionScope( INamedTypeSymbol type ) => this.GetExecutionScopeOrNull( type, 0 ) ?? ExecutionScope.RunTime;

        /// <summary>
        /// Determines the execution scope of a type, or returns <c>null</c> when no rule classifies it.
        /// </summary>
        private ExecutionScope? GetExecutionScopeOrNull( INamedTypeSymbol type, int depth )
        {
            type = type.OriginalDefinition;

            if ( this._executionScopes.TryGetValue( type, out var cached ) )
            {
                return cached;
            }

            if ( depth > _maxDepth )
            {
                return null;
            }

            var result = this.ComputeExecutionScope( type, depth );
            this._executionScopes.TryAdd( type, result );

            return result;
        }

        private ExecutionScope? ComputeExecutionScope( INamedTypeSymbol type, int depth )
        {
            var scopeFromAttribute = GetScopeFromAttributes( type );

            if ( scopeFromAttribute != null )
            {
                return scopeFromAttribute;
            }

            ExecutionScope? combinedScope = null;
            ExecutionScope? containingTypeScope = null;

            if ( type.ContainingType != null )
            {
                containingTypeScope = this.GetExecutionScopeOrNull( type.ContainingType, depth + 1 );

                switch ( containingTypeScope )
                {
                    case ExecutionScope.CompileTime:
                        return ExecutionScope.CompileTime;

                    case ExecutionScope.RunTimeOrCompileTime:
                        // The base types are still examined, because a nested fabric is compile-time.
                        combinedScope = ExecutionScope.RunTimeOrCompileTime;

                        break;
                }
            }
            else
            {
                var assemblyScope = GetAssemblyScope( type.ContainingAssembly );

                if ( assemblyScope != null )
                {
                    return assemblyScope;
                }
            }

            if ( type.BaseType != null )
            {
                combinedScope = Combine( this.GetExecutionScopeOrNull( type.BaseType, depth + 1 ), combinedScope, false );
            }

            foreach ( var interfaceType in type.AllInterfaces )
            {
                combinedScope = Combine( this.GetExecutionScopeOrNull( interfaceType, depth + 1 ), combinedScope, true );
            }

            if ( combinedScope != null )
            {
                return combinedScope;
            }

            if ( IsInSystemNamespace( type ) )
            {
                return null;
            }

            return containingTypeScope ?? ExecutionScope.RunTime;
        }

        /// <summary>
        /// Combines the scope accumulated so far for a type with the scope of one of its base types or interfaces.
        /// </summary>
        /// <remarks>
        /// This follows <c>SymbolClassifier.CombineBaseTypeScope</c>. A conflict between a compile-time and a run-time
        /// base type is reported by the engine as an error, so the analyzer keeps the scope accumulated so far and does
        /// not report anything about it.
        /// </remarks>
        private static ExecutionScope? Combine( ExecutionScope? baseTypeScope, ExecutionScope? combinedScope, bool baseTypeIsInterface )
        {
            if ( baseTypeScope == null )
            {
                return combinedScope;
            }

            if ( combinedScope == null || baseTypeScope == combinedScope )
            {
                return baseTypeScope;
            }

            if ( combinedScope == ExecutionScope.RunTimeOrCompileTime )
            {
                // A run-time-or-compile-time type may implement a compile-time or run-time interface, but a base class
                // of another scope determines the scope of the derived type.
                return baseTypeIsInterface ? combinedScope : baseTypeScope;
            }

            return combinedScope;
        }

        /// <summary>
        /// Returns the scope declared by an attribute on a type, or <c>null</c> when the type carries none.
        /// </summary>
        private static ExecutionScope? GetScopeFromAttributes( ISymbol symbol )
        {
            foreach ( var attribute in symbol.GetAttributes() )
            {
                var scope = GetScopeFromAttribute( attribute );

                if ( scope != null )
                {
                    return scope;
                }
            }

            return null;
        }

        private static ExecutionScope? GetScopeFromAttribute( AttributeData attribute )
        {
            if ( attribute.AttributeClass is not { } attributeClass
                 || attributeClass.ContainingNamespace?.ToDisplayString() != _scopeAttributeNamespace )
            {
                return null;
            }

            return attributeClass.Name switch
            {
                "CompileTimeAttribute" => ExecutionScope.CompileTime,
                "RunTimeAttribute" => ExecutionScope.RunTime,
                "RunTimeOrCompileTimeAttribute" or "ForcedGenericRunTimeOrCompileTimeAttribute" => ExecutionScope.RunTimeOrCompileTime,
                _ => null
            };
        }

        /// <summary>
        /// Returns the scope that an assembly-level attribute gives to every top-level type of the assembly, or
        /// <c>null</c> when the assembly does not declare one.
        /// </summary>
        /// <remarks>
        /// An <c>[assembly: RunTimeOrCompileTime]</c> attribute is ignored, as in the engine, because the scope must
        /// then be decided for each type.
        /// </remarks>
        private static ExecutionScope? GetAssemblyScope( IAssemblySymbol? assembly )
        {
            if ( assembly == null )
            {
                return null;
            }

            if ( assembly.Name == "Metalama.Compiler.Interface" )
            {
                return ExecutionScope.CompileTime;
            }

            var scope = GetScopeFromAttributes( assembly );

            if ( scope == null )
            {
                foreach ( var module in assembly.Modules )
                {
                    scope = GetScopeFromAttributes( module );

                    break;
                }
            }

            return scope == ExecutionScope.RunTimeOrCompileTime ? null : scope;
        }

        private static bool IsInSystemNamespace( INamedTypeSymbol type )
        {
            var rootNamespace = type.ContainingNamespace;

            while ( rootNamespace?.ContainingNamespace is { IsGlobalNamespace: false } parent )
            {
                rootNamespace = parent;
            }

            return rootNamespace is { IsGlobalNamespace: false, Name: "System" };
        }
    }
}
