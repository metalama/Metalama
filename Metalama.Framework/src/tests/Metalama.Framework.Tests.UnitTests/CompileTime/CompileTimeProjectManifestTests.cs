// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

public sealed class CompileTimeProjectManifestTests
{
    private static CompileTimeProjectManifest CreateManifest( LanguageVersion? languageVersion )
        => new(
            "test",
            ".NET Framework, Version=4.8",
            [],
            [],
            [],
            [],
            [],
            [],
            null,
            null,
            0,
            [],
            [],
            false,
            0,
            languageVersion );

    [Fact]
    public void LanguageVersionSerializedAsInt()
    {
        const LanguageVersion languageVersion = LanguageVersion.CSharp7;

        var json = CreateManifest( languageVersion ).ToJson();

        Assert.DoesNotContain( languageVersion.ToString(), json, StringComparison.Ordinal );
    }

    [Fact]
    public void UnexistingCSharpVersion()
    {
        const LanguageVersion languageVersion = (LanguageVersion) 9999;

        var json = CreateManifest( languageVersion ).ToJson();
        var roundtrip = CompileTimeProjectManifest.FromJson( json );
        Assert.Equal( languageVersion, roundtrip.LanguageVersion );
    }

    /// <summary>
    /// Verifies that a manifest that carries no language version resolves to C# 13, which is the highest version that
    /// the releases of Metalama that did not write the property could compile. Both reading sites use this value, so
    /// the manifest has one answer and not three. See issue #1928.
    /// </summary>
    [Fact]
    public void AbsentLanguageVersionResolvesToCSharp13()
    {
        var manifest = CreateManifest( null );

        Assert.Equal( AllLanguageVersions.CSharp13, manifest.RequiredLanguageVersion );
        Assert.Equal( AllLanguageVersions.CSharp13, manifest.ResolvedLanguageVersion );
    }

    /// <summary>
    /// Verifies that a language version that the Roslyn variant of the current process accepts is used as it is.
    /// </summary>
    [Fact]
    public void SupportedLanguageVersionIsNotClamped()
    {
        const LanguageVersion languageVersion = LanguageVersion.CSharp10;

        var manifest = CreateManifest( languageVersion );

        Assert.Equal( languageVersion, manifest.RequiredLanguageVersion );
        Assert.Equal( languageVersion, manifest.ResolvedLanguageVersion );
    }

    /// <summary>
    /// Verifies that a language version above the highest one that the Roslyn variant of the current process accepts
    /// is clamped, so that the compile-time code is parsed instead of being rejected with the Roslyn error
    /// <c>CS8192</c>, and that the version the manifest requires remains readable for the warning. See issue #1928.
    /// </summary>
    [Fact]
    public void LanguageVersionAboveTheRunningRoslynIsClamped()
    {
        // The value 1500 is C# 15, which no Roslyn version that Metalama consumes today knows.
        const LanguageVersion languageVersion = (LanguageVersion) 1500;

        var manifest = CreateManifest( languageVersion );

        Assert.Equal( languageVersion, manifest.RequiredLanguageVersion );
        Assert.Equal( RoslynApiVersion.Current.ToLanguageVersion(), manifest.ResolvedLanguageVersion );
    }

    /// <summary>
    /// Verifies that <see cref="CompileTimeProjectManifest.TryDeserialize"/> fails on a manifest that names the retired
    /// Roslyn API version <c>5.11.0</c>, and returns the Metalama version that the manifest names, so that the caller
    /// can report that the dependency must be recompiled. See issue #2400.
    /// </summary>
    [Fact]
    public void TryDeserializeReturnsMetalamaVersionOfManifestWithRetiredApiVersion()
    {
        const string metalamaVersion = "2027.0.1-preview";

        var json = CreateManifestWithTemplate( metalamaVersion ).ToJson();
        var usedApiVersionRegex = new Regex( "\"usedApiVersion\":\\s*\"[^\"]*\"" );

        Assert.Matches( usedApiVersionRegex, json );

        var retiredJson = usedApiVersionRegex.Replace( json, "\"usedApiVersion\": \"5.11.0\"" );

        using var stream = new MemoryStream( Encoding.UTF8.GetBytes( retiredJson ) );

        Assert.False( CompileTimeProjectManifest.TryDeserialize( stream, out var manifest, out var readMetalamaVersion ) );
        Assert.Null( manifest );
        Assert.Equal( metalamaVersion, readMetalamaVersion );
    }

    /// <summary>
    /// Verifies that <see cref="CompileTimeProjectManifest.TryDeserialize"/> reads a valid manifest and returns the
    /// Metalama version that wrote it.
    /// </summary>
    [Fact]
    public void TryDeserializeReadsValidManifest()
    {
        const string metalamaVersion = "2027.0.1-preview";

        var json = CreateManifestWithTemplate( metalamaVersion ).ToJson();

        using var stream = new MemoryStream( Encoding.UTF8.GetBytes( json ) );

        Assert.True( CompileTimeProjectManifest.TryDeserialize( stream, out var manifest, out var readMetalamaVersion ) );
        Assert.Equal( metalamaVersion, manifest.MetalamaVersion );
        Assert.Equal( metalamaVersion, readMetalamaVersion );
    }

    /// <summary>
    /// Verifies that <see cref="CompileTimeProjectManifest.TryDeserialize"/> returns a <c>null</c> Metalama version when
    /// the content is not valid JSON.
    /// </summary>
    [Fact]
    public void TryDeserializeReturnsNullVersionForInvalidJson()
    {
        using var stream = new MemoryStream( Encoding.UTF8.GetBytes( "{ not json" ) );

        Assert.False( CompileTimeProjectManifest.TryDeserialize( stream, out var manifest, out var readMetalamaVersion ) );
        Assert.Null( manifest );
        Assert.Null( readMetalamaVersion );
    }

    /// <summary>
    /// Creates a manifest that names the given Metalama version and that contains one template that uses the lowest
    /// Roslyn API version, so that the JSON contains a <c>usedApiVersion</c> property.
    /// </summary>
    private static CompileTimeProjectManifest CreateManifestWithTemplate( string metalamaVersion )
    {
        var templateSymbol = new TemplateSymbolManifest(
            id: "MyNamespace.MyAspect.MyTemplate()",
            scope: ExecutionScope.CompileTime,
            templateInfo: new TemplateInfoManifest( TemplateAttributeType.Template, isAbstract: false, hasNoBody: false ),
            usedApiVersion: RoslynApiVersion.Lowest,
            children: null );

        var rootSymbol = new TemplateSymbolManifest(
            id: "",
            scope: ExecutionScope.RunTime,
            templateInfo: null,
            usedApiVersion: null,
            children: new Dictionary<string, IReadOnlyList<TemplateSymbolManifest>> { ["MyTemplate"] = [templateSymbol] } );

        return new CompileTimeProjectManifest(
            "test",
            ".NET Framework, Version=4.8",
            [],
            [],
            [],
            [],
            [],
            [],
            null,
            new TemplateProjectManifest( rootSymbol ),
            0,
            [],
            [],
            false,
            0,
            null,
            metalamaVersion );
    }
}
