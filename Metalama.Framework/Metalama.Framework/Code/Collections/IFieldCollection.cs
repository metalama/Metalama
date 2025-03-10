// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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