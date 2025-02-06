// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using PostSharp.Engineering.BuildTools;
using PostSharp.Engineering.BuildTools.Build;
using PostSharp.Engineering.BuildTools.Build.Model;
using PostSharp.Engineering.BuildTools.Build.Solutions;
using PostSharp.Engineering.BuildTools.Dependencies.Definitions;
using PostSharp.Engineering.BuildTools.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MetalamaDependencies = PostSharp.Engineering.BuildTools.Dependencies.Definitions.MetalamaDependencies.V2025_1;

var product = new Product( MetalamaDependencies.MetalamaLinqPad )
{
    Solutions = [new DumpCapturingSolution( "Metalama.LinqPad.sln" ) { CanFormatCode = true }],
    PublicArtifacts = Pattern.Create( "Metalama.LinqPad.$(PackageVersion).nupkg" ),
    Dependencies = [DevelopmentDependencies.PostSharpEngineering, MetalamaDependencies.Metalama],
    MainVersionDependency = MetalamaDependencies.Metalama,

    // This is set temporarily to investigate hanging tests.
    // After removing, don't forget to run `b generate-scripts`. 
    BuildTimeOutThreshold = TimeSpan.FromMinutes( 25 ),
    Configurations = Product.DefaultConfigurations
        .WithValue(
            BuildConfiguration.Debug,
            c => c with
            {
                AdditionalArtifactRules = new[]
                {
                    $@"+:%system.teamcity.build.tempDir%/Metalama/EngineeringDumps/engdumps.tar.gz",
                }
            } )
};

return new EngineeringApp( product ).Run( args );

// TODO: This is a prototype that should go to the main engineering later or be removed.
#pragma warning disable CA1001
public class DumpCapturingSolution : DotNetSolution
{
    private SemaphoreSlim _semaphore = new( 1 );

    public DumpCapturingSolution( string solutionPath ) : base( solutionPath )
    {
    }

    public override bool Test( BuildContext context, BuildSettings settings )
    {
        var cancellationSource = new CancellationTokenSource();
        _ = Task.Run( async () => await this.MonitorTimeout( context, cancellationSource.Token ) );

        try
        {
            return base.Test( context, settings );
        }
        finally
        {
            // Wait for some time before zipping is finished.
            try
            {
                this._semaphore.Wait(30_000);
                cancellationSource.Cancel();
            }
            finally
            {
                this._semaphore.Release();
            }
        }
    }

    private async Task MonitorTimeout( BuildContext context, CancellationToken token )
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        try
        {
            await this._semaphore.WaitAsync( token );

            // Wait for 15 minutes (if the timeout is not met, this gets cancelled).
            await Task.Delay( 900_000, token );

            // Dump all child processes.

            var directory = $"{Path.GetTempPath()}\\Metalama\\EngineeringDumps";

            Directory.CreateDirectory( directory );

            var processesToCapture = new List<ManagementObject>();

            var mos = new ManagementObjectSearcher( $"Select * From Win32_Process Where ParentProcessID={Environment.ProcessId}" );

            foreach ( var o in mos.Get() )
            {
                var mo = (ManagementObject)o;
                ProcessChildProcesses( mo );
            }

            var dumpFiles = new List<string>();

            foreach ( var processToCapture in processesToCapture )
            {
                var process = Process.GetProcessById( Convert.ToInt32( processToCapture["ProcessID"], CultureInfo.InvariantCulture ) );

                var dumpFileName = $"{directory}\\Dump_{process.Id}_{Path.GetFileName( processToCapture["ExecutablePath"].ToString() )}.dmp";

                context.Console.WriteImportantMessage( $"Capturing dump of '{processToCapture["ExecutablePath"]}:{process.Id}' into '{dumpFileName}'. Command line: {processToCapture["CommandLine"]}" );

                if ( !ToolInvocationHelper.InvokeTool(
                    context.Console,
                    "dotnet",
                    $"dump collect -p {process.Id} -o \"{dumpFileName}\" --type Full",
                    null ) )
                {
                    context.Console.WriteImportantMessage( "Failed." );
                }
                else
                {
                    dumpFiles.Add( dumpFileName );
                    context.Console.WriteImportantMessage( "Success." );
                }
            }

            var targzFilename = $"{directory}\\engdumps.tar.gz";

            context.Console.WriteImportantMessage( $"Will create archive {targzFilename}." );

            using ( var outStream = File.Create( targzFilename ) )
            using ( var gzoStream = new GZipOutputStream( outStream ) )
            using ( var tarArchive = TarArchive.CreateOutputTarArchive( gzoStream ) )
            {
                tarArchive.RootPath = Path.GetDirectoryName( directory );

                foreach ( var dumpFile in dumpFiles )
                {
                    context.Console.WriteImportantMessage( $"Added { dumpFile}." );
                    var tarEntry = TarEntry.CreateEntryFromFile( dumpFile );
                    tarEntry.Name = Path.GetFileName( dumpFile );

                    tarArchive.WriteEntry( tarEntry, true );

                    File.Delete( dumpFile );
                }
            }

            void ProcessChildProcesses( ManagementObject processObject )
            {
                if ( FilterProcess( processObject ) )
                {
                    context.Console.WriteImportantMessage( $"Will capture dump of '{processObject["ExecutablePath"]}:{processObject["ProcessId"]}'. Command line: {processObject["CommandLine"]}" );
                    processesToCapture.Add( processObject );
                }
                else
                {
                    context.Console.WriteImportantMessage( $"Skipping dump of '{processObject["ExecutablePath"]}:{processObject["ProcessId"]}'. Command line: {processObject["CommandLine"]}" );
                }

                var cmos = new ManagementObjectSearcher( $"Select * From Win32_Process Where ParentProcessID={processObject["ProcessId"]}" );

                foreach ( var o in cmos.Get() )
                {
                    var cmo = (ManagementObject)o;
                    ProcessChildProcesses( cmo );
                }
            }
        }
        finally
        {
            this._semaphore.Release();
        }
    }

    private static bool FilterProcess( ManagementObject processObject )
    {
        if ( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            return false;
        }

        return processObject["ExecutablePath"].ToString().Contains( "dotnet", StringComparison.OrdinalIgnoreCase );
    }
}