// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.ExplicitRunTimeMethodWithCompileTimeParams;

/*
 * A method explicitly marked [RunTime] with compile-time parameter types should NOT trigger
 * the implicit parameter-type inference LAMA0292. The method is explicitly run-time in a run-time type,
 * which is compatible. The LAMA0117 errors for referencing compile-time types in a run-time method
 * are still reported by the standard scope validation. (#787)
 */

internal static class C
{
    [RunTime]
    public static bool M( IAttribute attribute, string name, out TypedConstant value )
    {
        value = default;

        return false;
    }
}
