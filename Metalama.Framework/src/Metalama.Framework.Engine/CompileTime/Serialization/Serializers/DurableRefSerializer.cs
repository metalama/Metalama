// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers;

/// <summary>
/// Serializes a <see cref="BoundDurableRef{T}"/>, which stores the reference it was created from instead of an
/// identifier.
/// </summary>
/// <remarks>
/// <para>
/// Retention and serialization are two distinct requirements, and only serialization requires an identifier. This
/// serializer computes that identifier. A batch compilation therefore computes an identifier only for the references
/// that are serialized, instead of for every reference that is made durable.
/// </para>
/// <para>
/// Deserialization always produces an identifier-based reference, because a reference is read in a compilation other
/// than the one that wrote it, and there is therefore no reference to store. The class is selected according to the
/// identifier, so that a type retains its type arguments and its nullable annotation.
/// <see cref="RefSerializer{T}"/> cannot be used here, because it always creates a <c>DeclarationIdRef</c>.
/// </para>
/// </remarks>
internal sealed class DurableRefSerializer<T> : ReferenceTypeSerializer<BaseRef<T>>
    where T : class, ICompilationElement
{
    public override BaseRef<T> CreateInstance( IArgumentsReader constructorArguments )
    {
        var id = constructorArguments.GetValue<string>( "id" ).AssertNotNull();

        return SerializableTypeId.IsTypeId( id )
            ? new TypeIdRef<T>( new SerializableTypeId( id ) )
            : new DeclarationIdRef<T>( new SerializableDeclarationId( id ) );
    }

    public override void SerializeObject( BaseRef<T> obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments )
    {
        constructorArguments.SetValue( "id", ((IDurableRefImpl) obj).Id );
    }
}
