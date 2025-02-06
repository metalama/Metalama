// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Workspaces;
using System.Linq;

namespace Metalama.LinqPad
{
    /// <summary>
    /// Provides a <see cref="ToDump"/> method, which can be used to format object trees in the way that <see cref="MetalamaWorkspaceDriver"/> does,
    /// but without using <see cref="MetalamaWorkspaceDriver"/>.
    /// </summary>
    [PublicAPI]
    public static class MetalamaDumper
    {
        private static readonly FacadeObjectFactory _facadeObjectFactory = new( GetWorkspaceExpression );

        /// <summary>
        /// Formats object trees in the way that <see cref="MetalamaWorkspaceDriver"/> does but without using <see cref="MetalamaWorkspaceDriver"/>.
        /// </summary>
        public static object? ToDump( object? obj ) => _facadeObjectFactory.GetFacade( obj ) ?? obj;

        /// <summary>
        /// Generates a C# expression that can load the workspace that contains a declaration. We assume the workspace
        /// is a part of the default <see cref="WorkspaceCollection"/>.
        /// </summary>
        private static GetCompilationInfo GetWorkspaceExpression( IDeclaration declaration )
        {
            if ( !WorkspaceCollection.Default.TryFindProject(
                    declaration.Compilation.GetRoslynCompilation(),
                    out var workspace,
                    out _,
                    out var isMetalamaOutput ) )
            {
                return new GetCompilationInfo( "workspace", isMetalamaOutput );
            }

            var workspaceLoadInfo = (IWorkspaceLoadInfo) workspace;
            var arguments = string.Join( ", ", workspaceLoadInfo.LoadedPaths.Select( x => "@\"" + x + "\"" ) );

            // TODO: properties

            return new GetCompilationInfo( $"WorkspaceCollection.Default.Load({arguments})", isMetalamaOutput );
        }
    }
}