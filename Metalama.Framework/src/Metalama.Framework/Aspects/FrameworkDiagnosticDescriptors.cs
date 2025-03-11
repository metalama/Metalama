// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Aspects;

// Range: 0700, 0750-0799

public static class FrameworkDiagnosticDescriptors
{
    internal static readonly DiagnosticDefinition<(string AspectType, DeclarationKind IntroducedDeclarationKind, DeclarationKind TargetDeclarationKind)>
        CannotUseIntroduceWithoutDeclaringType = new(
            "LAMA0700",
            "Cannot use [Introduce] in an aspect that is applied to a declaration that is neither a type nor a type member.",
            "The aspect '{0}' cannot introduce a {1} because it has been applied to a {2}, which is neither a type nor a type member.",
            "Metalama.Advices",
            Severity.Error );

    internal static readonly DiagnosticDefinition<(string AspectType, DeclarationKind IntroducedDeclarationKind, TypeKind TargetTypeKind)>
        CannotApplyAdviceOnTypeOrItsMembers = new(
            "LAMA0750",
            "Cannot use [Introduce] in an aspect that is applied to an unsupported type or its member.",
            "The aspect '{0}' cannot introduce a {1} because {2} is not a supported target type.",
            "Metalama.Advices",
            Severity.Error );

    internal static readonly DiagnosticDefinition<(IDeclaration InterfaceType, INamedType ImplementingType)>
        InternalImplementConstraint = new(
            "LAMA0751",
            "An interface cannot be implemented by a type because of the [InternalImplement] constraint.",
            "The interface '{0}' cannot be implemented by the type '{1}' because of the [InternalImplement] constraint.",
            "Metalama.Advices",
            Severity.Warning );
}