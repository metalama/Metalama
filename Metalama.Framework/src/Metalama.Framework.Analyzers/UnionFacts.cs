// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Analyzers
{
    /// <summary>
    /// The facts about the unions of C# 15 that the contracts of this assembly need.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This assembly compiles against the minimal supported Roslyn, which does not declare <c>ITypeSymbol.IsUnion</c>
    /// or <c>ITypeSymbol.UnionCaseTypes</c>. The facts are therefore derived from the members of the type, by the rules
    /// that the compiler applies. <c>UnionFacet</c> in <c>Metalama.Framework.Engine</c> applies the same rules to the
    /// code model.
    /// </para>
    /// <para>
    /// The compiler lowers a union to a type whose only storage is the <c>Value</c> property, of type
    /// <see cref="object"/>. A rule that judged a union from that storage would judge every union in the same way,
    /// whatever its case types are.
    /// </para>
    /// </remarks>
    internal static class UnionFacts
    {
        private const string _unionAttributeMetadataName = "System.Runtime.CompilerServices.UnionAttribute";

        private const string _unionInterfaceMetadataName = "System.Runtime.CompilerServices.IUnion";

        /// <summary>
        /// The keyword of a union declaration.
        /// </summary>
        private const string _unionKeyword = "union";

        /// <summary>
        /// The identifier of the property that holds the value of the case that a union currently carries.
        /// </summary>
        private const string _valuePropertyName = "Value";

        /// <summary>
        /// The identifier of the nested interface on which the attribute form of a union may declare its creation
        /// members, which the language proposal names a union member provider.
        /// </summary>
        private const string _memberProviderInterfaceName = "IUnionMembers";

        /// <summary>
        /// The identifier of the static creation methods of a union member provider.
        /// </summary>
        private const string _creationMethodName = "Create";

        /// <summary>
        /// Determines whether a type is a union, either because it carries <c>UnionAttribute</c> or because it is
        /// declared with the <c>union</c> keyword.
        /// </summary>
        /// <remarks>
        /// The compiler emits <c>UnionAttribute</c> on a union declaration, so a union of a referenced assembly
        /// carries the attribute. A union declaration of the current compilation does not report the attribute in
        /// <see cref="ISymbol.GetAttributes"/>, so its declaration is read instead. The compiler makes a union
        /// declaration implement <c>IUnion</c>, which allows the declarations of every other type to be skipped.
        /// </remarks>
        public static bool IsUnion( ITypeSymbol type )
        {
            if ( type is not INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct } namedType )
            {
                return false;
            }

            foreach ( var attribute in namedType.GetAttributes() )
            {
                if ( attribute.AttributeClass is { Name: "UnionAttribute" } attributeClass
                     && SymbolFacts.GetFullMetadataName( attributeClass ) == _unionAttributeMetadataName )
                {
                    return true;
                }
            }

            if ( namedType.DeclaringSyntaxReferences.IsEmpty || !ImplementsUnionInterface( namedType ) )
            {
                return false;
            }

            foreach ( var reference in namedType.DeclaringSyntaxReferences )
            {
                if ( reference.GetSyntax() is TypeDeclarationSyntax { Keyword.ValueText: _unionKeyword } )
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ImplementsUnionInterface( INamedTypeSymbol type )
        {
            foreach ( var interfaceType in type.Interfaces )
            {
                if ( interfaceType.Name == "IUnion" && SymbolFacts.GetFullMetadataName( interfaceType ) == _unionInterfaceMetadataName )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Enumerates the creation members of a union, which are the members that create a value of one of its cases.
        /// The type of the single parameter of a creation member is a case type of the union.
        /// </summary>
        /// <remarks>
        /// A union that has a union member provider interface creates the value of a case through a static method
        /// named <c>Create</c> of that interface or of an interface that the interface inherits. Every other union
        /// creates it through a public constructor that takes one parameter. Two creation members may have the same
        /// case type.
        /// </remarks>
        public static IEnumerable<IMethodSymbol> GetCreationMembers( INamedTypeSymbol union )
        {
            var memberProviderInterface = GetMemberProviderInterface( union );

            if ( memberProviderInterface == null )
            {
                foreach ( var constructor in union.InstanceConstructors )
                {
                    if ( constructor is { DeclaredAccessibility: Accessibility.Public, Parameters.Length: 1 }
                         && IsSuitableCreationParameter( constructor.Parameters[0] ) )
                    {
                        yield return constructor;
                    }
                }

                yield break;
            }

            foreach ( var declaringInterface in GetMemberProviderInterfaces( memberProviderInterface ) )
            {
                foreach ( var member in declaringInterface.GetMembers( _creationMethodName ) )
                {
                    if ( member is IMethodSymbol
                         {
                             IsStatic: true,
                             DeclaredAccessibility: Accessibility.Public,
                             MethodKind: MethodKind.Ordinary,
                             IsGenericMethod: false,
                             RefKind: RefKind.None,
                             Parameters.Length: 1
                         } method
                         && IsSuitableCreationParameter( method.Parameters[0] )
                         && SymbolEqualityComparer.Default.Equals( method.ReturnType, union ) )
                    {
                        yield return method;
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether a member of a union is the <c>Value</c> property, which is the storage of the union, or
        /// the backing field of that property.
        /// </summary>
        public static bool IsValueStorage( ISymbol member )
            => (member is IFieldSymbol { AssociatedSymbol: { } associatedSymbol } ? associatedSymbol : member) is IPropertySymbol
            {
                Name: _valuePropertyName, IsStatic: false, Type.SpecialType: SpecialType.System_Object
            };

        private static IEnumerable<INamedTypeSymbol> GetMemberProviderInterfaces( INamedTypeSymbol memberProviderInterface )
        {
            yield return memberProviderInterface;

            foreach ( var baseInterface in memberProviderInterface.AllInterfaces )
            {
                yield return baseInterface;
            }
        }

        /// <summary>
        /// Returns the union member provider interface of a union, or <c>null</c> when the union has none. The
        /// interface is a nongeneric public nested interface named <c>IUnionMembers</c> that the union implements.
        /// </summary>
        private static INamedTypeSymbol? GetMemberProviderInterface( INamedTypeSymbol union )
        {
            foreach ( var nestedType in union.GetTypeMembers( _memberProviderInterfaceName ) )
            {
                if ( nestedType.Arity != 0 )
                {
                    continue;
                }

                if ( nestedType is not { DeclaredAccessibility: Accessibility.Public, TypeKind: TypeKind.Interface }
                     || !union.AllInterfaces.Contains( nestedType, SymbolEqualityComparer.Default ) )
                {
                    return null;
                }

                return nestedType;
            }

            return null;
        }

        /// <summary>
        /// Determines whether the single parameter of a candidate creation member is passed in a way that makes the
        /// member a creation member. The compiler admits a parameter passed by value or by <c>in</c> and no other.
        /// </summary>
        private static bool IsSuitableCreationParameter( IParameterSymbol parameter ) => parameter.RefKind is RefKind.None or RefKind.In;
    }
}
