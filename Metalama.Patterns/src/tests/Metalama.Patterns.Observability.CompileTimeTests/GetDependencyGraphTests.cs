// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using FluentAssertions;
using Metalama.Framework.Code;
using Metalama.Patterns.Observability.Implementation;
using Metalama.Patterns.Observability.Implementation.DependencyAnalysis;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Patterns.Observability.CompileTimeTests;

public sealed class GetDependencyGraphTests : UnitTestClass
{
    public GetDependencyGraphTests( ITestOutputHelper testOutput ) : base( testOutput, false ) { }

    private static bool AlwaysTreatAsInpc( ITypeSymbol type ) => true;

    private static bool NeverTreatAsInpc( ITypeSymbol type ) => false;

    private static ObservableTypeInfo CreateDependencyGraph(
        TestContext context,
        INamedType type,
        Func<ISymbol, bool>? isConfiguredAsSafe = null,
        Action<string>? reportDiagnostic = null,
        Func<ITypeSymbol, bool>? treatAsImplementingInpc = null )
    {
        using ( context.WithExecutionContext( type.Compilation ) )
        {
            var assets = new Assets();

            return new DependencyGraphBuilder( assets )
                .GetDependencyGraph(
                    type,
                    new TestGraphBuildingContext(
                        type.Compilation,
                        assets,
                        isConfiguredAsSafe,
                        reportDiagnostic,
                        treatAsImplementingInpc: treatAsImplementingInpc ) );
        }
    }

