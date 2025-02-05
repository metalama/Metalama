// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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