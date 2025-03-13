// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime
{
    /// <summary>
    /// Exposes a method <see cref="TryFindAssembly"/>, which must try to find an assembly that of a given identity.
    /// </summary>
    internal interface IAssemblyLocator : IProjectService
    {
        /// <summary>
        /// Tries to find an assembly of a given identity.
        /// </summary>
        bool TryFindAssembly( AssemblyIdentity assemblyIdentity, [NotNullWhen( true )] out MetadataReference? reference );
    }
}