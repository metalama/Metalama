// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Options;
using Metalama.Framework.Services;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Extensibility;

public interface IExtensionLoader : IGlobalService
{
    IEnumerable<ExportExtensionAttribute> GetExtensionTypes(
        IProjectOptions projectOptions,
        CompileTimeDomain domain,
        ExtensionKinds extensionKinds,
        IDiagnosticAdder diagnosticAdder );

    /// <summary>
    /// Gets the resolved file paths for extension assemblies from the given assembly references.
    /// </summary>
    IEnumerable<string> GetExtensionAssemblyPaths( IEnumerable<TargetedAssemblyReference> assemblyReferences );
}