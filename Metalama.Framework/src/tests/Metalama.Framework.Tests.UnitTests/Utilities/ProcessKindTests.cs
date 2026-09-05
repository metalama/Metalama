// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using BackstageProcessKind = Metalama.Backstage.Diagnostics.ProcessKind;
using BackstageProcessKindDetector = Metalama.Backstage.Diagnostics.ProcessKindDetector;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// Tests the classification of the host process by its name.
/// </summary>
/// <remarks>
/// <para>
/// The classification is compiled into <c>Metalama.Backstage</c> and into
/// <c>Metalama.Framework.CompilerExtensions</c> from a single source file, because the second assembly can
/// reference nothing: it embeds and extracts the first one.
/// </para>
/// <para>
/// This test reads both assemblies as they are built, and not a copy of the shared source file compiled into the
/// test project. It reaches <c>Metalama.Backstage</c> through its package reference, and it loads
/// <c>Metalama.Framework.CompilerExtensions.dll</c> from the output directory of that project, whose path the
/// project file of this test passes as assembly metadata. A build that stops compiling the shared file into either
/// assembly, or that adds a second copy of the classification to one of them, therefore fails these tests.
/// </para>
/// <para>
/// The classification takes the process name and the command line as parameters, so every arm of the table is
/// exercised here without the corresponding process existing.
/// </para>
/// </remarks>
public sealed class ProcessKindTests
{
    /// <summary>
    /// The name of the assembly metadata item that carries the path of <c>Metalama.Framework.CompilerExtensions.dll</c>.
    /// </summary>
    private const string _compilerExtensionsAssemblyPathKey = "CompilerExtensionsAssemblyPath";

    /// <summary>
    /// The namespace in which <c>Metalama.Framework.CompilerExtensions</c> declares the shared types.
    /// </summary>
    private const string _compilerExtensionsNamespace = "Metalama.Framework.CompilerExtensions";

    /// <summary>
    /// The assembly <c>Metalama.Framework.CompilerExtensions.dll</c>, loaded once for the whole test class.
    /// </summary>
    private static readonly Lazy<Assembly> _compilerExtensionsAssembly = new( LoadCompilerExtensionsAssembly );

    /// <summary>
    /// Loads <c>Metalama.Framework.CompilerExtensions.dll</c> from the output directory of that project.
    /// </summary>
    /// <remarks>
    /// The assembly is an analyzer assembly that no project can reference, so it is loaded by path instead. The
    /// path is passed by the project file of this test, because the assembly is not copied to the output directory
    /// of this test: a reference to it would declare, in this test assembly, types of the same full name as the
    /// ones that are being read.
    /// </remarks>
    private static Assembly LoadCompilerExtensionsAssembly()
    {
        var relativePath = typeof(ProcessKindTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single( a => a.Key == _compilerExtensionsAssemblyPathKey )
            .Value;

        Assert.NotNull( relativePath );

        var path = Path.GetFullPath( relativePath! );

        Assert.True(
            File.Exists( path ),
            $"'{path}' does not exist. Build Metalama.Framework.CompilerExtensions, for instance by running 'Build.ps1 build', before running this test." );

        return Assembly.LoadFrom( path );
    }

    /// <summary>
    /// Gets a type of <c>Metalama.Framework.CompilerExtensions</c> by its name.
    /// </summary>
    /// <param name="typeName">The name of the type, without its namespace.</param>
    private static Type GetCompilerExtensionsType( string typeName )
        => _compilerExtensionsAssembly.Value.GetType( $"{_compilerExtensionsNamespace}.{typeName}", true )!;

    /// <summary>
    /// Classifies a process through <c>Metalama.Framework.CompilerExtensions</c>, and returns the name of the kind
    /// that it returns. The name is what the two assemblies are compared on, because the enumeration of one of them
    /// is not the enumeration of the other.
    /// </summary>
    /// <param name="processName">The process name, as <c>Process.ProcessName</c> gives it.</param>
    /// <param name="commandLine">The command line, as <c>Environment.CommandLine</c> gives it.</param>
    private static string GetCompilerExtensionsProcessKindName( string processName, string commandLine )
    {
        var method = GetCompilerExtensionsType( "ProcessKindDetector" )
            .GetMethod(
                nameof(BackstageProcessKindDetector.GetProcessKind),
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(string), typeof(string)],
                null );

        Assert.NotNull( method );

        var processKind = method!.Invoke( null, [processName, commandLine] );

        Assert.NotNull( processKind );

        return processKind!.ToString()!;
    }

