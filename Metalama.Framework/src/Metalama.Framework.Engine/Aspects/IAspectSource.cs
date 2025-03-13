// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Extensibility;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Aspects;

/// <summary>
/// Defines the semantics of an object that can return a set of <see cref="AspectInstance"/>
/// for a given <see cref="IAspectClass"/>.
/// </summary>
internal interface IAspectSource : IPipelineContributor
{
    ImmutableArray<IAspectClass> AspectClasses { get; }

    /// <summary>
    /// Returns a set of <see cref="AspectInstance"/> of a given type. This method is called when the given aspect
    /// type is being processed, not before.
    /// </summary>
    Task CollectAspectInstancesAsync( AspectInstanceCollector collector );
}