// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Runtime.Serialization;

namespace Metalama.Testing.AspectTesting;

[Serializable]
public sealed class InvalidTestTargetException : Exception
{
    public InvalidTestTargetException( string message ) : base( message ) { }

    private InvalidTestTargetException( SerializationInfo info, StreamingContext context )
        : base( info, context ) { }
}