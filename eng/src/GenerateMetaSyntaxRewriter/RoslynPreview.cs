// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Metalama.Framework.GenerateMetaSyntaxRewriter;

/// <summary>
/// Exposes the settings of the experimental Roslyn opt-in that the generator reads.
/// </summary>
/// <remarks>
/// The settings are declared in <c>eng/RoslynPreview.props</c> as MSBuild properties, and
/// <c>eng/src/Directory.Build.props</c> writes them into the assembly metadata of this program, because MSBuild
/// builds the program and <c>Build.ps1</c> runs it later, when no MSBuild property is visible any more. See issue
/// #1935.
/// </remarks>
internal static class RoslynPreview
{
    /// <summary>
    /// The key under which <c>eng/src/Directory.Build.props</c> declares <see cref="KeptGrammarFeatures"/>.
    /// </summary>
    private const string _keptGrammarFeaturesKey = "RoslynPreviewKeptGrammarFeatures";

    /// <summary>
    /// Gets the experimental Roslyn features whose grammar declarations the generator keeps, identified by the value
    /// of the <c>ExperimentalUrl</c> attribute that the <c>Syntax-*.xml</c> files carry. The set is empty when the
    /// opt-in is disabled, and an empty set removes every experimental declaration, which is the behaviour of the
    /// repository before issue #1935.
    /// </summary>
    public static IReadOnlySet<string> KeptGrammarFeatures { get; } = ReadKeptGrammarFeatures();

    /// <summary>
    /// Reads the value that <c>eng/src/Directory.Build.props</c> writes into the assembly metadata under
    /// <see cref="_keptGrammarFeaturesKey"/>, and splits it into a set. An absent value yields an empty set,
    /// which is the disabled state of the opt-in.
    /// </summary>
    private static IReadOnlySet<string> ReadKeptGrammarFeatures()
    {
        var value = typeof(RoslynPreview).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault( a => a.Key == _keptGrammarFeaturesKey )
            ?.Value;

        return new HashSet<string>(
            value?.Split( [';'], StringSplitOptions.RemoveEmptyEntries ) ?? [],
            StringComparer.OrdinalIgnoreCase );
    }
}
