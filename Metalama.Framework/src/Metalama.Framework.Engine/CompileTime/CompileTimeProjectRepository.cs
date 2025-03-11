// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CompileTime;

internal sealed partial class CompileTimeProjectRepository : IProjectService
{
    public CompileTimeProject RootProject { get; }

    // Maps the identity of the run-time project to the compile-time project.
    private readonly Dictionary<AssemblyIdentity, CompileTimeProject?> _projects;

    public bool TryGetCompileTimeProject( AssemblyIdentity assemblyIdentity, out CompileTimeProject? compileTimeProject )
        => this._projects.TryGetValue( assemblyIdentity, out compileTimeProject );

    internal static CompileTimeProjectRepository CreateTestInstance()
        => new( null, default, new Dictionary<AssemblyIdentity, CompileTimeProject?>(), CompileTimeProject.Empty );

    private CompileTimeProjectRepository(
        CompileTimeDomain? domain,
        ProjectServiceProvider serviceProvider,
        Dictionary<AssemblyIdentity, CompileTimeProject?> projects,
        CompileTimeProject rootProject )
    {
        this.RootProject = rootProject;
        this._projects = projects;

        // Register assemblies into the domain.
        if ( domain != null )
        {
            var referenceAssemblyLocator = serviceProvider.GetReferenceAssemblyLocator();
            domain.RegisterAssemblyPaths( referenceAssemblyLocator.AdditionalCompileTimeAssemblyPaths );
        }
    }
}