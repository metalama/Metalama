// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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