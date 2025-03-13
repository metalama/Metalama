// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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