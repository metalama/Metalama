// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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