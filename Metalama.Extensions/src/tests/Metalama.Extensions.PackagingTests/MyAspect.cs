// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Extensions.DependencyInjection;
using Metalama.Extensions.Metrics;
using Metalama.Framework.Aspects;
using Metalama.Framework.Metrics;

namespace Metalama.Extensions.PackagingTests;

public class MyAspect : OverrideMethodAspect
{
    [IntroduceDependency]
    private readonly TextWriter _textWriter;
    
    public override dynamic? OverrideMethod()
    {
        var statementCount = meta.Target.Method.Metrics().Get<StatementsCount>();
        this._textWriter.WriteLine($"Method '{meta.Target.Method.ToDisplayString(  )}' has {statementCount.Value} statement(s) plus this one.");

        return meta.Proceed();
    }
}