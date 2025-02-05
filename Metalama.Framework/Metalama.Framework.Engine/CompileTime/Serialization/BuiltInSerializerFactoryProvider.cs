// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CompileTime.Serialization.Serializers;
using Metalama.Framework.Engine.Services;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;

namespace Metalama.Framework.Engine.CompileTime.Serialization;

internal sealed class BuiltInSerializerFactoryProvider : SerializerFactoryProvider
{
    public BuiltInSerializerFactoryProvider( in ProjectServiceProvider serviceProvider )
        : base( serviceProvider, new ReflectionSerializationProvider( serviceProvider ) )
    {
        // intrinsic types
        this.AddSerializer<bool, BooleanSerializer>();
        this.AddSerializer<byte, ByteSerializer>();
        this.AddSerializer<char, CharSerializer>();
        this.AddSerializer<short, Int16Serializer>();
        this.AddSerializer<int, Int32Serializer>();
        this.AddSerializer<long, Int64Serializer>();
        this.AddSerializer<ushort, UInt16Serializer>();
        this.AddSerializer<uint, UInt32Serializer>();
        this.AddSerializer<ulong, UInt64Serializer>();
        this.AddSerializer<sbyte, SByteSerializer>();
        this.AddSerializer<float, SingleSerializer>();
        this.AddSerializer<decimal, DecimalSerializer>();
        this.AddSerializer<string, StringSerializer>();
        this.AddSerializer<double, DoubleSerializer>();

        this.AddSerializer<DateTime, DateTimeSerializer>();
        this.AddSerializer<TimeSpan, TimeSpanSerializer>();
        this.AddSerializer<DottedString, DottedStringSerializer>();
        this.AddSerializer<Guid, GuidSerializer>();
        this.AddSerializer<CultureInfo, CultureInfoSerializer>();

        // collections
        this.AddSerializer( typeof(List<>), typeof(ListSerializer<>) );
        this.AddSerializer( typeof(Dictionary<,>), typeof(DictionarySerializer<,>) );
        this.AddSerializer( typeof(ImmutableDictionary<,>), typeof(ImmutableDictionarySerializer<,>) );
        this.AddSerializer( typeof(ImmutableArray<>), typeof(ImmutableArraySerializer<>) );
        this.AddSerializer( typeof(ImmutableHashSet<>), typeof(ImmutableHashSetSerializer<>) );

        // Our own types.
        this.AddSerializer( typeof(SymbolRef<>), typeof(RefSerializer<>) );
        this.AddSerializer( typeof(SyntaxRef<>), typeof(RefSerializer<>) );
        this.AddSerializer( typeof(DeclarationIdRef<>), typeof(RefSerializer<>) );
        this.AddSerializer( typeof(TypeIdRef<>), typeof(TypeIdRefSerializer<>) );
        this.AddSerializer( typeof(SymbolIdRef<>), typeof(RefSerializer<>) );
        this.AddSerializer( typeof(IntroducedRef<>), typeof(RefSerializer<>) );
        this.AddSerializer<SerializableDeclarationId, SerializableDeclarationIdSerializer>();
        this.AddSerializer<SerializableTypeId, SerializableTypeIdSerializer>();
        this.AddSerializer<SymbolAttributeRef, AttributeRefSerializer>();
        this.AddSerializer<DeserializedAttributeRef, AttributeRefSerializer>();
        this.AddSerializer<SyntaxAttributeRef, AttributeRefSerializer>();
        this.AddSerializer<IntroducedAttributeRef, AttributeRefSerializer>();
        this.AddSerializer<TypedConstantRef, TypedConstantRefSerializer>();
        this.AddSerializer<AttributeSerializationData, AttributeSerializationDataSerializer>();

        // TODO: Backward-compatible deserializers for references.

        this.MakeReadOnly();
    }
}