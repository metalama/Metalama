// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Aspects;

public sealed class FormatSensitiveTests : AspectTestBase
{
    [Fact]
    public async Task CompileTimeSingleStatementUnderRunTimeIfAsync()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System;
using System.Linq;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

public class PropOverride : OverrideFieldOrPropertyAspect
{
    public override dynamic? OverrideProperty
    {
        get
        {
            return meta.Proceed()?.ToUpper();
        }
        set
        {
            // Here we are trying different kinds of compile-time statements.
            if (meta.RunTime(true))
                meta.Proceed();

            if (meta.RunTime(true))
                _ = meta.Proceed();

            if (meta.RunTime(true))
                meta.InsertComment( ""x"" );

        if (meta.RunTime(true))
            if (meta.Target.Declaration != null)
            {
                meta.Proceed();
            }
    }
}
}

public class TestClass
{
    [PropOverride]
    public string Prop132 { get; set; }
}
";

        var result = await CompileAsync( testContext, code );

        var transformedProperty = result.Value.ResultingCompilation.SyntaxTrees.SelectAsReadOnlyCollection( x => x.Value.GetRoot() )
            .SelectMany( x => x.DescendantNodes() )
            .Single( x => x is PropertyDeclarationSyntax { Identifier.Text: "Prop132" } )
            .NormalizeWhitespace()
            .ToString();

        const string expectedTransformedProperty = @"
[PropOverride]
public string Prop132
{
    get
    {
        return (global::System.String)this._prop132.ToUpper();
    }

    set
    {
        if (true)
        {
            this._prop132 = value;
        }

        if (true)
            this._prop132 = value;
        if (true)
        {
        // x
        }

        if (true)
        {
            this._prop132 = value;
        }
    }
}";

        AssertEx.EolInvariantEqual( expectedTransformedProperty.Trim(), transformedProperty.Trim() );
    }
}