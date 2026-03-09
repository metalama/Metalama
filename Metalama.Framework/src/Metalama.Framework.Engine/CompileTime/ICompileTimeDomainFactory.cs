// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Services;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CompileTime
{
    /// <summary>
    /// Manages the lifecycle of <see cref="CompileTimeDomain"/> instances. The factory decides whether to reuse
    /// an existing domain or create a new one based on assembly compatibility.
    /// </summary>
    [PublicAPI] // Used in Metalama.Try.
    public interface ICompileTimeDomainFactory : IGlobalService
    {
        /// <summary>
        /// Creates a new, empty <see cref="CompileTimeDomain"/>. Used by test infrastructure
        /// (e.g. <c>TestContext.Domain</c>) when a standalone domain is needed without assembly
        /// compatibility checks. Production code should use <see cref="GetOrCreateDomain"/> instead.
        /// </summary>
        CompileTimeDomain CreateDomain();

        /// <summary>
        /// Gets an existing <see cref="CompileTimeDomain"/> that is compatible with the specified assembly paths,
        /// or creates a new one if no compatible domain exists. A domain is compatible when none of the specified assemblies
        /// would conflict with assemblies already loaded in the domain (i.e. same simple name but different version or public key token).
        /// </summary>
        /// <param name="assemblyPaths">The paths of assemblies that will be loaded into the domain.</param>
        CompileTimeDomain GetOrCreateDomain( IReadOnlyCollection<string> assemblyPaths );
    }
}