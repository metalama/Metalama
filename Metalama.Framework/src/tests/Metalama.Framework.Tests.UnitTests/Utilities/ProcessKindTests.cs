// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Xunit;
using BackstageProcessKind = Metalama.Backstage.Diagnostics.ProcessKind;
using CompilerExtensionsProcessKind = Metalama.Framework.CompilerExtensions.ProcessKind;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// Tests the classification of the host process by its name. The classification is compiled into
/// <c>Metalama.Backstage</c> and into <c>Metalama.Framework.CompilerExtensions</c> from a single source file,
/// because the second assembly can reference nothing: it embeds and extracts the first one.
/// </summary>
public sealed class ProcessKindTests
{
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
            Enum.GetNames( typeof(CompilerExtensionsProcessKind) ).OrderBy( n => n, StringComparer.Ordinal );

        Assert.Equal( backstageNames, compilerExtensionsNames );
    }
}
