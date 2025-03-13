// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Patterns.Contracts;

[assembly: AspectOrder(
    AspectOrderDirection.RunTime,
    "Metalama.Patterns.Contracts." + nameof(SuspendInvariantsAttribute) + ":*",
    "Metalama.Patterns.Contracts." + nameof(CheckInvariantsAspect) + ":*",
    "Metalama.Patterns.Contracts." + nameof(InvariantAttribute) + ":*",
    "Metalama.Patterns.Contracts." + nameof(NotNullAttribute) + ":" + ContractAspect.BuildLayer,
    "Metalama.Patterns.Contracts." + nameof(RequiredAttribute) + ":" + ContractAspect.BuildLayer,
    "Metalama.Patterns.Contracts." + nameof(StringLengthAttribute) + ":" + ContractAspect.BuildLayer,
    "Metalama.Patterns.Contracts." + nameof(NotEmptyAttribute) + ":" + ContractAspect.BuildLayer,
    "Metalama.Patterns.Contracts." + nameof(RegularExpressionAttribute) + ":" + ContractAspect.BuildLayer,
    "Metalama.Patterns.Contracts." + nameof(PhoneAttribute) + ":" + ContractAspect.BuildLayer,
    "Metalama.Patterns.Contracts." + nameof(EmailAttribute) + ":" + ContractAspect.BuildLayer,
    "Metalama.Patterns.Contracts." + nameof(UrlAttribute) + ":" + ContractAspect.BuildLayer,
    "Metalama.Patterns.Contracts." + nameof(CreditCardAttribute) + ":" + ContractAspect.BuildLayer )]