    /// <summary>
    /// The process name and the command line of one process, and the kind that the table must classify it as.
    /// </summary>
    /// <param name="ProcessName">The process name, as <c>Process.ProcessName</c> gives it.</param>
    /// <param name="CommandLine">The command line, as <c>Environment.CommandLine</c> gives it.</param>
    /// <param name="ExpectedProcessKind">The expected kind.</param>
    private sealed record ProcessKindTestCase( string ProcessName, string CommandLine, BackstageProcessKind ExpectedProcessKind );

    /// <summary>
    /// The rows of the table, one for every process name and every command line that the classification matches.
    /// </summary>
    private static readonly ProcessKindTestCase[] _testCases =
    [
        // The user interface process of Visual Studio.
        new( "devenv", @"C:\Program Files\Microsoft Visual Studio\devenv.exe", BackstageProcessKind.DevEnv ),

        // The Roslyn analysis process, under the three names it has had.
        new( "ServiceHub.RoslynCodeAnalysisService", "", BackstageProcessKind.RoslynCodeAnalysisService ),
        new( "ServiceHub.RoslynCodeAnalysisServiceS", "", BackstageProcessKind.RoslynCodeAnalysisService ),
        new( "DevHub", "", BackstageProcessKind.RoslynCodeAnalysisService ),

        // The Code Lens service, which is told apart from the other ServiceHub services by its command line.
        new( "ServiceHub.Host", "ServiceHub.Host.dll $CodeLensService$", BackstageProcessKind.CodeLensService ),
        new( "ServiceHub.Host", "ServiceHub.Host.dll $SomeOtherService$", BackstageProcessKind.Other ),

        // The compiler and the compiler server of the .NET Framework build.
        new( "csc", "", BackstageProcessKind.Compiler ),
        new( "VBCSCompiler", "", BackstageProcessKind.Compiler ),

        // The test runner of Rider and of ReSharper.
        new( "ReSharperTestRunner", "", BackstageProcessKind.ResharperTestRunner ),
        new( "ReSharperTestRunner64", "", BackstageProcessKind.ResharperTestRunner ),

        // The language server of the Visual Studio Code C# Dev Kit, under both names it has had.
        new( "Microsoft.CodeAnalysis.LanguageServer", "", BackstageProcessKind.LanguageServer ),
        new( "Microsoft.VisualStudio.Code.LanguageServer", "", BackstageProcessKind.LanguageServer ),

        // An MSBuild node and the test host of the .NET Framework build.
        new( "MSBuild", "", BackstageProcessKind.MsBuild ),
        new( "testhost", "", BackstageProcessKind.TestHost ),

        // The hosts that run as an assembly under the dotnet process name.
        new( "dotnet", @"dotnet C:\Rider\JetBrains.ReSharper.Roslyn.Worker.dll", BackstageProcessKind.Rider ),
        new( "dotnet", @"dotnet C:\Rider\JetBrains.Roslyn.Worker.dll", BackstageProcessKind.Rider ),
        new( "dotnet", @"dotnet C:\Sdk\Roslyn\bincore\VBCSCompiler.dll", BackstageProcessKind.Compiler ),
        new( "dotnet", @"dotnet C:\Sdk\Roslyn\bincore\csc.dll", BackstageProcessKind.Compiler ),
        new( "dotnet", @"dotnet C:\DevKit\Microsoft.CodeAnalysis.LanguageServer.dll", BackstageProcessKind.LanguageServer ),
        new( "dotnet", @"dotnet C:\OmniSharp\OmniSharp.dll", BackstageProcessKind.OmniSharp ),
        new( "dotnet", @"dotnet C:\Rider\ReSharperTestRunner.dll", BackstageProcessKind.ResharperTestRunner ),
        new( "dotnet", @"dotnet C:\Sdk\MSBuild.dll", BackstageProcessKind.MsBuild ),
        new( "dotnet", @"dotnet C:\Sdk\dotnet-format.dll", BackstageProcessKind.Format ),
        new( "dotnet", @"dotnet C:\MyApp\MyApp.dll", BackstageProcessKind.Other ),

        // The LinqPad driver proxy process, which is matched by prefix because its name carries the version.
        new( "LINQPad", "", BackstageProcessKind.LinqPad ),
        new( "LINQPad8", "", BackstageProcessKind.LinqPad ),

        // Visual Studio for Mac is sunset and PB-2027.0 does not include it, so its process name is no longer
        // classified.
        new( "VisualStudio", "", BackstageProcessKind.Other ),

        // A process that no arm of the table matches.
        new( "notepad", "", BackstageProcessKind.Other )
    ];

