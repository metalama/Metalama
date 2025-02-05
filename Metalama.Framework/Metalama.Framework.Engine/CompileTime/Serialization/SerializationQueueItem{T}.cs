// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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