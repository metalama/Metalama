// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Engine.CompileTime.Serialization.Serializers;

/// <summary>
/// Serializes a <see cref="LiveDurableRef{T}"/>, which holds the reference it was made from rather than an identifier.
/// </summary>
/// <remarks>
/// <para>
/// Being durable for retention and being durable for serialization are two different requirements, and only the second
/// one needs an identifier. This serializer is where that identifier is produced: writing it here rather than when the
/// reference was made is exactly what a batch compilation gains, because the identifier is then computed once, for the
/// references that are actually written, instead of for every reference that is made durable.
/// </para>
/// <para>
/// The deserialized reference is identifier-based whatever was written, which is the only correct answer: a reference
/// is read in a compilation other than the one that wrote it, so there is nothing live for it to hold. The kind is
/// chosen from the identifier, so that a type keeps its type arguments and its nullable annotation. Reusing
/// <see cref="RefSerializer{T}"/> would lose that, because it always builds a <c>DeclarationIdRef</c>.
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
