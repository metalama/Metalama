// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Services;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Diagnostics;

public sealed class UserDiagnosticRegistry : IProjectService
{
    [PublicAPI]
    public static UserDiagnosticRegistry Empty { get; } = new();

    private readonly HashSet<string> _reportableDiagnosticIds;
    private readonly HashSet<string> _suppressableDiagnosticIds;

    internal UserDiagnosticRegistry( CompileTimeProject project )
    {
        this._reportableDiagnosticIds = project.ClosureDiagnosticManifest.DiagnosticDefinitions.Select( d => d.Id ).ToHashSet();
        this._reportableDiagnosticIds.Add( CodeFixHelper.SuggestionDiagnostic.Id );

        this._suppressableDiagnosticIds = project.ClosureDiagnosticManifest.SuppressionDefinitions.Select( d => d.SuppressedDiagnosticId ).ToHashSet();
    }

    private UserDiagnosticRegistry()
    {
        this._reportableDiagnosticIds = new HashSet<string>();
        this._suppressableDiagnosticIds = new HashSet<string>();
    }

    internal bool IsRegistered( IDiagnosticDefinition definition ) => this._reportableDiagnosticIds.Contains( definition.Id );

    internal bool IsRegistered( SuppressionDefinition definition ) => this._suppressableDiagnosticIds.Contains( definition.SuppressedDiagnosticId );
}