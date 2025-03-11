// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Extensions.DependencyInjection;
using Metalama.Extensions.DependencyInjection.DotNet.Tests.Advice.LazyOptional;
using Metalama.Framework.Aspects;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(DependencyAttribute), typeof(MyAspect) )]

namespace Metalama.Extensions.DependencyInjection.DotNet.Tests.Advice.LazyOptional;

public class MyAspect : TypeAspect
{
    [IntroduceDependency( IsLazy = true, IsRequired = false )]
    private readonly IFormatProvider _formatProvider;
}

// <target>
[MyAspect]
public class TargetClass
{
    public TargetClass() { }

    public TargetClass( int x, IFormatProvider existingParameter ) { }
}