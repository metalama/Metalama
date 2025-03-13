// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Testing.AspectTesting;

/// <summary>
/// Represents the properties read from assembly metadata and set from the MSBuild project.
/// </summary>
internal sealed record TestAssemblyMetadata(
    string ProjectDirectory,
    string SourceDirectory,
    ImmutableArray<string> ParserSymbols,
    string TargetFramework,
    bool MustLaunchDebugger,
    ImmutableArray<TestAssemblyReference> AssemblyReferences,
    ImmutableArray<TestAssemblyReference> CompileTimeAssemblyReferences,
    ImmutableArray<TestAssemblyReference> ExtensionReferences,
    ImmutableArray<string> PlugInTypes,
    string? GlobalUsingsFile,
    ImmutableArray<string> IgnoredWarnings )
{
    public TestProjectReferences ToProjectReferences()
        => new(
            [..this.AssemblyReferences.Select( x => x.ToMetadataReference()! )],
            this.ExtensionReferences,
            this.CompileTimeAssemblyReferences,
            this.PlugInTypes,
            this.GlobalUsingsFile );
}