// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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