// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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