// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Aspects;

public sealed class AdviceFactoryScenarioTests : UnitTestClass
{
    private async Task<ImmutableArray<Diagnostic>> RunAsync( string code, ExecutionScenario scenario )
    {
        using var testContext = this.CreateTestContext();

        var compilation = testContext.CreateCSharpCompilation( code );

        using var pipeline = new CompileTimeAspectPipeline( testContext.ServiceProvider, scenario );
        var diagnostics = new DiagnosticBag();

        await pipeline.ExecuteAsync( diagnostics.Report, null, compilation, ImmutableArray<ManagedResource>.Empty );

        return diagnostics.ToImmutableArray();
    }

    private static bool ContainsOutcome( ImmutableArray<Diagnostic> diagnostics, string outcome )
        => diagnostics.Any( d => d.GetMessage().Contains( $"Outcome={outcome}", StringComparison.Ordinal ) );

    private const string _overrideMethodAspect = @"
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using System.Linq;

public class TestAspect : TypeAspect
{
    private static readonly DiagnosticDefinition<string> _diagnostic
        = new(""TST001"", Severity.Warning, ""Outcome={0}"");

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var method = builder.Target.Methods.First(m => m.Name == ""M"");
        var result = builder.Advice.Override(method, nameof(Template));
        builder.Diagnostics.Report(_diagnostic.WithArguments(result.Outcome.ToString()), method);
    }

    [Template]
    public dynamic? Template() => meta.Proceed();
}

[TestAspect]
public class C
{
    public void M() {}
}
";

    [Fact]
    public async Task Override_OnRegularMethod_SkippedAtDesignTime()
    {
        var diagnostics = await this.RunAsync( _overrideMethodAspect, ExecutionScenario.DesignTime );

        Assert.True( ContainsOutcome( diagnostics, "Skipped" ), "Expected Outcome=Skipped at design time." );
    }

    [Fact]
    public async Task Override_OnRegularMethod_NotSkippedAtCompileTime()
    {
        var diagnostics = await this.RunAsync( _overrideMethodAspect, ExecutionScenario.CompileTime );

        Assert.False( ContainsOutcome( diagnostics, "Skipped" ), "Did not expect Outcome=Skipped at compile time." );
        Assert.True( ContainsOutcome( diagnostics, "Default" ), "Expected Outcome=Default at compile time." );
    }

    private const string _overridePartialMethodAspect = @"
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using System.Linq;

public class TestAspect : TypeAspect
{
    private static readonly DiagnosticDefinition<string> _diagnostic
        = new(""TST001"", Severity.Warning, ""Outcome={0}"");

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var method = builder.Target.Methods.First(m => m.Name == ""M"");
        var result = builder.Advice.Override(method, nameof(Template));
        builder.Diagnostics.Report(_diagnostic.WithArguments(result.Outcome.ToString()), method);
    }

    [Template]
    public void Template() { meta.Proceed(); }
}

[TestAspect]
public partial class C
{
    partial void M();
}
";

    [Fact]
    public async Task Override_OnPartialMethodWithoutImplementation_NotSkippedAtDesignTime()
    {
        // Override of a partial member without implementation also emits SetHasImplementationTransformation
        // (CompileTimeOnly). It's filtered out of the design-time syntax tree, but it IS applied to
        // MutableCompilation, and a subsequent aspect that reads HasImplementation observes the post-override
        // value. Skipping here would diverge the model seen by the next aspect.
        var diagnostics = await this.RunAsync( _overridePartialMethodAspect, ExecutionScenario.DesignTime );

        Assert.False( ContainsOutcome( diagnostics, "Skipped" ), "Override of a partial member without implementation must run at design time." );
    }

    private const string _overrideFieldAspect = @"
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using System.Linq;

public class TestAspect : TypeAspect
{
    private static readonly DiagnosticDefinition<string> _diagnostic
        = new(""TST001"", Severity.Warning, ""Outcome={0}"");

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var field = builder.Target.Fields.First(f => f.Name == ""_x"");
        var result = builder.Advice.Override(field, nameof(GetTemplate));
        builder.Diagnostics.Report(_diagnostic.WithArguments(result.Outcome.ToString()), field);
    }

    [Template]
    public dynamic? GetTemplate() => meta.Proceed();
}

