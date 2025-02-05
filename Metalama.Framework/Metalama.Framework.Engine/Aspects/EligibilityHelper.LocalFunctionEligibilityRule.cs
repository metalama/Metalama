// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Engine.Aspects;

internal partial class EligibilityHelper
{
    private sealed class LocalFunctionEligibilityRule : IEligibilityRule<IDeclaration>
    {
        public static LocalFunctionEligibilityRule Instance { get; } = new();

        private LocalFunctionEligibilityRule() { }

        public EligibleScenarios GetEligibility( IDeclaration obj )
            => obj is IMethod { MethodKind: MethodKind.LocalFunction or MethodKind.Lambda } ? EligibleScenarios.None : EligibleScenarios.All;

        public FormattableString? GetIneligibilityJustification( EligibleScenarios requestedEligibility, IDescribedObject<IDeclaration> describedObject )
            => ((IMethod) describedObject.Object).MethodKind switch
            {
                MethodKind.LocalFunction => $"it is a local function",
                MethodKind.Lambda => $"it is a lambda",
                _ => null
            };
    }

    private sealed class LocalFunctionParameterEligibilityRule : IEligibilityRule<IDeclaration>
    {
        public static LocalFunctionParameterEligibilityRule Instance { get; } = new();

        private LocalFunctionParameterEligibilityRule() { }

        public EligibleScenarios GetEligibility( IDeclaration obj )
            => obj is IParameter { DeclaringMember: IMethod { MethodKind: MethodKind.LocalFunction or MethodKind.Lambda } }
                ? EligibleScenarios.None
                : EligibleScenarios.All;

        public FormattableString? GetIneligibilityJustification( EligibleScenarios requestedEligibility, IDescribedObject<IDeclaration> describedObject )
            => ((IMethod) ((IParameter) describedObject.Object).DeclaringMember).MethodKind switch
            {
                MethodKind.LocalFunction => $"it is a parameter of a local function",
                MethodKind.Lambda => $"it is a parameter of a lambda",
                _ => null
            };
    }
}