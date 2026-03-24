// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.CompileTimeParamsInRunTimeType;

/*
 * A method with compile-time parameters (IAttribute, TypedConstant) in an unannotated type
 * should report a scope error instead of being silently treated as run-time-only. (#787)
 */

internal static class AttributeExtensions
{
    public static bool TryGetNamedArgument( this IAttribute attribute, string name, out TypedConstant value )
    {
        value = default;

        return false;
    }
}
