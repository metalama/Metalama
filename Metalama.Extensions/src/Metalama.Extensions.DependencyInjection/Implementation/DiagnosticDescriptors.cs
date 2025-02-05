// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Extensions.DependencyInjection.Implementation;

[CompileTime]
internal static class DiagnosticDescriptors
{
    private const string _category = "Metalama.Extensions.DependencyInjection";

    // Diagnostics need to be declared in an aspect or fabric at the moment.
    // Range: 0701-0749

    internal static readonly DiagnosticDefinition<(IType DependencyType, INamedType TargetType)>
        NoDependencyInjectionFrameworkRegistered = new(
            "LAMA0701",
            Severity.Error,
            "No dependency injection framework can handle the dependency '{0}' in type '{1}'.",
            "No dependency injection framework has been registered.",
            _category );

    internal static readonly DiagnosticDefinition<(IType DependencyType, INamedType TargetType)>
        NoSuitableDependencyInjectionFramework = new(
            "LAMA0702",
            Severity.Error,
            "None of the registered dependency injection frameworks can handle the dependency '{0}' in type '{1}'.",
            "None of the registered dependency injection frameworks can handle a dependency.",
            _category );

    internal static readonly DiagnosticDefinition<IDeclaration>
       AdviceUsedInNonTypeContext = new(
           "LAMA0703",
           Severity.Error,
           "Cannot use the [IntroduceDependency] advice because the target declaration '{0}' is not a type or a type member.",
           "Cannot use the [IntroduceDependency] advice because the target declaration is not a type or a type member.",
           _category );

    internal static readonly SuppressionDefinition NonNullableFieldMustContainValue = new( "CS8618" );

    internal static readonly SuppressionDefinition FieldIsNeverUsed = new( "CS0169" );

    internal static readonly SuppressionDefinition PrivateMemberIsUnused = new( "IDE0051" );
}