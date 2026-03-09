// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.EnumNestedInRunTimeOrCompileTimeType;

/*
 * An enum nested inside a [RunTimeOrCompileTime] type should be usable by members of that type. (#627)
 */

// <target>
[RunTimeOrCompileTime]
class C
{
    enum E
    {
        A
    }

    void M( E e ) { }
}
