// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Microsoft.Extensions.Logging;

namespace Metalama.Extensions.DependencyInjection.AspectTests.Advice.Logger;

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
public class Foo
{
    [TheAspect]
    public void Bar() { }

    [TheAspect]
    public void Bar2() { }
}