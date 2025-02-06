// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Serializers;

namespace Metalama.Patterns.Caching.Tests.Serializers;

[UsedImplicitly]
public sealed class BinarySerializerTests : SerializerBaseTests
{
#pragma warning disable CS0618 // Type or member is obsolete
    public BinarySerializerTests() : base( new BinaryCachingSerializer() ) { }
#pragma warning restore CS0618 // Type or member is obsolete
}