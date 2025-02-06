// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using LINQPad.Extensibility.DataContext;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Workspaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Metalama.LinqPad
{
    /// <summary>
    /// A LinqPad driver that lets you query Metalama workspaces.
    /// </summary>
    [UsedImplicitly]
    public sealed class MetalamaWorkspaceDriver : MetalamaScratchpadDriver
    {
        public override string Name => "Metalama Workspace";

        public override string GetConnectionDescription( IConnectionInfo cxInfo )
        {
            // For static drivers, we can use the description of the custom type & its assembly:
            var connectionData = new ConnectionData( cxInfo );

            return connectionData.DisplayName;
        }

        public override bool ShowConnectionDialog( IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions )
        {
            Initialize();

            // Prompt the user for a custom assembly and type name:
            var dialog = new ConnectionDialog( cxInfo );

            return dialog.ShowDialog() == true;
        }

        public override bool AreRepositoriesEquivalent( IConnectionInfo c1, IConnectionInfo c2 )
        {
            var data1 = new ConnectionData( c1 );
            var data2 = new ConnectionData( c2 );

            if ( data1.Project == data2.Project )
            {
                return true;
            }

            if ( data1.Project == null || data2.Project == null )
            {
                return false;
            }

            return string.Equals( Path.GetFullPath( data1.Project ), Path.GetFullPath( data2.Project ), StringComparison.OrdinalIgnoreCase );
        }

        public override List<ExplorerItem> GetSchemaAndBuildAssembly(
            IConnectionInfo cxInfo,
            AssemblyName assemblyToBuild,
            ref string nameSpace,
            ref string typeName )
        {
            Initialize();

            try
            {
                var connectionData = new ConnectionData( cxInfo );

                var escapedPath = connectionData.Project.ReplaceOrdinal( "\"", "\"\"" );
                var reportLoadErrors = connectionData.ReportWorkspaceErrors ? "true" : "false";

                var source = $@"
using System;
using System.Collections.Generic;
using Metalama.LinqPad;
using Metalama.Framework.Workspaces;

namespace {nameSpace}
{{
    // The main typed data class. The user's queries subclass this, so they have easy access to all its members.
	public class {typeName} : {nameof(MetalamaWorkspaceDataContext)}
	{{
	    public {typeName}() : base( @""{escapedPath}"", {reportLoadErrors} )
		{{
		}}        
	}}	
}}";

#pragma warning disable SYSLIB0044
                Compile( source, assemblyToBuild.CodeBase!, cxInfo );
#pragma warning restore SYSLIB0044

                // We intentinally do NOT load the workspace because the object explorer process does not copy
                // content assemblies, and the hack of copying them to the lib directory is not reliable.
                var schemaFactory = new SchemaFactory( FormatTypeName );
                var projectSchema = schemaFactory.GetSchema( "workspace" );

                return projectSchema;
            }
            catch ( Exception e )
            {
                Logger?.Error?.Log( e.ToString() );

                throw;
            }
        }
    }
}