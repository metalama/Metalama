// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Metalama.Framework.ConfigurationFiles;

/// <summary>
/// Source-generated JSON serialization context for Framework-specific configuration types.
/// This context is designed to be used together with BackstageJsonContext via TypeInfoResolverChain
/// to provide serialization support for both Backstage and Framework types.
/// </summary>
/// <remarks>
/// This context only includes Framework-specific types. Backstage types (TelemetryConfiguration,
/// LicensingConfiguration, etc.) are provided by BackstageJsonContext in the resolver chain.
/// </remarks>
[JsonSourceGenerationOptions( WriteIndented = true )]
[JsonSerializable( typeof(UserDiagnosticsConfiguration) )]
[JsonSerializable( typeof(UserDiagnosticRegistration) )]
[JsonSerializable( typeof(TestRunnerOptions) )]

// ImmutableDictionary and ImmutableHashSet types for UserDiagnosticsConfiguration
[JsonSerializable( typeof(ImmutableDictionary<string, UserDiagnosticRegistration>) )]
[JsonSerializable( typeof(ImmutableHashSet<string>) )]

// Dictionary and HashSet types needed for deserialization
[JsonSerializable( typeof(Dictionary<string, UserDiagnosticRegistration>) )]
[JsonSerializable( typeof(HashSet<string>) )]
[UsedImplicitly]
public partial class FrameworkConfigurationJsonContext : JsonSerializerContext { }