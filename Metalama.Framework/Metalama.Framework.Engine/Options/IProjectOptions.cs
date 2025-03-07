// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Services;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Options;

/// <summary>
/// Exposes project options (typically defined in MSBuild or .editorconfig) in a strongly-typed manner.
/// The production implementation is <see cref="MSBuildProjectOptions"/> but tests can provide their own implementation.
/// </summary>
public interface IProjectOptions : IProjectService, IEquatable<IProjectOptions>
{
    /// <summary>
    /// Gets a unique id of the options in the current process.
    /// It helps checking for equality without keeping too much memory allocated.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets the path to a file that gets touched when the project is built.
    /// </summary>
    string? BuildTouchFile { get; }

    /// <summary>
    /// Gets the path to a file that gets touched when the Roslyn analysis process generates new sources.
    /// </summary>
    string? SourceGeneratorTouchFile { get; }

    string? AssemblyName { get; }

    /// <summary>
    /// Gets a value indicating whether the aspect framework is enabled for the current project. If <c>false</c>,
    /// the project will not be modified. 
    /// </summary>
    bool IsFrameworkEnabled { get; }

    /// <summary>
    /// Gets a value indicating how the output syntax trees must be formatted.
    /// </summary>
    CodeFormattingOptions CodeFormattingOptions { get; }

    /// <summary>
    /// Gets a value indicating whether HTML files should be written for syntax-highlighted input and transformed
    /// code of all syntax trees in the project.
    /// </summary>
    bool WriteHtml { get; }

    /// <summary>
    /// Gets a value indicating whether the compile-time code must be formatted.
    /// </summary>
    bool FormatCompileTimeCode { get; }

    /// <summary>
    /// Gets a value indicating whether the user code processed by Metalama is trusted.
    /// </summary>
    bool IsUserCodeTrusted { get; }

    /// <summary>
    /// Gets the path to the <c>csproj</c> file.
    /// </summary>
    string? ProjectPath { get; }

    /// <summary>
    /// Gets the project file name without extension.
    /// </summary>
    string? ProjectName { get; }

    /// <summary>
    /// Gets the short target framework name, for instance <c>net6.0</c>.
    /// </summary>
    string? TargetFramework { get; }

    /// <summary>
    /// Gets the full target framework moniker, for instance <c>.NETCoreApp,Version=v6.0</c>.
    /// </summary>
    string? TargetFrameworkMoniker { get; }

    /// <summary>
    /// Gets the build configuration, e.g. <c>Debug</c> or <c>Release</c>.
    /// </summary>
    string? Configuration { get; }

    /// <summary>
    /// Gets a value indicating whether the design time experience is enabled for the project.
    /// </summary>
    bool IsDesignTimeEnabled { get; }

    /// <summary>
    /// Gets a path to a directory that stores additional compilation output files.
    /// </summary>
    string? AdditionalCompilationOutputDirectory { get; }

    /// <summary>
    /// Invoked when project options have been applied globally or contextually through the <see cref="!:ServiceProviderFactory" />,
    /// and are then overridden by options provided by the compiler.
    /// </summary>
    IProjectOptions Apply( IProjectOptions options );

    bool TryGetProperty( string name, out string? value );

    /// <summary>
    /// Gets a value indicating whether the compile-time-only code should be removed from the main compiled assembly.
    /// </summary>
    bool RemoveCompileTimeOnlyCode { get; }

    /// <summary>
    /// Gets a value indicating whether the linker should annotate the code for code coverage.
    /// </summary>
    bool RequiresCodeCoverageAnnotations { get; }

    bool AllowPreviewLanguageFeatures { get; }

    bool RequireOrderedAspects { get; }

    bool IsConcurrentBuildEnabled { get; }

    /// <summary>
    /// Gets the list of packages (given by name only) that should be included in the compile-time project.
    /// </summary>
    ImmutableArray<string> CompileTimePackages { get; }

    /// <summary>
    /// Gets the list of assemblies that should be included in the compile-time project.
    /// </summary>
    ImmutableArray<ExtensionAssemblyReference> CompileTimeAssemblies { get; }

    /// <summary>
    /// Gets the path to <c>project.assets.json</c>.
    /// </summary>
    string? ProjectAssetsFile { get; }

    /// <summary>
    /// Gets the timeout used when restoring reference assemblies, or <c>null</c> if the default value should be used.
    /// </summary>
    int? ReferenceAssemblyRestoreTimeout { get; }

    /// <summary>
    /// Gets a value indicating whether the json file with license consumption data should be written. If the property
    /// is null, it is considered <c>true</c> in trial mode and <c>false</c> otherwise.
    /// </summary>
    bool? WriteLicenseUsageData { get; }

    /// <summary>
    /// Gets a value indicating whether Roslyn (Microsoft.CodeAnalysis) types are considered compile-time-only.
    /// When set to <c>false</c>, Roslyn types are considered run-time-or-compile-time, which means they can be used in run-time code.
    /// </summary>
    bool RoslynIsCompileTimeOnly { get; }

    /// <summary>
    /// Gets a semicolon-separated list of target frameworks that can be used for compile-time code, e.g. <c>netstandard2.0;net6.0;net48</c>.
    /// </summary>
    string? CompileTimeTargetFrameworks { get; }

    /// <summary>
    /// Gets NuGet sources configured for restoring packages.
    /// </summary>
    string? RestoreSources { get; }

    /// <summary>
    /// Gets the C# language version that's used by templates. Any syntax from higher C# versions is not allowed in template bodies.
    /// </summary>
    string? TemplateLanguageVersion { get; }

    /// <summary>
    /// Gets a value indicating whether the transformed code should be debugged, as opposed to original user code.
    /// </summary>
    bool? DebugTransformedCode { get; }

    /// <summary>
    /// Gets the path to the directory where transformed files should be written.
    /// </summary>
    string? TransformedFilesOutputPath { get; }

    /// <summary>
    /// Gets a value indicating whether transformed files will be written to disk.
    /// </summary>
    bool? WriteTransformedFiles { get; }

    bool IsTest { get; }

    /// <summary>
    /// Gets the path to the directory where the Metalama.AssemblyLocator.Build.props
    /// and/or Metalama.AssemblyLocator.Build.targets files are stored.
    /// </summary>
    string? AssemblyLocatorHooksDirectory { get; }

    /// <summary>
    /// Gets names of attribute types that are considered to be affected by source generators.
    /// Partial members marked with these attributes are not eligible for overriding by Metalama aspects.
    /// </summary>
    ImmutableArray<string> SourceGeneratorAttributes { get; }

    /// <summary>
    /// Gets the list of extension assemblies for the current project.
    /// </summary>
    ImmutableArray<ExtensionAssemblyReference> ExtensionAssemblies { get; }

    /// <summary>
    /// Gets the list of design-time extension assemblies for the current project.
    /// </summary>
    ImmutableArray<ExtensionAssemblyReference> DesignTimeExtensionAssemblies { get; }

    bool AvoidLockingExtensionAssemblies { get; }
}