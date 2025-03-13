// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Patterns.Immutability;

[CompileTime]
internal static class ImmutabilityDiagnostics
{
    // Reserved range 5020-5039

    public static DiagnosticDefinition<IField> FieldMustBeReadOnly { get; } = new(
        "LAMA5020",
        Severity.Warning,
        "The '{0}' field must be read-only because of the [Immutable] aspect." );

    public static DiagnosticDefinition<IProperty> PropertyMustHaveNoSetter { get; } = new(
        "LAMA5021",
        Severity.Warning,
        "The '{0}' property must not have a setter because of the [Immutable] aspect." );

    public static DiagnosticDefinition<(IFieldOrProperty FieldOrProperty, DeclarationKind DeclarationKind)> FieldOrPropertyMustBeOfDeeplyImmutableType { get; } = new(
        "LAMA5022",
        Severity.Warning,
        "The type of the '{0}' {1} must be deeply immutable." );
}