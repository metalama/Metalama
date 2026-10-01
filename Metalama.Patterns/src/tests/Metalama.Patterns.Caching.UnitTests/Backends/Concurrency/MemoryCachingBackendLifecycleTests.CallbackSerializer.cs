// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Serializers;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency
{
    public sealed partial class MemoryCachingBackendLifecycleTests
    {
        /// <summary>
        /// An <see cref="ICachingSerializer"/> that stores string values and invokes a callback before it serializes each
        /// value.
        /// </summary>
        /// <remarks>
        /// The backend calls <see cref="Serialize"/> while it stores an item, so the callback runs at the place where the
        /// serializer of an application runs.
        /// </remarks>
        private sealed class CallbackSerializer : ICachingSerializer
        {
            private readonly Action<object?> _onSerialize;

            /// <summary>
            /// Initializes a new instance of the <see cref="CallbackSerializer"/> class.
            /// </summary>
            /// <param name="onSerialize">The callback that receives each value before the value is serialized.</param>
            public CallbackSerializer( Action<object?> onSerialize )
            {
                this._onSerialize = onSerialize;
            }

            /// <summary>
            /// Invokes the callback, then writes the value as a string.
            /// </summary>
            /// <param name="value">The value to serialize. The tests store only string values.</param>
            /// <param name="writer">The writer that receives the serialized value.</param>
            public void Serialize( object? value, BinaryWriter writer )
            {
                this._onSerialize( value );
                writer.Write( value as string ?? string.Empty );
            }

            /// <summary>
            /// Reads a value written by <see cref="Serialize"/>.
            /// </summary>
            /// <param name="reader">The reader that provides the serialized value.</param>
            /// <returns>The string value.</returns>
            public object? Deserialize( BinaryReader reader ) => reader.ReadString();
        }
    }
}
