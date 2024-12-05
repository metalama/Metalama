// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Comparers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;
using RoslynSpecialType = Microsoft.CodeAnalysis.SpecialType;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypedConstant = Microsoft.CodeAnalysis.TypedConstant;
using TypeKind = Microsoft.CodeAnalysis.TypeKind;

namespace Metalama.Framework.Engine.Utilities.Roslyn
{
    public static class SymbolExtensions
    {
        // Coverage: ignore
        internal static SpecialType ToOurSpecialType( this RoslynSpecialType type )
            => type switch
            {
                RoslynSpecialType.System_Byte => SpecialType.Byte,
                RoslynSpecialType.System_SByte => SpecialType.SByte,
                RoslynSpecialType.System_Int16 => SpecialType.Int16,
                RoslynSpecialType.System_Int32 => SpecialType.Int32,
                RoslynSpecialType.System_Int64 => SpecialType.Int64,
                RoslynSpecialType.System_UInt16 => SpecialType.UInt16,
                RoslynSpecialType.System_UInt32 => SpecialType.UInt32,
                RoslynSpecialType.System_UInt64 => SpecialType.UInt64,
                RoslynSpecialType.System_String => SpecialType.String,
                RoslynSpecialType.System_Char => SpecialType.Char,
                RoslynSpecialType.System_Decimal => SpecialType.Decimal,
                RoslynSpecialType.System_Single => SpecialType.Single,
                RoslynSpecialType.System_Double => SpecialType.Double,
                RoslynSpecialType.System_Boolean => SpecialType.Boolean,
                RoslynSpecialType.System_Object => SpecialType.Object,
                RoslynSpecialType.System_Void => SpecialType.Void,
                RoslynSpecialType.System_Collections_IEnumerable => SpecialType.IEnumerable,
                RoslynSpecialType.System_Collections_IEnumerator => SpecialType.IEnumerator,
                RoslynSpecialType.System_Collections_Generic_IEnumerable_T => SpecialType.IEnumerable_T,
                RoslynSpecialType.System_Collections_Generic_IEnumerator_T => SpecialType.IEnumerator_T,
                _ => SpecialType.None
            };

        internal static RoslynSpecialType ToRoslynSpecialType( this SpecialType type )
            => type switch
            {
                SpecialType.Byte => RoslynSpecialType.System_Byte,
                SpecialType.SByte => RoslynSpecialType.System_SByte,
                SpecialType.Int16 => RoslynSpecialType.System_Int16,
                SpecialType.Int32 => RoslynSpecialType.System_Int32,
                SpecialType.Int64 => RoslynSpecialType.System_Int64,
                SpecialType.UInt16 => RoslynSpecialType.System_UInt16,
                SpecialType.UInt32 => RoslynSpecialType.System_UInt32,
                SpecialType.UInt64 => RoslynSpecialType.System_UInt64,
                SpecialType.String => RoslynSpecialType.System_String,
                SpecialType.Char => RoslynSpecialType.System_Char,
                SpecialType.Decimal => RoslynSpecialType.System_Decimal,
                SpecialType.Single => RoslynSpecialType.System_Single,
                SpecialType.Double => RoslynSpecialType.System_Double,
                SpecialType.Boolean => RoslynSpecialType.System_Boolean,
                SpecialType.Object => RoslynSpecialType.System_Object,
                SpecialType.Void => RoslynSpecialType.System_Void,
                SpecialType.IEnumerable => RoslynSpecialType.System_Collections_IEnumerable,
                SpecialType.IEnumerator => RoslynSpecialType.System_Collections_IEnumerator,
                SpecialType.IEnumerable_T => RoslynSpecialType.System_Collections_Generic_IEnumerable_T,
                SpecialType.IEnumerator_T => RoslynSpecialType.System_Collections_Generic_IEnumerator_T,

                // Note that we have special types that Roslyn does not have.
                _ => RoslynSpecialType.None
            };

