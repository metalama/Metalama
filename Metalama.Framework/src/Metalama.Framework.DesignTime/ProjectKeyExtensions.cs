// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.DesignTime;

internal static class ProjectKeyExtensions
{
    public static ProjectKey GetProjectKey( this Compilation compilation ) => ProjectKeyFactory.FromCompilation( compilation );

    /// <inheritdoc cref="ProjectKeyFactory.TryFromCompilation"/>
    public static bool TryGetProjectKey( this Compilation compilation, [NotNullWhen( true )] out ProjectKey? projectKey )
        => ProjectKeyFactory.TryFromCompilation( compilation, out projectKey );
}