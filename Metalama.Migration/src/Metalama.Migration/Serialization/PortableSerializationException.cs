// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Runtime.Serialization;

namespace PostSharp.Serialization
{
    /// <summary>
    /// No equivalent in Metalama.
    /// </summary>
    [Obsolete( "", true )]
    public class PortableSerializationException : Exception
    {
        public PortableSerializationException() { }

        public PortableSerializationException( string message ) : base( message ) { }

        public PortableSerializationException( string message, Exception inner ) : base( message, inner ) { }

        protected PortableSerializationException(
            SerializationInfo info,
            StreamingContext context ) : base( info, context ) { }
    }
}