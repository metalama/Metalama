// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;

namespace Metalama.Extensions.DependencyInjection.DotNet.Tests.Aspect.LazyRequired;

// <target>
public class TargetClass
{
    [Dependency( IsLazy = true )]
    private readonly IFormatProvider _formatProvider;

    [Dependency( IsLazy = true )]
    private readonly ILogger _logger;

    public TargetClass() { }

    public TargetClass( int x, IFormatProvider existingParameter ) { }
}