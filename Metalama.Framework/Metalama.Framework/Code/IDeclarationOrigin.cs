// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Code;

/// <summary>
/// Represents the origin of the code, i.e. the artefact or function created the declaration.
/// </summary>
/// <seealso cref="IAspectDeclarationOrigin"/>
[CompileTime]
public interface IDeclarationOrigin
{
    /// <summary>
    /// Gets the kind of origin.
    /// </summary>
    DeclarationOriginKind Kind { get; }

    /// <summary>
    /// Gets a value indicating whether the declaration or its parent has an <see cref="CompilerGeneratedAttribute"/> regardless of the origin <see cref="Kind"/>.
    /// This property is currently <c>false</c> for declarations introduced by aspects because Metalama does not add the <see cref="CompilerGeneratedAttribute"/>
    /// to introduced declarations. Check the <see cref="Kind"/> property to skip declarations added by aspects.
    /// </summary>
    bool IsCompilerGenerated { get; }
}