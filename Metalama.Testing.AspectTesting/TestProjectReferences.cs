// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Testing.AspectTesting;

internal sealed record TestProjectReferences(
    ImmutableArray<PortableExecutableReference> MetadataReferences,
    ImmutableArray<TestAssemblyReference> ExtensionReferences,
    ImmutableArray<TestAssemblyReference> CompileTimeAssemblyReferences,
    ImmutableArray<string> PlugInTypes,
    string? GlobalUsingsFile );