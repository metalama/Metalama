// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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