    private static ICompilation CreateCompilation( TestContext testContext, string code )
        => testContext.CreateCompilation( code, additionalReferences: [MetadataReference.CreateFromFile( typeof(ObservableAttribute).Assembly.Location )] );

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void CommonPatterns()
    {
        // A general test of common class and expression patterns that should work.

        using var testContext = this.CreateTestContext();

        const string code = @"
class A
{
    public B A1 { get; set; }

    public int A2 { get; set; }

    public int A3 => this.A1.B1.C1.D1;

    public int A4 => this.A2;
    
    public int A5 => this.A1.B2;

    public int A6 => this.A2 + this.A1.B2;

    public int A7 => this.A3 + this.A4 + A5 + this.A6;

    public int A8 => this.A1.B1.C2;

    public int? A9 => A1?.B2;

    public int? A10 => this.A1?.B2;

    public int? A11 => (this.A1)?.B2;

    public int A12 => ++A1.B2;

    public int A13
    {
        get
        {
            A1.B2 = 99; // Write-only to A1.B2, should not be treated as a reference.
            return A1.B1.C2;
        }
    }
    
    // Demonstrate non-leaf access:
    public C A14 => A1.B1;

    public B A15 { get; set; }
}

class B
{
    public C B1 { get; set; }

    public int B2 { get; set; }
}

class C
{
    public D C1 { get; set; }

    public int C2 { get; set; }
}

class D
{
    public int D1 { get; set; }
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        const string expected = @"<root>
  A1
    B1 [ A14 ]
      C1
        D1 [ A3, A7 ]
      C2 [ A13, A8 ]
    B2 [ A10, A11, A12, A5, A6, A7, A9 ]
  A10
  A11
  A12
  A13
  A14
  A15
  A2 [ A4, A6, A7 ]
  A3 [ A7 ]
  A4 [ A7 ]
  A5 [ A7 ]
  A6 [ A7 ]
  A7
  A8
  A9
";

        diagnostics.Should().BeEmpty();
        result.ToString().Should().Be( NormalizeEOL( expected ) );

        this.TestOutput.WriteLine( result.ToString() );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void ExternalPropertyThenMethodWithReferencingArgThenMethodWithReferencingArg()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System;

public class A
{
    public int X { get; set; }

    public int Y { get; set; }

    public DateTime Z => DateTime.Now.AddTicks( X ).AddMinutes( Y );
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        const string expected = @"<root>
  X [ Z ]
  Y [ Z ]
  Z
";

        result.ToString().Should().Be( NormalizeEOL( expected ) );
        diagnostics.Should().BeEmpty();

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void VariableOfUnsafeType()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System;
using System.Collections.Generic;

public class A
{
    public int Y { get; set; }

    public int Z
    {
        get
        {
            var x = new List<int>();
            return x.Count + Y;
        }
    }
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  Y [ Z ]
  Z
";

        diagnostics.Should()
            .Equal( "LAMA5165@'x'" );

        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void VariableOfPrimitiveType()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System;
using System.Collections.Generic;

public class A
{
    public int X { get; set; }

    public int Y { get; set; }

    public int Z
    {
        get
        {
            var x = X;
            for ( int i = 0; i < 10; ++i )
            {
                x += i;
            }
            return x + Y;
        }
    }
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  X [ Z ]
  Y [ Z ]
  Z
";

        diagnostics.Should().BeEmpty();
        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void Const()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class A
{
    public int X { get; set; }

    public int Y
    {
        get
        {
            const int v = 123;
            return X + 123;
        }
    }
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  X [ Y ]
  Y
";

        diagnostics.Should().BeEmpty();
        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void PropertyReferencesSelf()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class A
{
    public int Y
    {
        get
        {
            // Y's reference to Y should not be recorded in the graph as it's not relevant.
            return Y + 1;
        }
    }
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  Y
";

        diagnostics.Should().BeEmpty();
        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void ArgumentNameColon()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class A
{
    public int X { get; set; }

    public int Y
    {
        get
        {
            // Don't be fooled by identifier 'arg1'            
            return Method( arg1: this.X );
        }
    }

    static int Method( int arg1 ) => arg1 * 2;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  X [ Y ]
  Y
";

        diagnostics.Should().BeEmpty();
        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void DateTime()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System;
public class A
{
    public int X { get; set; }

    public long Y => DateTime.Now.AddTicks( this.X ).Ticks;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  X [ Y ]
  Y
";

        diagnostics.Should().BeEmpty();
        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void TimeSpan()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System;
public class A
{
    public TimeSpan X { get; set; }

    public long Y => DateTime.Now.Add( this.X ).Ticks;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  X [ Y ]
  Y
";

        diagnostics.Should().BeEmpty();
        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void StringSplitAndJoin()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System;
public class A
{
    public string X { get; set; }

    public string Y => string.Join( '|', X.Split( ',' ) );
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  X [ Y ]
  Y
";

        diagnostics.Should().BeEmpty();
        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void StaticLocalFunctionInvocation()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class A
{
    public int Z { get; set; }

    public int X { get; set; }

    public int Y 
    {
        get
        {
            return Mul3( X );

            // Don't be fooled by parameter Z having same name as property Z:
            static int Mul3( int Z ) => Z * 2; 
        }
    }
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  X [ Y ]
  Y
  Z
";

        diagnostics.Should().BeEmpty();
        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "No" )]
    [Fact]
    public void NonStaticLocalFunctionInvocation()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class A
{
    public int Z { get; set; }

    public int X { get; set; }

    public int Y 
    {
        get
        {
            // Invocation of non-static local function is not supported.
            return Fn( this.X );
                        
            int Fn( int v ) => v * this.Z;
        }
    }
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  X [ Y ]
  Y
  Z
";

        diagnostics.Should().Equal( "LAMA5162@'Fn'" );
        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "No" )]
    [Fact]
    public void StaticLocalFunctionInvocationWithNonPrimitiveArgument()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class A
{
    public int Z { get; set; }

    public int X { get; set; }

    public int Y 
    {
        get
        {
            // Invocation of static local function with non-primitive args is not supported.
            return Fn( this );
                        
            static int Fn( A v ) => v.Z * 2;
        }
    }
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  X
  Y
  Z
";

        diagnostics.Should()
            .Equal( "LAMA5162@'Fn'" );

        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "No" )]
    [Fact]
    public void ExtensionMethodOnTargetType()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public static class Extensions
{
    public static int GetZ( this A a ) => a.Z;
}

public class A
{
    public int Z { get; set; }

    public int X { get; set; }

    public int Y => this.GetZ() + X;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  X [ Y ]
  Y
  Z
";

        diagnostics.Should()
            .Equal( "LAMA5162@'GetZ'" );

        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "No" )]
    [Fact]
    public void StaticMethodOfTargetTypeInvocationWithNonPrimitiveArgument()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class A
{
    public int Z { get; set; }

    public int X { get; set; }

    public int Y 
    {
        get
        {
            // Invocation of static method with non-primitive args is not supported.
            return Fn( this ) + X;
        }
    }

    private static int Fn( A v ) => v.Z * 2;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  X [ Y ]
  Y
  Z
";

        diagnostics.Should()
            .Equal( "LAMA5162@'Fn'" );

        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void StaticMethodOfTargetTypeInvocationWithPrimitiveArgument()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class A
{
    public int X { get; set; }

    public int Y 
    {
        get
        {
            // Invocation of static method with non-primitive args is not supported.
            return Fn( this.X );
        }
    }

    private static int Fn( int v ) => v * 2;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  X [ Y ]
  Y
";

        diagnostics.Should().BeEmpty();
        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "No" )]
    [Fact]
    public void SafeStaticMethodOfOtherTypeInvocationWithNonPrimitiveArgument()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class P
{
    public static int Fn( A v ) => v.Z * 2;
}

public class A
{
    public int Z { get; set; }

    public int X { get; set; }
    
    public int Y 
    {
        get
        {
            // Invocation of static method with non-primitive args is not supported.
            return P.Fn( this ) + this.X;
        }
    }
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph(
            testContext,
            type,
            isConfiguredAsSafe: _ => true,
            reportDiagnostic: diagnostics.Add,
            treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  X [ Y ]
  Y
  Z
";

        diagnostics.Should().BeEmpty();

        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void SafeStaticMethodOfOtherTypeInvocationWithPrimitiveArgument()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class P
{
    public static int Fn( int v ) => v * 2;
}

public class A
{
    public int X { get; set; }

    public int Y 
    {
        get
        {
            return P.Fn( this.X );
        }
    }
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph(
            testContext,
            type,
            isConfiguredAsSafe: _ => true,
            reportDiagnostic: diagnostics.Add,
            treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  X [ Y ]
  Y
";

        diagnostics.Should().BeEmpty();
        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void CoalesceExpression()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class P
{
    public static int Fn( int v ) => v * 2;
}

public class B
{
    public int B1 { get; set; }

    public C? B2 { get; set; }

    public C? B3 { get; set; }
}

public class C
{
    public int C1 { get; set; }
}

public class A
{
    public B? A1 { get; set; }

    public B? A2 { get; set; }

    // Q references A1.B1 and A2.B1
    public int Q => A1?.B1 ?? A2?.B1 ?? 42;

    // R references A1.B2.C1, A2.B2.C1, A1.B3.C1 and A2.B3.C1.
    public int R => ((this.A1 ?? this.A2)?.B2 ?? (this.A1 ?? this.A2)?.B3!).C1;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  A1
    B1 [ Q ]
    B2
      C1 [ R ]
    B3
      C1 [ R ]
  A2
    B1 [ Q ]
    B2
      C1 [ R ]
    B3
      C1 [ R ]
  Q
  R
";

        diagnostics.Should().BeEmpty();
        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void ConditionalExpression()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class P
{
    public static int Fn( int v ) => v * 2;
}

public class B
{
    public int B1 { get; set; }

    public C? B2 { get; set; }

    public C? B3 { get; set; }
}

public class C
{
    public int C1 { get; set; }
}

public class A
{
    public B? A1 { get; set; }

    public B? A2 { get; set; }

    public bool A3 { get; set; }

    // Q references A3, A1.B2.C1 and A2.B2.C1.
    public int? Q => (A3 ? A1 : A2)?.B2?.C1;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  A1
    B2
      C1 [ Q ]
  A2
    B2
      C1 [ Q ]
  A3 [ Q ]
  Q
";

        diagnostics.Should().BeEmpty();
        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void CombinedCoalesceAndConditionalExpressions()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class P
{
    public static int Fn( int v ) => v * 2;
}

public class B
{
    public int B1 { get; set; }

    public C? B2 { get; set; }

    public C? B3 { get; set; }
}

public class C
{
    public int C1 { get; set; }
}

public class A
{
    public B? A1 { get; set; }

    public B? A2 { get; set; }

    public B? A5 { get; set; }

    public B? A6 { get; set; }

    public B? A7 { get; set; }

    public bool A3 { get; set; }

    public bool A4 { get; set; }

    public int A8 { get; set; }

    // Q and R both reference A3, A4, A8, A1.B2.C1, A2.B2.C1, A5.B3.C1, A6.B3.C1 and A7.B3.C1.

    public int Q => ((A3 ? A1 : A2)?.B2 ?? (A4 ? A5 : A6)?.B3)?.C1 ?? A7?.B3?.C1 ?? A8;

    public int R => (((A3 ? A1 : A2)?.B2 ?? (A4 ? A5 : A6)?.B3) ?? A7?.B3)?.C1 ?? A8;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  A1
    B2
      C1 [ Q, R ]
  A2
    B2
      C1 [ Q, R ]
  A3 [ Q, R ]
  A4 [ Q, R ]
  A5
    B3
      C1 [ Q, R ]
  A6
    B3
      C1 [ Q, R ]
  A7
    B3
      C1 [ Q, R ]
  A8 [ Q, R ]
  Q
  R
";

        diagnostics.Should().BeEmpty();
        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "No" )]
    [Fact]
    public void ReferenceToChildPropertyOfNonInpcNonPrimitiveProperty()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
// TreatAsImplementsInpc will return false for class B
public class B
{
    public int B1 { get; set; }
}

public class A
{
    public B A1 { get; set; }

    // A1 is of a non-inpc, non-primary type.
    public int? A2 => A1?.B1;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: NeverTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = @"<root>
  A1
    B1 [ A2 ]
  A2
";

        diagnostics.Should().Equal( "LAMA5161@'A1'" );
        result.ToString().Should().Be( NormalizeEOL( expected ) );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void StaticReadOnlyPrimitiveFieldOfOtherType()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public static class Other
{
    public static readonly int Foo = 42;
}

public class A
{
    public int X => Other.Foo;

    public int Y { get; set; }

    public int Z => this.X + this.Y;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        const string expected = @"<root>
  X [ Z ]
  Y [ Z ]
  Z
";

        result.ToString().Should().Be( NormalizeEOL( expected ) );
        diagnostics.Should().BeEmpty();

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );
    }

    [Trait( "Supported", "No" )]
    [Fact]
    public void StaticMutablePrimitiveFieldOfOtherType()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public static class Other
{
    public static int Foo = 42;
}

public class A
{
    public int X => Other.Foo;

    public int Y { get; set; }

    public int Z => this.X + this.Y;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        const string expected = @"<root>
  X [ Z ]
  Y [ Z ]
  Z
";

        result.ToString().Should().Be( NormalizeEOL( expected ) );

        diagnostics.Should()
            .Equal( "LAMA5164@'Foo'" );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );
    }

    [Trait( "Supported", "No" )]
    [Fact]
    public void StaticReadOnlyNonPrimitiveFieldOfOtherType()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class B
{
    public int Q => 42;
}
public static class Other
{
    public static readonly B Foo;
}
public class A
{
    public int X => Other.Foo.Q;

    public int Y { get; set; }

    public int Z => this.X + this.Y;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        const string expected = @"<root>
  X [ Z ]
  Y [ Z ]
  Z
";

        result.ToString().Should().Be( NormalizeEOL( expected ) );

        diagnostics.Should()
            .Equal( "LAMA5164@'Foo'" );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void PublicStaticReadOnlyPrimitiveFieldOfTargetType()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class A
{
    public static readonly int Foo = 42;

    public int X => Foo;

    public int Y { get; set; }

    public int Z => this.X + this.Y;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        const string expected = @"<root>
  X [ Z ]
  Y [ Z ]
  Z
";

        result.ToString().Should().Be( NormalizeEOL( expected ) );
        diagnostics.Should().BeEmpty();

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );
    }

    [Trait( "Supported", "No" )]
    [Fact]
    public void PrivateStaticMutablePrimitiveFieldOfTargetType()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class A
{
    private static int Foo = 42;

    public int X => Foo;

    public int Y { get; set; }

    public int Z => this.X + this.Y;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        const string expected = @"<root>
  X [ Z ]
  Y [ Z ]
  Z
";

        result.ToString().Should().Be( NormalizeEOL( expected ) );

        diagnostics.Should()
            .Equal( "LAMA5164@'Foo'" );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void PrivateMutablePrimitiveFieldOfTargetType()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class A
{
    private int _foo = 42;

    public int X => this._foo;

    public int Y { get; set; }

    public int Z => this.X + this.Y;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        const string expected = @"<root>
  _foo [ X, Z ]
  X [ Z ]
  Y [ Z ]
  Z
";

        result.ToString().Should().Be( NormalizeEOL( expected ) );
        diagnostics.Should().BeEmpty();

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void PrivateMutableNonPrimitiveFieldOfTargetType()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class B
{
    public int Q => 42;
}
public class A
{
    private B _foo;

    public int X => this._foo.Q;

    public int Y { get; set; }

    public int Z => this.X + this.Y;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        const string expected = @"<root>
  _foo
    Q [ X, Z ]
  X [ Z ]
  Y [ Z ]
  Z
";

        result.ToString().Should().Be( NormalizeEOL( expected ) );
        diagnostics.Should().BeEmpty();

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );
    }

    [Trait( "Supported", "No" )]
    [Fact]
    public void PublicStaticMutablePrimitiveFieldOfTargetType()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
public class A
{
    public static int Foo = 42;

    public int X => Foo;

    public int Y { get; set; }

    public int Z => this.X + this.Y;
}";

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "A" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        const string expected = @"<root>
  X [ Z ]
  Y [ Z ]
  Z
";

        result.ToString().Should().Be( NormalizeEOL( expected ) );

        diagnostics.Should()
            .Equal( "LAMA5164@'Foo'" );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );
    }

    [Trait( "Supported", "Yes" )]
    [Fact]
    public void BinaryOperatorInExpression()
    {
        using var testContext = this.CreateTestContext();

        const string code = """
                            using System;

                            public class DateTimeViewModel
                            {
                                public DateTime DateTime { get; set; }
                            
                                public double MinutesFromNow => (DateTime.Now - this.DateTime).TotalMinutes;
                            }
                            """;

        var compilation = CreateCompilation( testContext, code );

        var type = compilation.Types.OfName( "DateTimeViewModel" ).Single();

        var diagnostics = new List<string>();

        var result = CreateDependencyGraph( testContext, type, reportDiagnostic: diagnostics.Add, treatAsImplementingInpc: AlwaysTreatAsInpc );

        this.TestOutput.WriteLines( diagnostics );
        this.TestOutput.WriteLine( result.ToString() );

        const string expected = """
                                <root>
                                  DateTime [ MinutesFromNow ]
                                  MinutesFromNow

                                """;

        result.ToString().Should().Be( NormalizeEOL( expected ) );

        diagnostics.Should().BeEmpty();
    }

    // ReSharper disable once InconsistentNaming
    private static string NormalizeEOL( string text )
    {
        return Regex.Replace( text, @"\r\n|\n\r|\n|\r", Environment.NewLine );
    }
}