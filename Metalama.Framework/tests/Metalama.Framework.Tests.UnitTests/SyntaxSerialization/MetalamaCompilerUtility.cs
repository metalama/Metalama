// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Utilities;
using Metalama.Framework.Engine.Pipeline;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Diagnostics;
using System.IO;

#if NET5_0_OR_GREATER
using System.Threading.Tasks;
#endif

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization
{
    internal static class MetalamaCompilerUtility
    {
        public static string CompileAssembly( GlobalServiceProvider serviceProvider, string baseDirectory, params string[] sourceFiles )
        {
            var dir = Path.Combine( baseDirectory, "CompileAssembly", Guid.NewGuid().ToString() );
            Directory.CreateDirectory( dir );
            var projectName = $"test-{Guid.NewGuid()}";

            void WriteFile( string name, string text ) => File.WriteAllText( Path.Combine( dir, name ), text );

            GlobalJsonHelper.WriteCurrentVersion( dir, serviceProvider.GetRequiredBackstageService<IPlatformInfo>() );

            var metadataReader = AssemblyMetadataReader.GetInstance( typeof(AspectPipeline).Assembly );

            var csproj = $@"
<Project Sdk='Microsoft.NET.Sdk'>
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include='Metalama.Compiler.Sdk' Version='{metadataReader.GetPackageVersion( "Metalama.Compiler.Sdk" )}' />
    <PackageReference Include='Metalama.Compiler' Version='{metadataReader.GetPackageVersion( "Metalama.Compiler" )}' />
  </ItemGroup>
</Project>
";

            WriteFile( $"{projectName}.csproj", csproj );

            for ( var i = 0; i < sourceFiles.Length; i++ )
            {
                WriteFile( $"file{i}.cs", sourceFiles[i] );
            }

            var psi = new ProcessStartInfo( "dotnet", "build" ) { WorkingDirectory = dir, RedirectStandardOutput = true, UseShellExecute = false };
            var process = Process.Start( psi )!;
            var outputPromise = process.StandardOutput.ReadToEndAsync();

#pragma warning disable VSTHRD002

#if NET5_0_OR_GREATER
            var completion = process.WaitForExitAsync();
            Task.WhenAll( completion, outputPromise ).Wait();
#else
            process.WaitForExit();
            outputPromise.Wait();
#endif

            if ( process.ExitCode != 0 )
            {
                throw new InvalidOperationException( outputPromise.Result );
            }

#pragma warning restore VSTHRD002

            return Path.Combine( dir, $"bin/Debug/net48/{projectName}.dll" );
        }
    }
}