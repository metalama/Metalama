// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Contracts.AspectTests.Diagnostics;

public class NotNull_Ineligible_GenericStruct
{
    public void Method<T>( [NotNull] T x )
        where T : struct { }
}