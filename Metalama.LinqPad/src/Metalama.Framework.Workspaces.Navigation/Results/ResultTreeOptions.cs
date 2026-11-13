// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Workspaces.Navigation.Results;

/// <summary>
/// Limits the size of a result tree.
/// </summary>
/// <param name="MaxDepth">The number of levels of properties expanded below the result.</param>
/// <param name="MaxItems">The number of items shown per collection.</param>
/// <param name="RootDirectory">The directory against which source locations are made relative, or <c>null</c> to keep them absolute.</param>
internal sealed record ResultTreeOptions( int MaxDepth = 1, int MaxItems = 50, string? RootDirectory = null );
