// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Formatting;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Options;

/// <summary>
/// An implementation of <see cref="IProjectOptions"/> that delegates all properties and methods to another <see cref="IProjectOptions"/>.
/// All members are virtual and at least one must be overridden.
/// </summary>
public abstract class ProjectOptionsWrapper : IProjectOptions
{
    private IProjectOptions Wrapped { get; }

    protected ProjectOptionsWrapper( IProjectOptions wrapped )
    {
        this.Wrapped = wrapped;
    }

    public int Id { get; } = ProjectOptionsIdGenerator.GetNextId();

    public virtual string? BuildTouchFile => this.Wrapped.BuildTouchFile;

    public virtual string? SourceGeneratorTouchFile => this.Wrapped.SourceGeneratorTouchFile;

    public virtual string? AssemblyName => this.Wrapped.AssemblyName;

    public virtual bool IsFrameworkEnabled => this.Wrapped.IsFrameworkEnabled;

    public virtual CodeFormattingOptions CodeFormattingOptions => this.Wrapped.CodeFormattingOptions;

    public virtual bool WriteHtml => this.Wrapped.WriteHtml;

    public virtual bool FormatCompileTimeCode => this.Wrapped.FormatCompileTimeCode;

    public virtual bool IsUserCodeTrusted => this.Wrapped.IsUserCodeTrusted;

    public virtual string? ProjectPath => this.Wrapped.ProjectPath;

    public virtual string? ProjectName => this.Wrapped.ProjectName;

    public virtual string? TargetFramework => this.Wrapped.TargetFramework;

    public virtual string? TargetFrameworkMoniker => this.Wrapped.TargetFrameworkMoniker;

    public virtual string? Configuration => this.Wrapped.Configuration;

    public virtual bool IsDesignTimeEnabled => this.Wrapped.IsDesignTimeEnabled;

    public virtual string? AdditionalCompilationOutputDirectory => this.Wrapped.AdditionalCompilationOutputDirectory;

    public virtual IProjectOptions Apply( IProjectOptions options ) => this.Wrapped.Apply( options );

    public virtual bool TryGetProperty( string name, out string? value ) => this.Wrapped.TryGetProperty( name, out value );

    public virtual bool RemoveCompileTimeOnlyCode => this.Wrapped.RemoveCompileTimeOnlyCode;

    public virtual bool RequiresCodeCoverageAnnotations => this.Wrapped.RequiresCodeCoverageAnnotations;

    public virtual bool AllowPreviewLanguageFeatures => this.Wrapped.AllowPreviewLanguageFeatures;

    public virtual bool RequireOrderedAspects => this.Wrapped.RequireOrderedAspects;

    public virtual bool IsConcurrentBuildEnabled => this.Wrapped.IsConcurrentBuildEnabled;

    public virtual ImmutableArray<string> CompileTimePackages => this.Wrapped.CompileTimePackages;

    public virtual ImmutableArray<ExtensionAssemblyReference> CompileTimeAssemblies => this.Wrapped.CompileTimeAssemblies;

    public virtual string? ProjectAssetsFile => this.Wrapped.ProjectAssetsFile;

    public virtual int? ReferenceAssemblyRestoreTimeout => this.Wrapped.ReferenceAssemblyRestoreTimeout;

    public virtual bool? WriteLicenseUsageData => this.Wrapped.WriteLicenseUsageData;

    public virtual bool? WriteTransformedFiles => this.Wrapped.WriteTransformedFiles;

    public virtual bool IsTest => this.Wrapped.IsTest;

    public virtual string? AssemblyLocatorHooksDirectory => this.Wrapped.AssemblyLocatorHooksDirectory;

    public virtual bool RoslynIsCompileTimeOnly => this.Wrapped.RoslynIsCompileTimeOnly;

    public virtual string? CompileTimeTargetFrameworks => this.Wrapped.CompileTimeTargetFrameworks;

    public virtual string? RestoreSources => this.Wrapped.RestoreSources;

    public virtual string? TemplateLanguageVersion => this.Wrapped.TemplateLanguageVersion;

    public virtual bool? DebugTransformedCode => this.Wrapped.DebugTransformedCode;

    public virtual string? TransformedFilesOutputPath => this.Wrapped.TransformedFilesOutputPath;

    public virtual ImmutableArray<string> SourceGeneratorAttributes => this.Wrapped.SourceGeneratorAttributes;

    public virtual ImmutableArray<ExtensionAssemblyReference> ExtensionAssemblies => this.Wrapped.ExtensionAssemblies;

    public virtual ImmutableArray<ExtensionAssemblyReference> DesignTimeExtensionAssemblies => this.Wrapped.DesignTimeExtensionAssemblies;

    public virtual bool AvoidLockingExtensionAssemblies => this.Wrapped.AvoidLockingExtensionAssemblies;

    public sealed override int GetHashCode() => throw new NotImplementedException();

    public sealed override bool Equals( object? obj ) => this.Equals( obj as IProjectOptions );

    public bool Equals( IProjectOptions? other ) => other?.GetType() == this.GetType() && this.Wrapped.Equals( ((ProjectOptionsWrapper) other).Wrapped );
}