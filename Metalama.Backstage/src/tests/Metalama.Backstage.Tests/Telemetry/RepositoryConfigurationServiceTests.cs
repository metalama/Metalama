// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Repositories;
using Metalama.Backstage.Testing;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Telemetry;

public sealed class RepositoryConfigurationServiceTests : TestsBase
{
    private const string _repoRoot = @"C:\repo";
    private const string _projectDirectory = @"C:\repo\src\project";

    public RepositoryConfigurationServiceTests( ITestOutputHelper logger ) : base( logger ) { }

    private RepositoryConfigurationService CreateService() => new( this.ServiceProvider );

    private void CreateGitRoot() => this.FileSystem.CreateDirectory( Path.Combine( _repoRoot, ".git" ) );

    private void WriteFile( string directory, string content )
    {
        this.FileSystem.CreateDirectory( directory );
        this.FileSystem.WriteAllText( Path.Combine( directory, "metalama.json" ), content );
    }

    [Fact]
    public void OptOutAtRepoRootIsRead()
    {
        this.CreateGitRoot();
        this.WriteFile( _repoRoot, """{ "telemetry": { "enabled": false } }""" );

        var result = this.CreateService().GetConfiguration( _projectDirectory );

        Assert.False( result.Configuration.Telemetry!.Enabled );
        Assert.Empty( result.Warnings );
    }

    [Fact]
    public void ExplicitOptInIsParsed()
    {
        this.CreateGitRoot();
        this.WriteFile( _repoRoot, """{ "telemetry": { "enabled": true } }""" );

        var result = this.CreateService().GetConfiguration( _projectDirectory );

        Assert.True( result.Configuration.Telemetry!.Enabled );
        Assert.Empty( result.Warnings );
    }

    [Fact]
    public void CamelCaseAndUnknownKeysAreTolerated()
    {
        this.CreateGitRoot();

        // camelCase keys, plus an unknown key that must be ignored for forward-compatibility.
        this.WriteFile( _repoRoot, """{ "telemetry": { "enabled": false }, "someFutureSetting": 42 }""" );

        var result = this.CreateService().GetConfiguration( _projectDirectory );

        Assert.False( result.Configuration.Telemetry!.Enabled );
        Assert.Empty( result.Warnings );
    }

    [Fact]
    public void NoFileReturnsEmptyConfiguration()
    {
        this.CreateGitRoot();

        var result = this.CreateService().GetConfiguration( _projectDirectory );

        Assert.Null( result.Configuration.Telemetry );
        Assert.Empty( result.Warnings );
    }

    [Fact]
    public void MisplacedFileIsIgnoredAndWarns()
    {
        this.CreateGitRoot();

        // The file is in a non-root directory: it must be ignored and a warning produced.
        this.WriteFile( Path.Combine( _repoRoot, "src" ), """{ "telemetry": { "enabled": false } }""" );

        var result = this.CreateService().GetConfiguration( _projectDirectory );

        Assert.Null( result.Configuration.Telemetry );
        Assert.Equal( RepositoryConfigurationWarningKind.MisplacedFile, Assert.Single( result.Warnings ).Kind );
    }

    [Fact]
    public void MalformedFileIsIgnoredAndWarns()
    {
        this.CreateGitRoot();
        this.WriteFile( _repoRoot, "{ this is not valid json" );

        var result = this.CreateService().GetConfiguration( _projectDirectory );

        Assert.Null( result.Configuration.Telemetry );
        Assert.Equal( RepositoryConfigurationWarningKind.MalformedFile, Assert.Single( result.Warnings ).Kind );
    }

    [Fact]
    public void WithoutGitRootFileIsIgnoredSilently()
    {
        // No .git anywhere: the repository root cannot be confirmed, so no metalama.json is honored and the global
        // default applies. This is a common case (e.g. building outside a git working tree), so it is not even warned.
        this.WriteFile( _repoRoot, """{ "telemetry": { "enabled": false } }""" );

        var result = this.CreateService().GetConfiguration( _projectDirectory );

        Assert.Null( result.Configuration.Telemetry );
        Assert.Empty( result.Warnings );
    }

    [Fact]
    public void ResultIsCachedPerDirectory()
    {
        this.CreateGitRoot();
        this.WriteFile( _repoRoot, """{ "telemetry": { "enabled": false } }""" );

        var service = this.CreateService();
        var first = service.GetConfiguration( _projectDirectory );

        // Changing the file after the first resolution must not affect the cached result (10-minute TTL).
        this.FileSystem.WriteAllText( Path.Combine( _repoRoot, "metalama.json" ), """{ "telemetry": { "enabled": true } }""" );
        var second = service.GetConfiguration( _projectDirectory );

        Assert.Same( first, second );
        Assert.False( second.Configuration.Telemetry!.Enabled );
    }
}
