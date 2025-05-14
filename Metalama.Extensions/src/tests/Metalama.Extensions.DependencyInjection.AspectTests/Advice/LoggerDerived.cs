// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Microsoft.Extensions.Logging;

namespace Metalama.Extensions.DependencyInjection.AspectTests.Advice.LoggerDerived;

public class TheAspect : OverrideMethodAspect
{
    [IntroduceDependency]
    private readonly ILogger _logger;

    public override dynamic? OverrideMethod()
    {
        this._logger.LogTrace( $"Starting {meta.Target.Method}" );

        return meta.Proceed();
    }
}

// <target>
public class BaseClass
{
    [TheAspect]
    public void Bar() { }

    [TheAspect]
    public void Bar2() { }
}

// <target>
public class DerivedClass : BaseClass
{
    [TheAspect]
    public void Foo() { }
}