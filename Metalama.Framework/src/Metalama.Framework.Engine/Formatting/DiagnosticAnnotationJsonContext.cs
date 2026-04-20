// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Metalama.Framework.Engine.Formatting;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull )]
[JsonSerializable( typeof(DiagnosticAnnotation) )]
[JsonSerializable( typeof(DiagnosticSeverity) )]
internal partial class DiagnosticAnnotationJsonContext : JsonSerializerContext { }