// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Runtime.Serialization;

namespace Metalama.Patterns.Caching.Tests.Serializers;

[DataContract]
[Serializable]
internal sealed class MyObject
{
    // ReSharper disable once MemberCanBePrivate.Local
#pragma warning disable SA1401
    public static int NextValue = 10;
#pragma warning restore SA1401

    [DataMember]
    public int Value { get; set; } = NextValue++;
}