// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents an assembly identity, used in project references.
    /// </summary>
    [InternalImplement]
    [CompileTime]
    public interface IAssemblyIdentity : IEquatable<IAssemblyIdentity>
    {
        /// <summary>
        /// Gets the assembly name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the assembly version.
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// Gets the assembly culture, or an empty string if the culture is neutral.
        /// </summary>
        string CultureName { get; }

        /// <summary>
        /// Gets the full public key, or an empty array.
        /// </summary>
        ImmutableArray<byte> PublicKey { get; }

        /// <summary>
        /// Gets the public key token, or an empty array.
        /// </summary>
        ImmutableArray<byte> PublicKeyToken { get; }

        /// <summary>
        /// Gets a value indicating whether the assembly has either a <see cref="PublicKey"/> or a <see cref="PublicKeyToken"/>.
        /// </summary>
        bool IsStrongNamed { get; }

        /// <summary>
        /// Gets a value indicating whether the assembly has a full <see cref="PublicKey"/>.
        /// </summary>
        bool HasPublicKey { get; }
    }
}