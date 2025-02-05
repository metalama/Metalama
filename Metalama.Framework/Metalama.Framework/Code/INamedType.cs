// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents a class, struct, interface, enum, or delegate.
    /// </summary>
    public interface INamedType : IType, IGeneric, INamespaceOrNamedType, IEquatable<INamedType>
    {
        // TODO: there should probably be an interface to represent named tuples. It would be derived from INamedType
        // and be augmented by the names of tuple members.

        // TODO: the default constructor should be represented as a pseudo-method.
        bool HasDefaultConstructor { get; }

        /// <summary>
        /// Gets the type from which the current type derives.
        /// </summary>
        INamedType? BaseType { get; }

        /// <summary>
        /// Gets the list of all interfaces (recursive) that the current type implements.
        /// </summary>
        IImplementedInterfaceCollection AllImplementedInterfaces { get; }

        /// <summary>
        /// Gets the list of interfaces that the current type implements.
        /// </summary>
        IImplementedInterfaceCollection ImplementedInterfaces { get; }

        [Obsolete( "Use the ContainingNamespace property." )]
        INamespace Namespace { get; }

        /// <summary>
        /// Gets the namespace of the current type.
        /// </summary>
        new INamespace ContainingNamespace { get; }

        /// <summary>
        /// Gets the nested types of the current type.
        /// </summary>
        [Obsolete( "Use the Types property." )]
        INamedTypeCollection NestedTypes { get; }

        /// <summary>
        /// Gets the list of nested types defined in the current type or inherited from the base types.
        /// </summary>
        INamedTypeCollection AllTypes { get; }

        /// <summary>
        /// Gets the list of properties defined in the current type, but not those inherited from the base types.
        /// Note that fields can be promoted to properties by aspects, so a source code field can be 
        /// represented in the <see cref="Properties" /> collection instead of the <see cref="Fields"/>
        /// collection.
        /// </summary>
        IPropertyCollection Properties { get; }

        /// <summary>
        /// Gets the list of properties defined in the current type or inherited from the base types.
        /// Note that fields can be promoted to properties by aspects, so a source code field can be 
        /// represented in the <see cref="Properties" /> collection instead of the <see cref="Fields"/>
        /// collection. 
        /// </summary>
        IPropertyCollection AllProperties { get; }

        /// <summary>
        /// Gets the list of indexers defined in the current type.
        /// </summary>
        IIndexerCollection Indexers { get; }

        /// <summary>
        /// Gets the list of indexers defined in the current type or inherited from the base types.
        /// </summary>
        IIndexerCollection AllIndexers { get; }

        /// <summary>
        /// Gets the list of fields defined in the current type, but not those inherited from the base type.
        /// Note that fields can be promoted to properties by aspects, so a source code field can be 
        /// represented in the <see cref="Properties" /> collection instead of the <see cref="Fields"/>
        /// collection.
        /// </summary>
        IFieldCollection Fields { get; }

        /// <summary>
        /// Gets the list of fields defined in the current type or inherited from the base types.
        /// Note that fields can be promoted to properties by aspects, so a source code field can be 
        /// represented in the <see cref="Properties" /> collection instead of the <see cref="Fields"/>
        /// collection. 
        /// </summary>
        IFieldCollection AllFields { get; }

        /// <summary>
        /// Gets the union of the <see cref="Fields"/> and <see cref="Properties"/> collections.
        /// </summary>
        IFieldOrPropertyCollection FieldsAndProperties { get; }

        /// <summary>
        /// Gets the union of the <see cref="AllFields"/> and <see cref="AllProperties"/> collections.
        /// </summary>
        IFieldOrPropertyCollection AllFieldsAndProperties { get; }

        /// <summary>
        /// Gets the list of events defined in the current type, but not those inherited from the base
        /// types.
        /// </summary>
        IEventCollection Events { get; }

        /// <summary>
        /// Gets the list of events defined in the current type or inherited from the base types.
        /// </summary>
        IEventCollection AllEvents { get; }

        /// <summary>
        /// Gets the list of methods defined in the current type, but not those inherited from the base
        /// type, and not constructors or the finalizer.
        /// </summary>
        IMethodCollection Methods { get; }

        /// <summary>
        /// Gets the list of methods defined in the current type or inherited from the base type.
        /// </summary>
        IMethodCollection AllMethods { get; }

        /// <summary>
        /// Gets the primary constructor if it is defined, otherwise returns <c>null</c>.
        /// </summary>
        /// <remarks>
        /// Primary constructors are recognized only for the current compilation.
        /// </remarks>
        IConstructor? PrimaryConstructor { get; }

        /// <summary>
        /// Gets the list of constructors, including the implicit default constructor if any, but not the static constructor. 
        /// </summary>
        IConstructorCollection Constructors { get; }

        /// <summary>
        /// Gets the static constructor.
        /// </summary>
        IConstructor? StaticConstructor { get; }

        /// <summary>
        /// Gets the finalizer of the type. For value types returns <c>null</c>.
        /// </summary>
        IMethod? Finalizer { get; }

        /// <summary>
        /// Gets a value indicating whether the type is <c>readonly</c>.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets a value indicating whether the type is a <c>ref</c> struct.
        /// </summary>
        bool IsRef { get; }

        /// <summary>
        /// Determines whether the type if subclass of the given class or interface.
        /// </summary>
        bool IsSubclassOf( INamedType type );

        /// <summary>
        /// Finds the the implementation of the given interface member that is valid for this type.
        /// </summary>
        bool TryFindImplementationForInterfaceMember( IMember interfaceMember, [NotNullWhen( true )] out IMember? implementationMember );

        [Obsolete( "Renamed Definition." )]
        INamedType TypeDefinition { get; }

        /// <summary>
        /// Gets the type definition with unassigned type parameters. When the current <see cref="INamedType"/> is not a generic type instance,
        /// returns the current <see cref="INamedType"/>.
        /// </summary>
        new INamedType Definition { get; }

        /// <summary>
        /// Gets the underlying type of an enum, the non-nullable type of a nullable reference type, or the current type.
        /// </summary>
        INamedType UnderlyingType { get; }

        new IRef<INamedType> ToRef();

        new INamedType ToNullable();
        
        // Note that ToNonNullable, when called with Nullable<T>, can return an ITypeParameter and therefore cannot be cast to INamedType.

        INamedType MakeGenericInstance( IReadOnlyList<IType> typeArguments );
    }
}