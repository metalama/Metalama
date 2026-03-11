// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;

namespace Metalama.Framework.Engine.Aspects;

internal partial class EligibilityHelper
{
    private sealed class DelegateParameterEligibilityRule : IEligibilityRule<IDeclaration>
    {
        public static DelegateParameterEligibilityRule Instance { get; } = new();

        private DelegateParameterEligibilityRule() { }

        public EligibleScenarios GetEligibility( IDeclaration obj )
            => obj.DeclarationKind == DeclarationKind.Parameter
               && obj is IParameter { DeclaringMember: IMethod { MethodKind: MethodKind.DelegateInvoke } }
                ? EligibleScenarios.None
                : EligibleScenarios.All;

        public FormattableString? GetIneligibilityJustification( EligibleScenarios requestedEligibility, IDescribedObject<IDeclaration> describedObject )
            => $"the declaring member of {describedObject} must not be a delegate member";
    }

    private sealed class DelegateTypeEligibilityRule : IEligibilityRule<IDeclaration>
    {
        public static DelegateTypeEligibilityRule Instance { get; } = new();

        private DelegateTypeEligibilityRule() { }

        public EligibleScenarios GetEligibility( IDeclaration obj )
            => obj is INamedType { TypeKind: TypeKind.Delegate }
                ? EligibleScenarios.None
                : EligibleScenarios.All;

        public FormattableString? GetIneligibilityJustification( EligibleScenarios requestedEligibility, IDescribedObject<IDeclaration> describedObject )
            => $"{describedObject} must not be a delegate";
    }
}
