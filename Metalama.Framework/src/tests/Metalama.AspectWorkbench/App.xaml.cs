// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Extensibility;
using Metalama.Framework.Engine.Pipeline.CompileTime;

namespace Metalama.AspectWorkbench
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    internal sealed partial class App
    {
        public App()
        {
            BackstageServiceFactory.Initialize( new BackstageInitializationOptions( new MyApplicationInfo() ) { AddLicensing = true, AddSupportServices = true }, "AspectWorkbench" );
        }

        private sealed class MyApplicationInfo : ApplicationInfoBase
        {
            public MyApplicationInfo() : base( typeof(CompileTimeAspectPipeline).Assembly ) { }

            public override string Name => "Metalama.AspectWorkbench";

            public override bool IsTelemetryEnabled => false;

            public override bool IsLongRunningProcess => true;

            public override bool ShouldCreateLocalCrashReports => false;
        }
    }
}