[TestAspect]
public class C
{
    public int _x;
}
";

    [Fact]
    public async Task Override_OnField_NotSkippedAtDesignTime()
    {
        // Override of a field promotes it to a property via PromoteFieldTransformation (CompileTimeOnly).
        // The promotion is filtered out of the design-time syntax tree, but it IS applied to MutableCompilation,
        // so a subsequent aspect would see the property where there was a field. The advice must still run.
        var diagnostics = await this.RunAsync( _overrideFieldAspect, ExecutionScenario.DesignTime );

        Assert.False( ContainsOutcome( diagnostics, "Skipped" ), "Override of a field must run at design time." );
    }

    private const string _overrideConstructorAspect = @"
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using System.Linq;

public class TestAspect : TypeAspect
{
    private static readonly DiagnosticDefinition<string> _diagnostic
        = new(""TST001"", Severity.Warning, ""Outcome={0}"");

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var ctor = builder.Target.Constructors.First();
        var result = builder.Advice.Override(ctor, nameof(Template));
        builder.Diagnostics.Report(_diagnostic.WithArguments(result.Outcome.ToString()), ctor);
    }

    [Template]
    public void Template() { meta.Proceed(); }
}

[TestAspect]
public class C
{
    public C() {}
}
";

    [Fact]
    public async Task Override_OnConstructor_SkippedAtDesignTime()
    {
        var diagnostics = await this.RunAsync( _overrideConstructorAspect, ExecutionScenario.DesignTime );

        Assert.True( ContainsOutcome( diagnostics, "Skipped" ), "Expected Outcome=Skipped at design time." );
    }

    private const string _addContractAspect = @"
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using System.Linq;

public class TestAspect : TypeAspect
{
    private static readonly DiagnosticDefinition<string> _diagnostic
        = new(""TST001"", Severity.Warning, ""Outcome={0}"");

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var method = builder.Target.Methods.First(m => m.Name == ""M"");
        var parameter = method.Parameters[0];
        var result = builder.Advice.AddContract(parameter, nameof(Template));
        builder.Diagnostics.Report(_diagnostic.WithArguments(result.Outcome.ToString()), parameter);
    }

    [Template]
    public void Template(dynamic? value) {}
}

[TestAspect]
public class C
{
    public void M(int x) {}
}
";

    [Fact]
    public async Task AddContract_OnParameter_SkippedAtDesignTime()
    {
        var diagnostics = await this.RunAsync( _addContractAspect, ExecutionScenario.DesignTime );

        Assert.True( ContainsOutcome( diagnostics, "Skipped" ), "Expected Outcome=Skipped at design time." );
    }

    [Fact]
    public async Task AddContract_OnParameter_NotSkippedAtCompileTime()
    {
        var diagnostics = await this.RunAsync( _addContractAspect, ExecutionScenario.CompileTime );

        Assert.False( ContainsOutcome( diagnostics, "Skipped" ), "Did not expect Outcome=Skipped at compile time." );
    }

    private const string _addInitializerOnConstructorAspect = @"
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using System.Linq;

public class TestAspect : TypeAspect
{
    private static readonly DiagnosticDefinition<string> _diagnostic
        = new(""TST001"", Severity.Warning, ""Outcome={0}"");

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var ctor = builder.Target.Constructors.First();
        var result = builder.Advice.AddInitializer(ctor, nameof(Template));
        builder.Diagnostics.Report(_diagnostic.WithArguments(result.Outcome.ToString()), ctor);
    }

    [Template]
    public void Template() {}
}

[TestAspect]
public class C
{
    public C() {}
}
";

    [Fact]
    public async Task AddInitializer_OnConstructor_SkippedAtDesignTime()
    {
        var diagnostics = await this.RunAsync( _addInitializerOnConstructorAspect, ExecutionScenario.DesignTime );

        Assert.True( ContainsOutcome( diagnostics, "Skipped" ), "Expected Outcome=Skipped at design time." );
    }

    private const string _introduceMethodAspect = @"
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

public class TestAspect : TypeAspect
{
    private static readonly DiagnosticDefinition<string> _diagnostic
        = new(""TST001"", Severity.Warning, ""Outcome={0}"");

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var result = builder.Advice.IntroduceMethod(builder.Target, nameof(Template));
        builder.Diagnostics.Report(_diagnostic.WithArguments(result.Outcome.ToString()), builder.Target);
    }

    [Template]
    public void Template() {}
}

[TestAspect]
public class C
{
}
";

    [Fact]
    public async Task IntroduceMethod_NotSkippedAtDesignTime()
    {
        // IntroduceMethod produces an Always-observable transformation; it must run regardless of scenario.
        var diagnostics = await this.RunAsync( _introduceMethodAspect, ExecutionScenario.DesignTime );

        Assert.False( ContainsOutcome( diagnostics, "Skipped" ), "IntroduceMethod must not be skipped at design time." );
    }
}
