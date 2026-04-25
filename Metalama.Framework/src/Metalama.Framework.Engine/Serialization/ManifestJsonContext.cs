// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metalama.Framework.Engine.Serialization;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase )]
[JsonSerializable( typeof(CompileTimeProjectManifest) )]
[JsonSerializable( typeof(CompileTimeFileManifest) )]
[JsonSerializable( typeof(CompileTimeDiagnosticManifest) )]
[JsonSerializable( typeof(CompileTimeDiagnosticLocationManifest) )]
[JsonSerializable( typeof(TemplateProjectManifest) )]
[JsonSerializable( typeof(TemplateSymbolManifest) )]
[JsonSerializable( typeof(TemplateInfoManifest) )]
[JsonSerializable( typeof(DiagnosticManifest) )]
[JsonSerializable( typeof(ExecutionScope) )]
[JsonSerializable( typeof(TemplateAttributeType) )]
[JsonSerializable( typeof(DiagnosticSeverity) )]
[JsonSerializable( typeof(TextSpan) )]
[JsonSerializable( typeof(LinePositionSpan) )]
[JsonSerializable( typeof(IReadOnlyList<string>) )]
[JsonSerializable( typeof(IReadOnlyList<CompileTimeFileManifest>) )]
[JsonSerializable( typeof(IReadOnlyList<CompileTimeDiagnosticManifest>) )]
[JsonSerializable( typeof(IReadOnlyList<CompileTimeDiagnosticLocationManifest>) )]
[JsonSerializable( typeof(IReadOnlyList<TemplateSymbolManifest>) )]
[JsonSerializable( typeof(IReadOnlyDictionary<string, IReadOnlyList<TemplateSymbolManifest>>) )]
[JsonSerializable( typeof(ImmutableDictionary<string, string?>) )]
[JsonSerializable( typeof(IEnumerable<string>) )]
internal partial class ManifestJsonContext : JsonSerializerContext
{
    private static ManifestJsonContext? _indented;
    private static ManifestJsonContext? _compact;

    /// <summary>
    /// Gets the context configured for indented output (used for writing manifests).
    /// </summary>
    public static ManifestJsonContext Indented => _indented ??= new ManifestJsonContext( CreateOptions( writeIndented: true ) );

    /// <summary>
    /// Gets the context configured for compact output.
    /// </summary>

    public static ManifestJsonContext Compact => _compact ??= new ManifestJsonContext( CreateOptions( writeIndented: false ) );

    private static JsonSerializerOptions CreateOptions( bool writeIndented )
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        // Add custom converters
        options.Converters.Add( new LanguageVersionJsonConverter() );
        options.Converters.Add( new TextSpanJsonConverter() );
        options.Converters.Add( new LinePositionSpanJsonConverter() );
        options.Converters.Add( new NullableLinePositionSpanJsonConverter() );
        options.Converters.Add( new JsonStringEnumConverter() );

        return options;
    }
}