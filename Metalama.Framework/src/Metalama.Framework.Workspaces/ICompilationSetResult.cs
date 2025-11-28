// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Introspection;

namespace Metalama.Framework.Workspaces;

/// <summary>
/// Represents the result of compiling a set of compilations, providing access to both introspection details
/// and the transformed code produced by Metalama.
/// </summary>
/// <seealso cref="ICompilationSet"/>
/// <seealso cref="IIntrospectionCompilationDetails"/>
/// <seealso href="@introspection-api"/>
public interface ICompilationSetResult : IIntrospectionCompilationDetails
{
    /// <summary>
    /// Gets the transformed code produced by Metalama for the compilations in this set.
    /// </summary>
    /// <seealso cref="IProjectSet.SourceCode"/>
    ICompilationSet TransformedCode { get; }
}