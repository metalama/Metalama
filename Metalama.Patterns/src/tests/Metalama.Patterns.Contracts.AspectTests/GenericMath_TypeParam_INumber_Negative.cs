// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Numerics;

namespace Metalama.Patterns.Contracts.AspectTests.GenericMath_TypeParam_INumber_Negative;

internal class C
{
    public void M<T>( [Negative] T value )
        where T : INumber<T> { }
}
