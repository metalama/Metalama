// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Runtime.Serialization;

namespace Metalama.Testing.AspectTesting;

[Serializable]
public sealed class InvalidTestOptionException : Exception
{
    public InvalidTestOptionException( string message ) : base( message ) { }

    private InvalidTestOptionException( SerializationInfo info, StreamingContext context )
        : base( info, context ) { }
}