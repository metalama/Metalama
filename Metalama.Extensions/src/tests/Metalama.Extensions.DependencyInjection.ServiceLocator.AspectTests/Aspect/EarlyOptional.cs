// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Extensions.DependencyInjection.DotNet.Tests.Aspect.EarlyOptional;

// ReSharper disable UnusedParameter.Local

// <target>
public class TargetClass
{
    [Dependency( IsRequired = false )]
    private readonly IFormatProvider? _formatProvider;

    public TargetClass() { }

    public TargetClass( int x, IFormatProvider existingParameter ) { }
}