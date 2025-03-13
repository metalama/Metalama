// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Services;

public interface ISourceGeneratorDetectionService : IProjectService
{
    /// <summary>
    /// Returns whether the given declaration is a member marked for generation by a source generator and should not be overridden by Metalama.
    /// </summary>
    /// <remarks>This checks that the declaration is a partial member with one of a list of known source generator attributes applied.</remarks>
    bool IsWellKnownGeneratedDeclaration( IMember member );
}