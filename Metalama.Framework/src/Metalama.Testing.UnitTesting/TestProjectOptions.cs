// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Utilities;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Options;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Metalama.Testing.UnitTesting;

/// <summary>
/// An implementation of <see cref="IProjectOptions"/> that can be used in tests.
/// </summary>
internal sealed class TestProjectOptions : DefaultProjectOptions, IDisposable
{
    public TestContextOptions TestContextOptions { get; }

    private readonly Lazy<string> _baseDirectory;
    private readonly Lazy<string> _projectDirectory;

    private int _fileLockers;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestProjectOptions"/> class from
    /// a prototype <see cref="UnitTesting.TestContextOptions"/>, allowing to override some properties.
    /// </summary>
    public TestProjectOptions( TestProjectOptions prototype, CodeFormattingOptions? codeFormattingOptions = null )
    {
        this._baseDirectory = prototype._baseDirectory;
        this._projectDirectory = prototype._projectDirectory;
        this.SourceGeneratorTouchFile = prototype.SourceGeneratorTouchFile;
        this.BuildTouchFile = prototype.BuildTouchFile;
        this.TestContextOptions = prototype.TestContextOptions;
        this.DomainObserver = new DomainObserverImpl( this );
    }

    public TestProjectOptions( TestContextOptions testContextOptions )
    {
        this.TestContextOptions = testContextOptions;

        // We don't use the backstage TempFileManager because it would generate paths that are too long.
        var baseDirectory = Path.Combine( MetalamaPathUtilities.GetTempPath(), "Metalama", "Tests", Guid.NewGuid().ToString() );

        if ( testContextOptions.TempPathLength.HasValue )
        {
            var currentLenght = baseDirectory.Length;
            var remainingLength = testContextOptions.TempPathLength.Value - currentLenght - 1;

            switch ( remainingLength )
            {
                case < 0:
                    throw new InvalidOperationException( "The base path is too short." );

                case >= 0:
                    baseDirectory += '_' + new string( 'x', remainingLength );

                    break;
            }
        }

        this._baseDirectory = CreateDirectoryLazy( baseDirectory );
        this._projectDirectory = CreateDirectoryLazy( Path.Combine( baseDirectory, "Project" ) );

        if ( testContextOptions.HasSourceGeneratorTouchFile )
        {
            this.SourceGeneratorTouchFile = Path.Combine( baseDirectory, "SourceGeneratorTouchFile.txt" );
        }

        if ( testContextOptions.HasBuildTouchFile )
        {
            this.BuildTouchFile = Path.Combine( baseDirectory, "BuildTouchFile.txt" );
        }

        this.DomainObserver = new DomainObserverImpl( this );
    }

    internal ICompileTimeDomainObserver DomainObserver { get; }

    private static Lazy<string> CreateDirectoryLazy( string path )
        => new(
            () =>
            {
                Directory.CreateDirectory( path );

                return path;
            } );

    public override string? ProjectName => this.TestContextOptions.ProjectName;

    public string BaseDirectory => this._baseDirectory.Value;

    public override CodeFormattingOptions CodeFormattingOptions => this.TestContextOptions.CodeFormattingOptions;

    public override bool FormatCompileTimeCode => this.TestContextOptions.FormatCompileTimeCode;

    public override bool RequireOrderedAspects => this.TestContextOptions.RequireOrderedAspects;

    public ImmutableArray<Assembly> AdditionalAssemblies => this.TestContextOptions.AdditionalAssemblies;

    public override string? SourceGeneratorTouchFile { get; }

    public override bool IsTest => true;

    public override string? BuildTouchFile { get; }

    public override bool RoslynIsCompileTimeOnly => this.TestContextOptions.RoslynIsCompileTimeOnly;

    public override ImmutableArray<ExtensionAssemblyReference> CompileTimeAssemblies
        => this.TestContextOptions.CompileTimeAssemblies.Select( x => new ExtensionAssemblyReference( x ) ).ToImmutableArray();

    public override string? TemplateLanguageVersion => this.TestContextOptions.TemplateLanguageVersion;

    public override bool TryGetProperty( string name, [NotNullWhen( true )] out string? value )
        => this.TestContextOptions.Properties.TryGetValue( name, out value );

    private void AddFileLocker() => Interlocked.Increment( ref this._fileLockers );

    private void RemoveFileLocker()
    {
        if ( Interlocked.Decrement( ref this._fileLockers ) == 0 )
        {
            this.Dispose();
        }
    }

    public void Dispose()
    {
        if ( this._fileLockers == 0 )
        {
            if ( Directory.Exists( this.BaseDirectory ) )
            {
                try
                {
                    RetryHelper.Retry( () => Directory.Delete( this.BaseDirectory, true ) );
                }
                catch ( DirectoryNotFoundException ) { }
                catch ( UnauthorizedAccessException ) { }
            }
        }
    }

    private sealed class DomainObserverImpl : ICompileTimeDomainObserver
    {
        private readonly TestProjectOptions _parent;

        public DomainObserverImpl( TestProjectOptions parent )
        {
            this._parent = parent;
        }

        void ICompileTimeDomainObserver.OnDomainCreated( CompileTimeDomain domain ) => this._parent.AddFileLocker();

        void ICompileTimeDomainObserver.OnDomainUnloaded( CompileTimeDomain domain ) => this._parent.RemoveFileLocker();
    }
}