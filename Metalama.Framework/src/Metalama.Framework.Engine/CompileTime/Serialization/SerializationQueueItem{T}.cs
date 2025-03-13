// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.CompileTime.Serialization
{
    internal sealed class SerializationQueueItem<T>
    {
        public SerializationQueueItem( T? o, SerializationCause? cause )
        {
            this.Value = o;
            this.Cause = cause;
        }

        public T? Value { get; }

        public SerializationCause? Cause { get; }
    }
}