// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Extensions.DependencyInjection.AspectTests.Aspect.LazyRequired;

// <target>
public class TargetClass
{
    [Dependency( IsLazy = true )]
    private readonly IFormatProvider _formatProvider;

    public TargetClass() { }

    public TargetClass( int x, IFormatProvider existingParameter ) { }
}