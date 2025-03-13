// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Serialization
{
    /// <summary>
    /// Interface that can be implemented by serializable classes.
    /// It defines methods <see cref="OnDeserialized"/> and <see cref="OnSerializing"/> called during serialization.
    /// </summary>
    [CompileTime]
    public interface ICompileTimeSerializationCallback
    {
        /// <summary>
        /// Method called after the object has been deserialized.
        /// </summary>
        void OnDeserialized();

        /// <summary>
        /// Method called before the object is being serialized.
        /// </summary>
        void OnSerializing();
    }
}