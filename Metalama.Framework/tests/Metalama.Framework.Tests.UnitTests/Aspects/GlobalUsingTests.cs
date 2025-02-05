// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Aspects;

public sealed class GlobalUsingTests : AspectTestBase
{
    private const string _globalUsings = @"
global using SEXCEPT = global::System.Exception;
global using OMA = Metalama.Framework.Aspects.OverrideMethodAspect;
global using static global::System.Console;
";

    private const string _code = @"

using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

static class Program
{

    // <target>
    [Log]
    static int Add(int a, int b)
    {
        if (a == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(a));
        }
        return a + b;
    }
}


public class LogAttribute : OMA
{
    public override dynamic? OverrideMethod()
    {
        WriteLine(meta.Target.Method.ToDisplayString() + "": started."");
        try
        {
            var result = meta.Proceed();
            WriteLine(meta.Target.Method.ToDisplayString() + "": succeeded."");
            return result;
        }
        catch (SEXCEPT ex)
        {
        WriteLine(meta.Target.Method.ToDisplayString() + "": failed due "" + ex.Message);
        throw;
        }
    }
}
";

    [Fact]
    public async Task SameSyntaxTreeAsync()
    {
        using var testContext = this.CreateTestContext();
        var result = await CompileAsync( testContext, _globalUsings + _code );
        Assert.True( result.IsSuccessful );
        Assert.Empty( result.Value.ResultingCompilation.Compilation.GetDiagnostics().Where( d => d.Severity >= DiagnosticSeverity.Warning ) );
    }

    [Fact]
    public async Task DifferentSyntaxTreeAsync()
    {
        using var testContext = this.CreateTestContext();
        var result = await CompileAsync( testContext, new Dictionary<string, string>() { ["usings.cs"] = _globalUsings, ["code.cs"] = _code } );
        Assert.True( result.IsSuccessful );
        Assert.Empty( result.Value.ResultingCompilation.Compilation.GetDiagnostics().Where( d => d.Severity >= DiagnosticSeverity.Warning ) );
    }
}