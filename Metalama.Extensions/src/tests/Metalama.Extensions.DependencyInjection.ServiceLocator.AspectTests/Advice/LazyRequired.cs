// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Extensions.DependencyInjection;
using Metalama.Extensions.DependencyInjection.DotNet.Tests.Advice.LazyRequired;
using Metalama.Framework.Aspects;

// ReSharper disable UnusedParameter.Local

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(DependencyAttribute), typeof(MyAspect) )]

namespace Metalama.Extensions.DependencyInjection.DotNet.Tests.Advice.LazyRequired;

public class MyAspect : TypeAspect
{
    [IntroduceDependency( IsLazy = true )]
    private readonly IFormatProvider _formatProvider;
}

// <target>
[MyAspect]
public class TargetClass
{
    public TargetClass() { }

    public TargetClass( int x, IFormatProvider existingParameter ) { }
}