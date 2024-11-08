// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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