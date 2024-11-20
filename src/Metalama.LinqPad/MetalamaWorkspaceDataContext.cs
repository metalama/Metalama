// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using LINQPad;
using Metalama.Framework.Workspaces;

namespace Metalama.LinqPad
{
    /// <summary>
    /// The base class for all queries created with <see cref="MetalamaWorkspaceDriver"/>.
    /// </summary>
    [PublicAPI]
    public abstract class MetalamaWorkspaceDataContext
    {
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once MemberCanBePrivate.Global
#pragma warning disable SA1401, IDE1006
        protected readonly Workspace workspace;
#pragma warning restore SA1401, IDE1006

        protected MetalamaWorkspaceDataContext( string path, bool reportWorkspaceErrors )
        {
            DriverInitialization.Initialize();

            this.workspace = WorkspaceCollection.Default.Load( path );

            if ( reportWorkspaceErrors )
            {
                this.workspace.WorkspaceDiagnostics.Dump( "Loading Issues" );
            }
        }
    }
}