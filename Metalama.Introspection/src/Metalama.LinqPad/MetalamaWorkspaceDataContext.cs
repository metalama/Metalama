// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using LINQPad;
using Metalama.Framework.Workspaces;
using System;
using System.Runtime.InteropServices;

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

            try
            {
                this.workspace = WorkspaceCollection.Default.Load( path );

                if ( reportWorkspaceErrors )
                {
                    this.workspace.WorkspaceDiagnostics.Dump( "Loading Issues" );
                }
            }
            catch ( AggregateException e ) when ( e.InnerException is MSBuildInitializationException initializationException )
            {
                if ( initializationException.HasArchitectureMismatch )
                {
                    throw new MSBuildInitializationException(
                        $"LINQPad was launched for a process architecture ({RuntimeInformation.ProcessArchitecture}) for which no suitable .NET SDK is installed. "
                        + "Try running LINQPad with a different processor architecture." );
                }
                else
                {
                    throw;
                }
            }
        }
    }
}