        internal static bool IsGenericTypeDefinition( this INamedTypeSymbol namedType )
        {
            if ( namedType.IsUnboundGenericType )
            {
                return true;
            }

            if ( namedType.TypeArguments.Length != namedType.TypeParameters.Length )
            {
                return false;
            }

            foreach ( var t in namedType.TypeArguments )
            {
                if ( t is ITypeParameterSymbol p )
                {
                    if ( !p.ContainingSymbol.Equals( namedType ) )
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool AnyBaseType( this INamedTypeSymbol type, Predicate<INamedTypeSymbol> predicate )
        {
            for ( var t = type; t != null; t = t.BaseType )
            {
                if ( predicate( t ) )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get top-level (non-nested) types in an assembly.
        /// </summary>
        internal static IEnumerable<INamedTypeSymbol> GetTypes( this IAssemblySymbol assembly ) => assembly.GlobalNamespace.GetTypes();

        /// <summary>
        /// Get top-level (non-nested) types in a module.
        /// </summary>
        internal static IEnumerable<INamedTypeSymbol> GetTypes( this IModuleSymbol module ) => module.GlobalNamespace.GetTypes();

        /// <summary>
        /// Get all types in an assembly, including nested types.
        /// </summary>
        internal static IEnumerable<INamedTypeSymbol> GetAllTypes( this IAssemblySymbol assembly ) => assembly.GlobalNamespace.GetAllTypes();

        private static IEnumerable<INamedTypeSymbol> GetTypes( this INamespaceSymbol namespaceSymbol )
            => namespaceSymbol.SelectManyRecursive( ns => ns.GetNamespaceMembers(), true ).SelectMany( ns => ns.GetTypeMembers() );

        private static IEnumerable<INamedTypeSymbol> GetAllTypes( this INamespaceSymbol namespaceSymbol )
            => namespaceSymbol.GetTypes().SelectMany( type => type.SelectManyRecursive( t => t.GetTypeMembers(), true ) );

        internal static bool IsAccessor( this IMethodSymbol method )
            => method.MethodKind switch
            {
                MethodKind.PropertyGet => true,
                MethodKind.PropertySet => true,
                MethodKind.EventAdd => true,
                MethodKind.EventRemove => true,
                MethodKind.EventRaise => true,
                _ => false
            };

        internal static bool? HasModifier( this ISymbol symbol, SyntaxKind kind )
        {
            if ( symbol.DeclaringSyntaxReferences.IsEmpty )
            {
                if ( symbol is IMethodSymbol { MethodKind: MethodKind.Constructor, IsImplicitlyDeclared: true } && kind == SyntaxKind.UnsafeKeyword )
                {
                    return false;
                }

                return null;
            }

            return symbol.DeclaringSyntaxReferences.Any(
                r => r.GetSyntax() is MemberDeclarationSyntax member && member.Modifiers.Any( m => m.IsKind( kind ) ) );
        }

        internal static bool IsInterfaceMemberImplementation( this ISymbol symbol )
            => symbol switch
            {
                IMethodSymbol methodSymbol => methodSymbol.ExplicitInterfaceImplementations.Any(),
                IPropertySymbol propertySymbol => propertySymbol.ExplicitInterfaceImplementations.Any(),
                IEventSymbol eventSymbol => eventSymbol.ExplicitInterfaceImplementations.Any(),
                _ => false
            };

        // This won't return the correct result for invalid code where multiple properties have the same name,
        // but I think that's fine.
        internal static IFieldSymbol? GetBackingField( this IPropertySymbol property )
            => property.ContainingType.GetMembers( $"<{property.Name}>k__BackingField" )
                .OfType<IFieldSymbol>()
                .FirstOrDefault();

        // ReSharper disable once UnusedParameter.Global

        internal static IFieldSymbol? GetBackingField( this IEventSymbol @event )

            // TODO: Currently Roslyn does not expose the event field in the symbol model and therefore we cannot find it.
            => null;

        internal static ImmutableArray<IParameterSymbol> GetParameters( this ISymbol symbol )
            => symbol switch
            {
                IMethodSymbol method => method.Parameters,
                IPropertySymbol property => property.Parameters,
                _ => ImmutableArray<IParameterSymbol>.Empty
            };

        internal static bool HasDefaultConstructor( this INamedTypeSymbol type )
            => type.TypeKind == TypeKind.Struct ||
               (type is { TypeKind: TypeKind.Class, IsAbstract: false } &&
                type.InstanceConstructors.Any( ctor => ctor.Parameters.Length == 0 ));

        internal static bool IsVisibleTo( this ISymbol symbol, Compilation compilation, ISymbol otherSymbol )
            => compilation.IsSymbolAccessibleWithin(
                symbol,
                otherSymbol switch
                {
                    INamedTypeSymbol type => type,
                    _ => otherSymbol.ContainingType
                } );

        internal static bool IsPrimaryConstructor( this IMethodSymbol constructorSymbol )
        {
            var declarationSyntax = constructorSymbol.GetPrimaryDeclarationSyntax();

#if ROSLYN_4_8_0_OR_GREATER
            return
                constructorSymbol is { MethodKind: MethodKind.Constructor }
                && declarationSyntax is TypeDeclarationSyntax { ParameterList: not null };
#else
            return
                constructorSymbol is { MethodKind: MethodKind.Constructor }
                && declarationSyntax is RecordDeclarationSyntax { ParameterList: not null };
#endif
        }

        internal static FrameworkName? GetTargetFramework( this Compilation compilation )
        {
            var attribute = compilation.Assembly.GetAttributes().FirstOrDefault( a => a.AttributeClass?.Name == nameof(TargetFrameworkAttribute) );

            if ( attribute == null || attribute.ConstructorArguments.IsDefaultOrEmpty )
            {
                return null;
            }

            var frameworkNameString = (string?) attribute.ConstructorArguments[0].Value;

            if ( frameworkNameString == null )
            {
                return null;
            }

            return new FrameworkName( frameworkNameString );
        }

        internal static bool IsCompilerGenerated( this ISymbol declaration )
            => declaration.GetAttributes().Any( a => a.AttributeConstructor?.ContainingType.Name == nameof(CompilerGeneratedAttribute) );

        /// <summary>
        /// Gets the kind of operator based represented by the method.
        /// </summary>
        internal static OperatorKind GetOperatorKind( this IMethodSymbol method ) => SymbolHelpers.GetOperatorKindFromName( method.Name );

        public static INamedTypeSymbol GetTopmostContainingType( this INamedTypeSymbol type )
            => type.ContainingType == null ? type : type.ContainingType.GetTopmostContainingType();

        public static INamedTypeSymbol? GetClosestContainingType( this ISymbol symbol )
            => symbol switch
            {
                INamedTypeSymbol type => type,
                _ => symbol.ContainingType
            };

        internal static ISymbol? GetClosestContainingMember( this ISymbol symbol )
            => symbol switch
            {
                INamedTypeSymbol type => type,
                IMethodSymbol method => method,
                IFieldSymbol field => field,
                IPropertySymbol property => property,
                IEventSymbol @event => @event,
                INamespaceSymbol => null,
                _ => symbol.ContainingSymbol?.GetClosestContainingMember()
            };

        internal static bool IsTaskConfigureAwait( this ISymbol? symbol )
            => symbol is IMethodSymbol
            {
                Name: "ConfigureAwait",
                ContainingType: var containingType
            } && containingType.ConstructedFrom.GetReflectionFullName() is "System.Threading.Tasks.Task" or "System.Threading.Tasks.Task`1";

        internal static bool IsExplicitInterfaceMemberImplementation( this ISymbol? symbol )
            => symbol switch
            {
                IMethodSymbol method => method.ExplicitInterfaceImplementations.Length > 0,
                IPropertySymbol property => property.ExplicitInterfaceImplementations.Length > 0,
                IEventSymbol @event => @event.ExplicitInterfaceImplementations.Length > 0,
                _ => false
            };

        /// <summary>
        /// Translate a symbol to a different <see cref="CompilationContext"/> if necessary, but only in
        /// the debug build. This is to make a symbol compatible with the <see cref="SafeSymbolComparer"/>.
        /// </summary>
        internal static T TranslateIfNecessary<T>( this T symbol, CompilationContext compilation )
            where T : ISymbol
        {
#if DEBUG
            if ( symbol.BelongsToCompilation( compilation ) == false )
            {
                return (T) SymbolId.Create( symbol ).Resolve( compilation.Compilation ).AssertSymbolNotNull();
            }
#endif
            return symbol;
        }

        internal static bool TryGetNamedArgument( this AttributeData attribute, string name, out TypedConstant value )
        {
            foreach ( var argument in attribute.NamedArguments )
            {
                if ( argument.Key == name )
                {
                    value = argument.Value;

                    return true;
                }
            }

            value = default;

            return false;
        }
    }
}