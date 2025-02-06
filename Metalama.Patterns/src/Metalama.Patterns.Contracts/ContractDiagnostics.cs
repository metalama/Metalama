// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Patterns.Contracts.Numeric;

namespace Metalama.Patterns.Contracts;

[CompileTime]
internal static class ContractDiagnostics
{
    // Reserved range: 5000-5019

    public static DiagnosticDefinition<(IDeclaration Declaration, string TargetBasicType, string AspectType, NumericRange Range)> RangeCannotBeApplied { get; }
        = new(
            "LAMA5001",
            Severity.Error,
            "The [{2}] contract cannot be applied to '{0}' because the value range {3} cannot be satisfied by the type {1}.",
            $"The Range contract cannot be applied because the range cannot be satisfied by the type of the target declaration.",
            "Metalama.Patterns.Contracts" );

    public static DiagnosticDefinition<(IDeclaration Declaration, string AspectType)> NotNullableOnNullable { get; }
        = new(
            "LAMA5002",
            Severity.Warning,
            "The [{1}] contract has been applied to '{0}', but its type is nullable.",
            $"A non-nullable contract has been applied to a declaration of nullable type.",
            "Metalama.Patterns.Contracts" );

    public static DiagnosticDefinition<(IDeclaration Declaration, string AspectType)> ContractRedundant { get; }
        = new(
            "LAMA5003",
            Severity.Warning,
            "The [{1}] contract is redundant because the [NotNull] contract is automatically added by a fabric.",
            $"The non-nullable contract is redundant because it is automatically added by a fabric.",
            "Metalama.Patterns.Contracts" );

    public static DiagnosticDefinition<(IMethod Method, INamedType Type)> SuspensionNotSupported { get; }
        = new(
            "LAMA5004",
            Severity.Error,
            $"The [{nameof(SuspendInvariantsAttribute)}] aspect cannot be applied to method '{{0}}' because the {nameof(ContractOptions.IsInvariantSuspensionSupported)} option is not set for the type '{{1}}'.",
            $"The [${nameof(SuspendInvariantsAttribute)}] aspect cannot be applied to the method because the {nameof(ContractOptions.IsInvariantSuspensionSupported)} option is not set for the its declaring type." );

    public static DiagnosticDefinition<(IMethod Method, INamedType Type)> SuspensionRedundant { get; }
        = new(
            "LAMA5005",
            Severity.Warning,
            $"The [{nameof(SuspendInvariantsAttribute)}] aspect on method '{{0}}' is redundant the type '{{1}}' does not contain any invariants.",
            $"The [${nameof(SuspendInvariantsAttribute)}] aspect is redundant because the type does not contain any invariants." );

    public static DiagnosticDefinition<(IDeclaration Declaration, string TargetBasicType, string AspectType, NumericRange Range)> RangeIsRedundant { get; }
        = new(
            "LAMA5006",
            Severity.Warning,
            "The [{2}] contract is redundant on '{0}' because the value range {3} is always satisfied by the type {1}.",
            $"The Range contract is redundant because the range is always satisfied by the type of the target declaration.",
            "Metalama.Patterns.Contracts" );

    public static DiagnosticDefinition<(IDeclaration Declaration, string OldName, string NewName1, string NewName2, InequalityStrictness Strictness)>
        AttributeMeaningIsAmbiguous { get; }
        = new(
            "LAMA5007",
            Severity.Warning,
            "The meaning of the [{1}] attribute on {0} is ambiguous because the inequality strictness is not specified. " +
            "It is now interpreted as {4}, which is non-standard, and this behavior might be changed in the future. " +
            $"Use either [{{2}}] or [{{3}}] or specify the {nameof(ContractOptions.DefaultInequalityStrictness)} property in {nameof(ContractOptions)} " +
            $"using the {nameof(ContractConfigurationExtensions.ConfigureContracts)} fabric extension method.",
            $"The meaning of the inequality contract is ambiguous.",
            "Metalama.Patterns.Contracts" );
}