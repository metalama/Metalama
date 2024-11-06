// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Services;
using System.Collections.Immutable;

namespace Metalama.Framework.Options;

/// <summary>
/// Base interface for <c>IProjectOptions</c> in Engine.
/// </summary>
public interface IBaseProjectOptions : IProjectService
{
    /// <summary>
    /// Gets names of attribute types that are considered to be affected by source generators.
    /// Partial members marked with these attributes are not eligible for overriding by Metalama aspects.
    /// </summary>
    ImmutableArray<string> SourceGeneratorAttributes { get; }
}