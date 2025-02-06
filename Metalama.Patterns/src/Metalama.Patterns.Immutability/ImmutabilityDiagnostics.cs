// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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