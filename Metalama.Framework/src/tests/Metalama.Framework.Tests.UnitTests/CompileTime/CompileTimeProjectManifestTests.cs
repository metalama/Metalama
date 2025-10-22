// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime.Manifest;
using Microsoft.CodeAnalysis.CSharp;
using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime;

public sealed class CompileTimeProjectManifestTests
{
    [Fact]
    public void LanguageVersionSerializedAsInt()
    {
        const LanguageVersion languageVersion = LanguageVersion.CSharp7;

        var manifest = new CompileTimeProjectManifest(
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
            null,
            0,
            languageVersion );

        var json = manifest.ToJson();

        Assert.DoesNotContain( languageVersion.ToString(), json, StringComparison.Ordinal );
    }

    [Fact]
    public void UnexistingCSharpVersion()
    {
        const LanguageVersion languageVersion = (LanguageVersion) 9999;

        var manifest = new CompileTimeProjectManifest(
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
            null,
            0,
            languageVersion );

        var json = manifest.ToJson();
        var roundtrip = CompileTimeProjectManifest.FromJson( json );
        Assert.Equal( languageVersion, roundtrip.LanguageVersion );
    }
}