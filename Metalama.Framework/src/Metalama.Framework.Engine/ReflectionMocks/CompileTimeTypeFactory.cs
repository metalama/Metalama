// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using TypeKind = Microsoft.CodeAnalysis.TypeKind;

namespace Metalama.Framework.Engine.ReflectionMocks
{
    /// <summary>
    /// Creates and ensures the uniqueness of <see cref="CompileTimeType"/> instances. This is the only way to obtain one:
    /// the mock constructors are not accessible elsewhere, so that two mocks of the same type are always the same instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A mock is built structurally, one cached instance per level: the element type of an array, or each argument of a
    /// constructed generic type, is itself a cached mock. This is what lets a mock be serialized without a compilation.
    /// </para>
    /// <para>
    /// The class does not depend on the project (nor on any compilation): the instances it creates hold only strings and
    /// a durable <c>TypeIdRef</c>. It is registered as an <see cref="IProjectService"/> because we want the lifetime and
    /// scope of its dictionary to be project-scoped, not because it needs anything from the project.
    /// </para>
    /// </remarks>
    internal class CompileTimeTypeFactory : IProjectService
    {
        // Keyed by SerializableTypeId, including for type parameters: Get() asks for the id with the generic context
        // included, which embeds the declaring type and so distinguishes the 'T' of one type from the 'T' of another.
        private readonly ConcurrentDictionary<string, CompileTimeType> _instances = new( StringComparer.Ordinal );

        public CompileTimeType Get( ITypeSymbol symbol )
            => symbol.Kind switch
            {
                SymbolKind.DynamicType when symbol is IDynamicTypeSymbol => throw new AssertionFailedException(
                    "Cannot get a System.Type for the 'dynamic' type." ),
                SymbolKind.ArrayType when symbol is IArrayTypeSymbol { ElementType: IDynamicTypeSymbol } => throw new AssertionFailedException(
                    "Cannot get a System.Type for the 'dynamic[]' type." ),
                _ => this.GetOrAdd( symbol.GetSerializableTypeId( true ), symbol, static ( f, id, s ) => f.CreateFromSymbol( id, s ) )
            };

        /// <summary>
        /// Creates a mock from a reflection full name, i.e. from the wire, without any compilation. A nested type is
        /// separated from its declaring type by <c>+</c> (e.g. <c>Ns.Outer+Inner</c>), which is what the serialization
        /// format has always stored (see <c>ReflectionHelper.GetReflectionName</c>), so the chain of declaring types is
        /// rebuilt from it here.
        /// </summary>
        /// <remarks>
        /// Only the innermost type's kind is known from the wire, so the declaring types are reconstructed with their
        /// enum-ness and value-type-ness left unknown rather than guessed.
        /// </remarks>

        // These two overloads do not consult the instance cache (the wire gives no SerializableTypeId to key on), so
        // they could be static. They are deliberately kept as instance members: creating a mock must go through the
        // project-scoped factory, which is the invariant the whole class exists to enforce. Making them static would
        // also strip the factory dependency from SerializationReader, its only caller.
#pragma warning disable CA1822
        public CompileTimeType CreateNamedType( string fullName, string assemblyName, bool? isEnum, bool? isValueType )
        {
            var lastNestedSeparator = fullName.LastIndexOf( '+' );

            var declaringType = lastNestedSeparator < 0
                ? null
                : (CompileTimeNamedType) this.CreateNamedType( fullName[..lastNestedSeparator], assemblyName, null, null );

            var split = TypeNameHelper.SplitNamespaceAndName( fullName );

            // Only the reflection name is known from the wire, so the generic arguments are not reconstructed. A generic
            // type built this way is still recognized as one by the arity backtick in its name; it just has no arguments.
            return new CompileTimeNamedType(
                null,
                split.Name,
                declaringType != null ? null : split.Namespace,
                assemblyName,
                isEnum,
                isValueType,
                null,
                default,
                declaringType );
        }

        public Type CreateNamedType( Type type )
        {
            // The namespace comes from the mock declaring type when there is one; otherwise (a top-level type, or a real
            // declaring type that is not itself a mock) it is taken from the reflection type directly.
            var mockDeclaringType = type.DeclaringType as CompileTimeNamedType;

            return new CompileTimeNamedType(
                null,
                type.Name,
                mockDeclaringType != null ? null : type.Namespace,
                (type as CompileTimeType)?.AssemblyName ?? type.Assembly.GetName().Name.AssertNotNull(),
                type.IsEnumTypeOrNull(),
                type.IsValueTypeOrNull(),
                null,
                default,
                mockDeclaringType );
        }
#pragma warning restore CA1822

        private CompileTimeType GetOrAdd<TState>(
            SerializableTypeId typeId,
            TState state,
            Func<CompileTimeTypeFactory, SerializableTypeId, TState, CompileTimeType> create )
            => this._instances.GetOrAdd(
                typeId.Id,
                static ( key, x ) => x.create( x.me, new SerializableTypeId( key ), x.state ),
                (me: this, state, create) );

        private CompileTimeType CreateFromSymbol( SerializableTypeId typeId, ITypeSymbol symbol )
        {
            var name = symbol.GetReflectionName().AssertNotNull();

            // Switching on Kind before the type test: a Kind comparison is cheaper than an interface type check.
            // ErrorType is included with NamedType because IErrorTypeSymbol derives from INamedTypeSymbol, so it was
            // matched by the previous `case INamedTypeSymbol` too.
            switch ( symbol.Kind )
            {
                case SymbolKind.ArrayType when symbol is IArrayTypeSymbol arrayType:
                    return new CompileTimeArrayType( typeId, this.Get( arrayType.ElementType ), arrayType.Rank );

                case SymbolKind.PointerType when symbol is IPointerTypeSymbol pointerType:
                    return new CompileTimePointerType( typeId, this.Get( pointerType.PointedAtType ) );

                case SymbolKind.TypeParameter when symbol is ITypeParameterSymbol typeParameter:
                    return new CompileTimeGenericParameterType(
                        typeId,
                        name,
                        typeParameter.DeclaringType == null ? null : this.Get( typeParameter.DeclaringType ),
                        typeParameter.Ordinal );

                case SymbolKind.NamedType or SymbolKind.ErrorType when symbol is INamedTypeSymbol namedType:
                    {
                        var isConstructed = namedType is { IsGenericType: true } && !namedType.IsGenericTypeDefinition();

                        // Only a *constructed* type populates its arguments. An open definition's arguments are its type
                        // parameters, but a parameter's own DeclaringType is the definition, so populating them would
                        // re-enter Get() for the definition that is still being built and overflow the stack. An open
                        // definition therefore keeps an empty argument list.
                        var genericArguments = isConstructed
                            ? namedType.TypeArguments.SelectAsImmutableArray( a => (Type) this.Get( a ) )
                            : ImmutableArray<Type>.Empty;

                        return new CompileTimeNamedType(
                            typeId,
                            name,
                            namedType.ContainingNamespace?.GetFullName(),
                            namedType.ContainingAssembly.Name,
                            namedType.TypeKind == TypeKind.Enum,
                            namedType.IsValueType,
                            isConstructed ? (CompileTimeNamedType) this.Get( namedType.OriginalDefinition ) : null,
                            genericArguments,
                            namedType.ContainingType == null ? null : (CompileTimeNamedType) this.Get( namedType.ContainingType ) );
                    }

                default:
                    throw new AssertionFailedException( $"Don't know how to build a CompileTimeType for '{symbol}' ({symbol.Kind})." );
            }
        }
    }
}