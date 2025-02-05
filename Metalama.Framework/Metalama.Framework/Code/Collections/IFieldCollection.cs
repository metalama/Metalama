// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Code.Collections
{
    /// <summary>
    /// Represents a list of fields.
    /// </summary>
    /// <remarks>
    ///  <para>The order of items in this list is undetermined and may change between versions.</para>
    /// </remarks>
    public interface IFieldCollection : IMemberCollection<IField>
    {
        /// <summary>
        /// Gets a field of a given name or throws an exception if there is none.
        /// </summary>
        IField this[ string name ] { get; }
    }
}