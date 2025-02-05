// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

namespace Metalama.Framework.Tests.UnitTests.Aspects;

public sealed class XmlDocTriviaTests : AspectTestBase
{
    [Theory]
    [InlineData( CodeFormattingOptions.None )]
    [InlineData( CodeFormattingOptions.Default )]
    [InlineData( CodeFormattingOptions.Formatted )]
    public async Task IntroduceAttribute( CodeFormattingOptions codeFormattingOptions )
    {
        using var testContext = this.CreateTestContext( new TestContextOptions() { CodeFormattingOptions = codeFormattingOptions } );

        const string code = @"
using System;
using System.Linq;
using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects; 
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;

public class TestAspect : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        foreach( var member in builder.Target.Members() )
        {
            builder.Advice.IntroduceAttribute(
                member,
                AttributeConstruction.Create(
                    ((INamedType)TypeFactory.GetType(typeof(TestAttribute))).Constructors.Single()),
                OverrideStrategy.Override);
        }
    }
}

public class TestAttribute : Attribute { }

[TestAspect]
public class TestClass
{
    /// <summary/>
    public string? Field;

    /// <summary/>
    public string? Property { get; set; }

    /// <summary/>
    public event EventHandler? EventField;

    /// <summary/>
    public event EventHandler? Event
    {
        add {}
        remove {}
    }

    /// <summary/>
    public string? Method() => null;
}";

        var result = await CompileAsync( testContext, code );

        Assert.Empty( result.Value.ResultingCompilation.Compilation.GetDiagnostics().Where( d => d.Id is not "CS0067" and not "CS8019" ) );

        var emitResult = result.Value.ResultingCompilation.Compilation.Emit( new MemoryStream() );

        Assert.Empty( emitResult.Diagnostics.Where( d => d.Id is not "CS0067" and not "CS8019" ) );

        var transformedProperty = result.Value.ResultingCompilation.SyntaxTrees.SelectAsReadOnlyCollection( x => x.Value.GetRoot() )
            .SelectMany( x => x.DescendantNodes() )
            .Single( x => x is TypeDeclarationSyntax { Identifier.Text: "TestClass" } )
            .NormalizeWhitespace()
            .ToString();

        var expectedTransformedProperty =
            codeFormattingOptions != CodeFormattingOptions.Formatted
                ? @"
[TestAspect]
public class TestClass
{
    /// <summary/>
    [global::TestAttribute]
    public string? Field;
    /// <summary/>
    [global::TestAttribute]
    public string? Property { get; set; }

    /// <summary/>
    [global::TestAttribute]
    public event EventHandler? EventField;
    /// <summary/>
    [global::TestAttribute]
    public event EventHandler? Event
    {
        add
        {
        }

        remove
        {
        }
    }

    /// <summary/>
    [global::TestAttribute]
    public string? Method() => null;
}"
                : @"
[TestAspect]
public class TestClass
{
    /// <summary/>
    [Test]
    public string? Field;
    /// <summary/>
    [Test]
    public string? Property { get; set; }

    /// <summary/>
    [Test]
    public event EventHandler? EventField;
    /// <summary/>
    [Test]
    public event EventHandler? Event
    {
        add
        {
        }

        remove
        {
        }
    }

    /// <summary/>
    [Test]
    public string? Method() => null;
}";

        AssertEx.EolInvariantEqual( expectedTransformedProperty.Trim(), transformedProperty.Trim() );
    }
}