// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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