    /// <summary>
    /// Gets the rows of <see cref="_testCases"/> in the form that <see cref="MemberDataAttribute"/> requires.
    /// </summary>
    public static IEnumerable<object[]> TestCases
        => _testCases.SelectAsArray( c => new object[] { c.ProcessName, c.CommandLine, c.ExpectedProcessKind } );

    /// <summary>
    /// Verifies that the two assemblies classify the host process into the same set of kinds. The test fails when
    /// one of them stops compiling the shared source file, and it failed before that file existed, when the two
    /// copies of the classification had diverged on the language server of the Visual Studio Code C# Dev Kit.
    /// </summary>
    [Fact]
    public void BothAssembliesDeclareTheSameProcessKinds()
    {
        var backstageNames = Enum.GetNames( typeof(BackstageProcessKind) ).OrderBy( n => n, StringComparer.Ordinal );

        var compilerExtensionsNames =
            Enum.GetNames( GetCompilerExtensionsType( "ProcessKind" ) ).OrderBy( n => n, StringComparer.Ordinal );

        Assert.Equal( backstageNames, compilerExtensionsNames );
    }

    /// <summary>
    /// Verifies that every arm of the table classifies as it is meant to, and that the two assemblies agree on
    /// every one of them.
    /// </summary>
    /// <param name="processName">The process name, as <c>Process.ProcessName</c> gives it.</param>
    /// <param name="commandLine">The command line, as <c>Environment.CommandLine</c> gives it.</param>
    /// <param name="expectedProcessKind">The expected kind.</param>
    [Theory]
    [MemberData( nameof(TestCases) )]
    public void BothAssembliesClassifyTheSameWay( string processName, string commandLine, BackstageProcessKind expectedProcessKind )
    {
        Assert.Equal( expectedProcessKind, BackstageProcessKindDetector.GetProcessKind( processName, commandLine ) );

        Assert.Equal( expectedProcessKind.ToString(), GetCompilerExtensionsProcessKindName( processName, commandLine ) );
    }

    /// <summary>
    /// Verifies that the table above covers every kind that the classification can return, so that a kind added to
    /// the enumeration without a row here is reported.
    /// </summary>
    /// <remarks>
    /// The four kinds excluded here are not returned by the classification. Three of them are declared by the
    /// application that carries them, through <c>IApplicationInfo.ProcessKind</c>, and the fourth is
    /// <see cref="BackstageProcessKind.VisualStudioMac"/>, which the enumeration keeps because
    /// <c>diagnostics.json</c> names it.
    /// </remarks>
    [Fact]
    public void EveryClassifiableKindIsCovered()
    {
        var kindsDeclaredByTheApplication = new[]
        {
            BackstageProcessKind.BackstageWorker,
            BackstageProcessKind.BackstageDesktopWindows,
            BackstageProcessKind.DotNetTool,
            BackstageProcessKind.VisualStudioMac
        };

        var expectedKinds = Enum.GetValues( typeof(BackstageProcessKind) )
            .Cast<BackstageProcessKind>()
            .Except( kindsDeclaredByTheApplication )
            .OrderBy( k => k.ToString(), StringComparer.Ordinal );

        var coveredKinds = _testCases
            .SelectAsArray( c => c.ExpectedProcessKind )
            .Distinct()
            .OrderBy( k => k.ToString(), StringComparer.Ordinal );

        Assert.Equal( expectedKinds, coveredKinds );
    }
}
