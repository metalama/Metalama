// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Contracts.AspectTests.CrossAssemblyObjectRange;

public interface IValidated
{
    [return: StrictlyPositive]
    public object M( [NonNegative] object a, [Range( 0, 100 )] object b, [LessThanOrEqual( 100 )] out object c );
}