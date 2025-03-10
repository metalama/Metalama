// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Framework.Engine.Options;

public static class ProjectOptionsFactoryExtensions
{
    public static IProjectOptions GetProjectOptions(
        this IProjectOptionsFactory factory,
        AnalyzerConfigOptionsProvider options,
        TransformerOptions? transformerOptions = null )
        => factory.GetProjectOptions( options.GlobalOptions, transformerOptions );

    public static IProjectOptions GetProjectOptions(
        this IProjectOptionsFactory factory,
        Microsoft.CodeAnalysis.Project project,
        TransformerOptions? transformerOptions = null )
        => factory.GetProjectOptions( project.AnalyzerOptions.AnalyzerConfigOptionsProvider, transformerOptions );
}