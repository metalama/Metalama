// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.Diagnostics;

public sealed class UserDiagnosticRegistry : IProjectService
{
    [PublicAPI]
    public static UserDiagnosticRegistry Empty { get; } = new();

    private readonly DiagnosticManifest _combinedManifest;

    internal UserDiagnosticRegistry( CompileTimeProject project, DiagnosticManifest? extensionDiagnostics = null )
    {
        this._combinedManifest = project.ClosureDiagnosticManifest.Add( CodeFixHelper.SuggestionDiagnostic );

        if ( extensionDiagnostics != null )
        {
            this._combinedManifest = this._combinedManifest.Union( extensionDiagnostics );
        }
    }

    private UserDiagnosticRegistry()
    {
        this._combinedManifest = DiagnosticManifest.Empty;
    }

    internal bool IsRegistered( IDiagnosticDefinition definition ) => this._combinedManifest.DiagnosticDefinitions.ContainsKey( definition.Id );

    internal bool IsRegistered( SuppressionDefinition definition )
        => this._combinedManifest.SuppressionDefinitions.ContainsKey( definition.SuppressedDiagnosticId );
}