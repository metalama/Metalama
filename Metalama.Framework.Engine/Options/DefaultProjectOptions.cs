// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Formatting;
using System;
using System.Collections.Immutable;
using System.IO;

namespace Metalama.Framework.Engine.Options;

/// <summary>
/// A base implementation of <see cref="IProjectOptions"/> that provides default values.
/// </summary>
public class DefaultProjectOptions : IProjectOptions
{
    public virtual string? BuildTouchFile => null;

    public virtual string? SourceGeneratorTouchFile => null;

    public virtual string? AssemblyName => null;

    public virtual bool IsFrameworkEnabled => true;

    public virtual CodeFormattingOptions CodeFormattingOptions => CodeFormattingOptions.Default;

    public virtual bool WriteHtml => false;

    public virtual bool FormatCompileTimeCode => false;

    public virtual bool IsUserCodeTrusted => true;

    public virtual string? ProjectPath => null;

    public virtual string? ProjectName
    {
        get
        {
            var path = this.ProjectPath;

            if ( path == null )
            {
                return null;
            }
            else
            {
                return Path.GetFileNameWithoutExtension( path );
            }
        }
    }

    public virtual string? TargetFramework => "net6.0";

    public virtual string? TargetFrameworkMoniker => null;

    public virtual string? Configuration => "Debug";

    public virtual IProjectOptions Apply( IProjectOptions options ) => options;

    public virtual bool TryGetProperty( string name, out string? value )
    {
        value = null;

        return false;
    }

    public virtual bool RemoveCompileTimeOnlyCode => true;

    public virtual bool RequiresCodeCoverageAnnotations => false;

    public virtual bool AllowPreviewLanguageFeatures => false;

    public virtual bool RequireOrderedAspects => false;

    public virtual bool IsConcurrentBuildEnabled => false;

    public virtual ImmutableArray<string> CompileTimePackages => ImmutableArray<string>.Empty;

    public virtual string? ProjectAssetsFile => null;

    public virtual int? ReferenceAssemblyRestoreTimeout => null;

    public virtual bool IsDesignTimeEnabled => true;

    public virtual string? AdditionalCompilationOutputDirectory => null;

    public virtual string? License => null;

    public virtual bool IgnoreUserProfileLicense => false;

    public virtual bool? WriteLicenseUsageData => null;

    public virtual bool RoslynIsCompileTimeOnly => true;

    public virtual string? CompileTimeTargetFrameworks => null;

    public virtual string? RestoreSources => null;

    public virtual bool? WriteTransformedFiles => false;

    public virtual bool IsTest => false;

    public virtual string? AssemblyLocatorHooksDirectory => null;

    public virtual string? TemplateLanguageVersion => null;

    public virtual bool? DebugTransformedCode => null;

    public virtual string? TransformedFilesOutputPath => null;

    public virtual ImmutableArray<string> SourceGeneratorAttributes => ImmutableArray<string>.Empty;

    // IProjectOptions is currently not used as a dictionary key, so we can throw here.
    public sealed override int GetHashCode() => throw new NotImplementedException();

    public sealed override bool Equals( object? obj ) => this.Equals( obj as IProjectOptions );

    public virtual bool Equals( IProjectOptions? other ) => ReferenceEquals( this, other );
}