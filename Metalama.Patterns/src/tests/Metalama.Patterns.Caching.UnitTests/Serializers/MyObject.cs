// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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