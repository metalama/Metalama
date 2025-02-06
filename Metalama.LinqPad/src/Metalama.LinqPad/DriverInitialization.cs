// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using LINQPad;
using Metalama.Backstage.Application;
using Metalama.Backstage.Extensibility;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Workspaces;
using Microsoft.CodeAnalysis.MSBuild;
using System.IO;

namespace Metalama.LinqPad;

internal static class DriverInitialization
{
    private static readonly object _sync = new();
    private static bool _isInitialized;

    public static void Initialize()
    {
        // Reset counters every time. 
        DiagnosticReporter.ClearCounters();

        lock ( _sync )
        {
            if ( _isInitialized )
            {
                return;
            }
            else
            {
                _isInitialized = true;
            }

            if ( !BackstageServiceFactoryInitializer.IsInitialized )
            {
                // Don't enforce licensing in workspaces.

                BackstageServiceFactoryInitializer.Initialize(
                    new BackstageInitializationOptions( new LinqPadApplicationInfo() ) { AddSupportServices = true } );
            }

            DiagnosticReporter.ReportAction = diagnostics => diagnostics.Dump( "Error List" );
        }
    }

    private class LinqPadApplicationInfo : ApplicationInfoBase
    {
        public LinqPadApplicationInfo() : base( typeof(LinqPadApplicationInfo).Assembly ) { }

        public override string Name => "Metalama.LinqPad";
    }
}