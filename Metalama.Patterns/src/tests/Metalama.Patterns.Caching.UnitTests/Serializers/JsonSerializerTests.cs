// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Serializers;

namespace Metalama.Patterns.Caching.Tests.Serializers;

[UsedImplicitly]
public sealed class JsonSerializerTests : SerializerBaseTests
{
    public JsonSerializerTests() : base( new JsonCachingSerializer() ) { }
}