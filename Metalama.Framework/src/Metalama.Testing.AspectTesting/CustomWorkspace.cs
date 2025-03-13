// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Metalama.Testing.AspectTesting;

/// <summary>
/// A Roslyn <see cref="Workspace"/> initialized with a given <see cref="Solution"/>.
/// </summary>
internal sealed class CustomWorkspace : Workspace
{
    public CustomWorkspace( Solution initialSolution ) : base( MefHostServices.DefaultHost, "Custom" )
    {
        this.SetCurrentSolution( initialSolution );
    }

    public override bool CanApplyChange( ApplyChangesKind feature )
    {
        // all kinds supported.
        return true;
    }
}