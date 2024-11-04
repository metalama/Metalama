// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.AspectWeavers;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.RunTime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Metalama.Testing.UnitTesting;

/// <summary>
/// Utility class that creates a <see cref="CSharpCompilation"/>.
/// </summary>
[PublicAPI]
public static class TestCompilationFactory
{
    private static readonly ConcurrentDictionary<Assembly, PortableExecutableReference> _metadataReferenceCache = new();

    /// <summary>
    /// List of system assemblies that can be added as references to compilation if they are present in the AppDomain.
    /// </summary>
    private static readonly ImmutableHashSet<string> _allowedSystemAssemblies = ImmutableHashSet.Create(
        "System",
        "System.Collections",
        "System.Collections.Concurrent",
        "System.Collections.Immutable",
        "System.Collections.NonGeneric",
        "System.ComponentModel",
        "System.Console",
        "System.Core",
        "System.IO",
        "System.IO.FileSystem",
        "System.IO.FileSystem.Watcher",
        "System.Linq",
        "System.Linq.Expressions",
        "System.Memory",
        "System.ObjectModel",
        "System.Reflection",
        "System.Runtime",
        "System.Text.RegularExpressions",
        "System.Threading",
        "System.Threading.Tasks",
        "System.Threading.Thread",
        "System.Threading.ThreadPool",
        "System.Private.CoreLib" );

    public static CSharpCompilation CreateEmptyCSharpCompilation(
        string? name,
        IEnumerable<Assembly>? additionalAssemblies = null,
        bool addMetalamaReferences = true,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        ImmutableArray<string> implicitUsings = default,
        NullableContextOptions nullableContextOptions = NullableContextOptions.Enable,
        bool warnAsErrors = false )
        => CreateEmptyCSharpCompilation(
            name,
            GetMetadataReferences( additionalAssemblies, addMetalamaReferences ),
            outputKind,
            implicitUsings,
            nullableContextOptions,
            warnAsErrors );

    public static CSharpCompilation CreateEmptyCSharpCompilation(
        string? name,
        IEnumerable<MetadataReference> metadataReferences,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        ImmutableArray<string> implicitUsings = default,
        NullableContextOptions nullableContextOptions = NullableContextOptions.Enable,
        bool warnAsErrors = false )
        => CSharpCompilation.Create( name ?? "test_" + RandomIdGenerator.GenerateId() )
            .WithOptions( GetCompilationOptions( outputKind, implicitUsings, nullableContextOptions, warnAsErrors ) )
            .AddReferences( metadataReferences );

    public static IReadOnlyList<PortableExecutableReference> GetMetadataReferences(
        IEnumerable<Assembly>? additionalAssemblies = null,
        bool addMetalamaReferences = true )
    {
#if NET5_0_OR_GREATER
        var standardLibrariesNames = new[] { "netstandard" };
#else
        var standardLibrariesNames = new[] { "netstandard", "mscorlib" };
#endif

        var standardLibraries = standardLibrariesNames
            .SelectAsImmutableArray(
                r => MetadataReference.CreateFromFile( Path.Combine( Path.GetDirectoryName( typeof(object).Assembly.Location )!, r + ".dll" ) ) );

        var metalamaLibraries = addMetalamaReferences
            ? new[] { typeof(IAspect).Assembly, typeof(IAspectWeaver).Assembly, typeof(ITemplateSyntaxFactory).Assembly, typeof(FieldOrPropertyInfo).Assembly }
            : null;

        // Force the loading of some system assemblies before we search them in the AppDomain.
        _ = typeof(DynamicAttribute).Assembly;
        _ = typeof(Console).Assembly;
#if NETFRAMEWORK
        _ = Assembly.Load( "System.Reflection, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" );
        _ = Assembly.Load( "System.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" );
#endif

        var systemLibraries = AppDomainUtility.GetLoadedAssemblies(
                a => !a.IsDynamic && _allowedSystemAssemblies.Contains( a.GetName().Name.AssertNotNull() )
                                  && !string.IsNullOrEmpty( a.Location ) )
            .Concat( metalamaLibraries ?? Enumerable.Empty<Assembly>() )
            .Concat( additionalAssemblies ?? Enumerable.Empty<Assembly>() )
            .Distinct()
            .Select( GetCachedMetadataReference )
            .ToReadOnlyList();

        return standardLibraries.Concat( systemLibraries );
    }

    // Caching is critical for memory usage, otherwise we get random OutOfMemoryException in parallel tests.
    private static PortableExecutableReference GetCachedMetadataReference( Assembly assembly )
        => _metadataReferenceCache.GetOrAdd( assembly, a => MetadataReference.CreateFromFile( a.Location ) );

