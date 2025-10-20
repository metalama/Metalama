// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// Represents a contribution of an aspect or fabric to the fabric. Typically a source of child aspects, options, or validators.
/// </summary>
public interface IPipelineContributor
{
    PipelineContributorKind Kind { get; }
}

public enum PipelineContributorKind
{
    None,
    AspectSource,
    HierarchicalOptionsSource,
    TransitiveAspect,
    
    Extension = 100,
    DiagnosticSource,
    FirstCustomExtension = 2000
    
}

public static class PipelineContributorExtensions
{
    public static IEnumerable<T> OfKind<T>( this IEnumerable<IPipelineContributor> contributors, PipelineContributorKind kind )
        where T : IPipelineContributor
        => contributors.Where( c => c.Kind == kind ).OfType<T>();
    
    public static IEnumerable<T> OfKind<T>( this ImmutableArray<IPipelineContributor> contributors, PipelineContributorKind kind )
        where T : IPipelineContributor
        => contributors.Where( c => c.Kind == kind ).OfType<T>();

    public static IEnumerable<IExtensionPipelineContributor> Extensions( this IEnumerable<IPipelineContributor> contributors )
        => contributors.Where( c => c.Kind >= PipelineContributorKind.Extension ).Cast<IExtensionPipelineContributor>();
    
    public static IEnumerable<IExtensionPipelineContributor> Extensions( this ImmutableArray<IPipelineContributor> contributors )
        => contributors.Where( c => c.Kind >= PipelineContributorKind.Extension ).Cast<IExtensionPipelineContributor>();
}