    public static CSharpParseOptions GetParseOptions( IEnumerable<string>? preprocessorSymbols = null )
        => SupportedCSharpVersions.DefaultParseOptions.WithPreprocessorSymbols( preprocessorSymbols ?? new[] { "METALAMA" } );

    public static CSharpCompilationOptions GetCompilationOptions(
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        ImmutableArray<string> implicitUsings = default,
        NullableContextOptions nullableContextOptions = NullableContextOptions.Enable,
        bool warnAsErrors = false )
        => new(
            outputKind,
            allowUnsafe: true,
            nullableContextOptions: nullableContextOptions,
            usings: implicitUsings.IsDefault
                ? ImmutableArray<string>.Empty
                : implicitUsings,
            metadataImportOptions: MetadataImportOptions.All,
            generalDiagnosticOption: warnAsErrors ? ReportDiagnostic.Error : ReportDiagnostic.Default );

    public static CSharpCompilation CreateCSharpCompilation(
        string code,
        string? dependentCode = null,
        bool ignoreErrors = false,
        IEnumerable<MetadataReference>? additionalReferences = null,
        string? name = null,
        bool addMetalamaReferences = true,
        IEnumerable<string>? preprocessorSymbols = null,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        bool warnAsErrors = false )
        => CreateCSharpCompilation(
            new Dictionary<string, string> { { RandomIdGenerator.GenerateId() + ".cs", code } },
            dependentCode,
            ignoreErrors,
            additionalReferences,
            name,
            addMetalamaReferences,
            preprocessorSymbols,
            outputKind,
            warnAsErrors );

    public static CSharpCompilation CreateCSharpCompilation(
        IReadOnlyDictionary<string, string> code,
        string? dependentCode = null,
        bool ignoreErrors = false,
        IEnumerable<MetadataReference>? additionalReferences = null,
        string? name = null,
        bool addMetalamaReferences = true,
        IEnumerable<string>? preprocessorSymbols = null,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        bool warnAsErrors = false )
        => CreateCSharpCompilation(
            code,
            dependentCode == null ? null : ImmutableDictionary.Create<string, string>().Add( "dependent.cs", dependentCode ),
            ignoreErrors,
            additionalReferences,
            name,
            addMetalamaReferences,
            preprocessorSymbols,
            outputKind,
            warnAsErrors );

    public static CSharpCompilation CreateCSharpCompilation(
        IReadOnlyDictionary<string, string> code,
        IReadOnlyDictionary<string, string>? dependentCode,
        bool ignoreErrors = false,
        IEnumerable<MetadataReference>? additionalReferences = null,
        string? name = null,
        bool addMetalamaReferences = true,
        IEnumerable<string>? preprocessorSymbols = null,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        bool warnAsErrors = false )
    {
        var additionalAssemblies = new[] { typeof(FieldOrPropertyInfo).Assembly, typeof(UnitTestClass).Assembly };

        var parseOptions = GetParseOptions( preprocessorSymbols );

        var mainRoslynCompilation = CreateEmptyCSharpCompilation( name, additionalAssemblies, addMetalamaReferences, outputKind, warnAsErrors: warnAsErrors )
            .AddSyntaxTrees( code.SelectAsArray( c => SyntaxFactory.ParseSyntaxTree( c.Value, path: c.Key, options: parseOptions ) ) );

        if ( dependentCode != null )
        {
            var dependentCompilation = CreateEmptyCSharpCompilation(
                    name == null ? null : null + ".Dependency",
                    additionalAssemblies,
                    warnAsErrors: warnAsErrors )
                .AddSyntaxTrees( dependentCode.SelectAsArray( c => SyntaxFactory.ParseSyntaxTree( c.Value, path: c.Key, options: parseOptions ) ) );

            mainRoslynCompilation = mainRoslynCompilation.AddReferences( dependentCompilation.ToMetadataReference() );
        }

        if ( additionalReferences != null )
        {
            mainRoslynCompilation = mainRoslynCompilation.AddReferences( additionalReferences );
        }

        if ( !ignoreErrors && !warnAsErrors )
        {
            AssertNoError( mainRoslynCompilation );
        }

        return mainRoslynCompilation;
    }

    private static void AssertNoError( CSharpCompilation mainRoslynCompilation )
    {
        var diagnostics = mainRoslynCompilation.GetDiagnostics();

        if ( diagnostics.Any( diag => diag.Severity >= DiagnosticSeverity.Error ) )
        {
            var lines = diagnostics.Select( diag => diag.ToString() ).Prepend( "The given code produced errors:" );

            throw new InvalidOperationException( string.Join( Environment.NewLine, lines ) );
        }
    }
}