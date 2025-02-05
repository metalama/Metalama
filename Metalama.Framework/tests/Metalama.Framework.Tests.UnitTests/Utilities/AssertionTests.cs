// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

#pragma warning disable VSTHRD200

public sealed class AssertionTests : UnitTestClass
{
    [Fact]
    public void Symbols()
    {
        const string code = @"
using System;

public class X
{
    public int A;
    public int B { get; set; }
    public event EventHandler C;
    public void D(int p) { }
    public static X() { }
    public X() { }
    public ~X() { }
}
";

        var compilation = CSharpCompilation.Create( null, [CSharpSyntaxTree.ParseText( code, path: Path.Combine( "path", "file.cs" ) )] );
        
        var typeX = compilation.Assembly.GlobalNamespace.GetTypeMembers().Single();
        var memberA = typeX.GetMembers().Single( m => m.Name == "A" );
        var memberB = typeX.GetMembers().Single( m => m.Name == "B" );
        var memberC = typeX.GetMembers().Single( m => m.Name == "C" );
        var memberD = typeX.GetMembers().Single( m => m.Name == "D" );
        
        // ReSharper disable once InconsistentNaming
        var memberDP = ((IMethodSymbol) typeX.GetMembers().Single( m => m.Name == "D" )).Parameters[0];
        var memberX = typeX.GetMembers().Single( m => m is IMethodSymbol { MethodKind: MethodKind.Constructor } );
        var memberTildeX = typeX.GetMembers().Single( m => m is IMethodSymbol { MethodKind: MethodKind.Destructor } );

        Assert.Equal( "NamedType:X:[(3,13)-(3,14):file.cs]", new AssertionFailedException( $"{typeX}" ).Message );
        Assert.Equal( "Field:X.A:[(5,15)-(5,16):file.cs]", new AssertionFailedException( $"{memberA}" ).Message );
        Assert.Equal( "Property:X.B:[(6,15)-(6,16):file.cs]", new AssertionFailedException( $"{memberB}" ).Message );
        Assert.Equal( "Event:X.C:[(7,30)-(7,31):file.cs]", new AssertionFailedException( $"{memberC}" ).Message );
        Assert.Equal( "Method:X.D(int):[(8,16)-(8,17):file.cs]", new AssertionFailedException( $"{memberD}" ).Message );
        Assert.Equal( "Parameter:X.D(int):int p:[(8,22)-(8,23):file.cs]", new AssertionFailedException( $"{memberDP}" ).Message );
        Assert.Equal( "Method:X.X():[(10,11)-(10,12):file.cs]", new AssertionFailedException( $"{memberX}" ).Message );
        Assert.Equal( "Method:X.~X():[(11,12)-(11,13):file.cs]", new AssertionFailedException( $"{memberTildeX}" ).Message );
    }

    [Fact]
    public void SymbolErrors()
    {
        const string code = @"
using System;

public class X
{
    private Goo z;

    public void Foo()
    {
        _ = this.Quz(); // Non-existent member.
        _ = new NS.X(); // Non-existent namespace.
        _ = Y.Bar(); // Inaccessible member.
    }
}

public class Y
{
    static int Bar() => 42;
}
";

        var compilation = CSharpCompilation.Create( null, [CSharpSyntaxTree.ParseText( code, path: Path.Combine( "path", "file.cs" ) )] );

        // Just make sure we are able to create exception for all symbols.
        // ReSharper disable once UnusedVariable
        var symbols =
            compilation.SyntaxTrees
                .SelectMany( s => s.GetRoot().DescendantNodesAndSelf() )
                .Select( n => compilation.GetSemanticModel( n.SyntaxTree ).GetSymbolInfo( n ) )
                .SelectMany( si => si.CandidateSymbols.Append( si.Symbol ) )
                .Select( s => new AssertionFailedException( $"{s}" ) )
                .ToList();
    }

    [Fact]
    public void SyntaxNodes()
    {
        const string code = @"
using System;

public class X
{
    private int field;

    private int AutoProperty { get; set; }

    private int PropertyWithExpressionBodies 
    { 
        get => 42; 
        set => _ = 42;
    }

    private int PropertyWithBlockBodies 
    { 
        get
        {
            _ = 42;
            return 42;
        }
        set
        {
            _ = 42;
            _ = 42;
        }
    }

    public void MethodWithExpressionBody() => _ = 42;

    public void MethodWithBody()
    {
        Console.WriteLine();
        Console.WriteLine(""42"");
        Console.WriteLine(""42"", 42);

        if (true) { }
        else if (false) { }
        else { }

        for(int i = 0; i < 42; i++) { }

        foreach(var x in [42,42,42]) { }

        switch(42)
        {
            case 42:
                break;
            default:
                break;
        }

        var x = new [] {42, 42};
        int[] y = new int[] {42, 42};

        lock(new object()) { }

        _ = 42 switch { 42 => 42, _ => 0 };
    }
}
";

        var compilation = CSharpCompilation.Create( null, [CSharpSyntaxTree.ParseText( code, path: Path.Combine( "path", "file.cs" ) )] );

        // Just make sure we are able to create exception for all symbols.
        var exceptionStrings =
            compilation.SyntaxTrees
                .SelectMany( t => t.GetRoot().DescendantNodesAndSelf() )
                .Select( n => (Node: n, ExceptionFactory: (Func<Exception>) (() => new AssertionFailedException( $"{n}" ))) )
                .ToList();

        var seenCombinations = new HashSet<(SyntaxKind Kind, string String)>();
        var uniqueStrings = new List<string>();
        var index = 0;

        foreach ( var (node, exceptionFactory) in exceptionStrings )
        {
            if ( seenCombinations.Add( (node.Kind(), node.ToString()) ) )
            {
                uniqueStrings.Add( $"[{index}] => {exceptionFactory().Message}" );
            }

            index++;
        }

        const string expected = @"
[0] => [CompilationUnit:(1,0)-(61,0):file.cs] using <IdentifierName>;public class <identifier>{<...>}
[1] => [UsingDirective:(1,0)-(1,13):file.cs] using <identifier>;
[2] => [IdentifierName:(1,6)-(1,12):file.cs] <identifier>
[3] => [ClassDeclaration:(3,0)-(60,1):file.cs] public class <identifier>{<...>}
[4] => [FieldDeclaration:(5,4)-(5,22):file.cs] private <PredefinedType> <VariableDeclarator>;
[5] => [VariableDeclaration:(5,12)-(5,21):file.cs] int <identifier>
[6] => [PredefinedType:(5,12)-(5,15):file.cs] int 
[7] => [VariableDeclarator:(5,16)-(5,21):file.cs] <identifier>
[8] => [PropertyDeclaration:(7,4)-(7,42):file.cs] private int <identifier> { get; set; }
[10] => [AccessorList:(7,29)-(7,42):file.cs] { get; set; }
[11] => [GetAccessorDeclaration:(7,31)-(7,35):file.cs] get; 
[12] => [SetAccessorDeclaration:(7,36)-(7,40):file.cs] set; 
[13] => [PropertyDeclaration:(9,4)-(13,5):file.cs] private int <identifier> { get => <NumericLiteralExpression>; set => <SimpleAssignmentExpression>; }
[15] => [AccessorList:(10,4)-(13,5):file.cs] { get => <numeric_literal>; set => <IdentifierName> = <NumericLiteralExpression>; }
[16] => [GetAccessorDeclaration:(11,8)-(11,18):file.cs] get => <numeric_literal>; 
[17] => [ArrowExpressionClause:(11,12)-(11,17):file.cs] => <numeric_literal>
[18] => [NumericLiteralExpression:(11,15)-(11,17):file.cs] <numeric_literal>
[19] => [SetAccessorDeclaration:(12,8)-(12,22):file.cs] set => <IdentifierName> = <NumericLiteralExpression>;
[20] => [ArrowExpressionClause:(12,12)-(12,21):file.cs] => <IdentifierName> = <NumericLiteralExpression>
[21] => [SimpleAssignmentExpression:(12,15)-(12,21):file.cs] <identifier> = <numeric_literal>
[22] => [IdentifierName:(12,15)-(12,16):file.cs] <identifier> 
[24] => [PropertyDeclaration:(15,4)-(27,5):file.cs] private int <identifier> { get {<...>} set {<...>} }
[26] => [AccessorList:(16,4)-(27,5):file.cs] { get {<...>} set {<...>} }
[27] => [GetAccessorDeclaration:(17,8)-(21,9):file.cs] get {<...>}
[28] => [Block:(18,8)-(21,9):file.cs] {<...>}
[29] => [ExpressionStatement:(19,12)-(19,19):file.cs] <identifier> = <numeric_literal>;
[33] => [ReturnStatement:(20,12)-(20,22):file.cs] return <numeric_literal>;
[35] => [SetAccessorDeclaration:(22,8)-(26,9):file.cs] set {<...>}
[36] => [Block:(23,8)-(26,9):file.cs] {<...>}
[45] => [MethodDeclaration:(29,4)-(29,53):file.cs] public void <identifier>() => <SimpleAssignmentExpression>;
[46] => [PredefinedType:(29,11)-(29,15):file.cs] void 
[47] => [ParameterList:(29,40)-(29,42):file.cs] () 
[52] => [MethodDeclaration:(31,4)-(59,5):file.cs] public void <identifier>() {<...>}
[55] => [Block:(32,4)-(59,5):file.cs] {<...>}
[56] => [ExpressionStatement:(33,8)-(33,28):file.cs] <IdentifierName>.<IdentifierName>();
[57] => [InvocationExpression:(33,8)-(33,27):file.cs] <IdentifierName>.<IdentifierName>()
[58] => [SimpleMemberAccessExpression:(33,8)-(33,25):file.cs] <identifier>.<identifier>
[59] => [IdentifierName:(33,8)-(33,15):file.cs] <identifier>
[60] => [IdentifierName:(33,16)-(33,25):file.cs] <identifier>
[61] => [ArgumentList:(33,25)-(33,27):file.cs] ()
[62] => [ExpressionStatement:(34,8)-(34,32):file.cs] <IdentifierName>.<IdentifierName>(<Argument>);
[63] => [InvocationExpression:(34,8)-(34,31):file.cs] <IdentifierName>.<IdentifierName>(<Argument>)
[67] => [ArgumentList:(34,25)-(34,31):file.cs] (<StringLiteralExpression>)
[68] => [Argument:(34,26)-(34,30):file.cs] <string_literal>
[69] => [StringLiteralExpression:(34,26)-(34,30):file.cs] <string_literal>
[70] => [ExpressionStatement:(35,8)-(35,36):file.cs] <IdentifierName>.<IdentifierName>(<Argument>, <Argument>);
[71] => [InvocationExpression:(35,8)-(35,35):file.cs] <IdentifierName>.<IdentifierName>(<Argument>, <Argument>)
[75] => [ArgumentList:(35,25)-(35,35):file.cs] (<StringLiteralExpression>, <NumericLiteralExpression>)
[78] => [Argument:(35,32)-(35,34):file.cs] <numeric_literal>
[80] => [IfStatement:(37,8)-(39,16):file.cs] if (true) { } else <IfStatement>
[81] => [TrueLiteralExpression:(37,12)-(37,16):file.cs] true
[82] => [Block:(37,18)-(37,21):file.cs] { }
[83] => [ElseClause:(38,8)-(39,16):file.cs] else if (<FalseLiteralExpression>) <Block><ElseClause>
[84] => [IfStatement:(38,13)-(39,16):file.cs] if (false) { } else <Block>
[85] => [FalseLiteralExpression:(38,17)-(38,22):file.cs] false
[87] => [ElseClause:(39,8)-(39,16):file.cs] else { }
[89] => [ForStatement:(41,8)-(41,39):file.cs] for(<PredefinedType> <VariableDeclarator>; <IdentifierName> < <NumericLiteralExpression>; <IdentifierName>++) { }
[90] => [VariableDeclaration:(41,12)-(41,21):file.cs] int <identifier> <EqualsValueClause>
[92] => [VariableDeclarator:(41,16)-(41,21):file.cs] <identifier> = <NumericLiteralExpression>
[93] => [EqualsValueClause:(41,18)-(41,21):file.cs] = <numeric_literal>
[94] => [NumericLiteralExpression:(41,20)-(41,21):file.cs] <numeric_literal>
[95] => [LessThanExpression:(41,23)-(41,29):file.cs] <identifier> < <numeric_literal>
[96] => [IdentifierName:(41,23)-(41,24):file.cs] <identifier> 
[98] => [PostIncrementExpression:(41,31)-(41,34):file.cs] <identifier>++
[101] => [ForEachStatement:(43,8)-(43,40):file.cs] foreach(var <identifier> in [<...>]) { }
[102] => [IdentifierName:(43,16)-(43,19):file.cs] var 
[103] => [CollectionExpression:(43,25)-(43,35):file.cs] [<...>]
[104] => [ExpressionElement:(43,26)-(43,28):file.cs] <numeric_literal>
[111] => [SwitchStatement:(45,8)-(51,9):file.cs] switch(<numeric_literal>) {<...>}
[113] => [SwitchSection:(47,12)-(48,22):file.cs] case <NumericLiteralExpression>: break;
[114] => [CaseSwitchLabel:(47,12)-(47,20):file.cs] case <numeric_literal>:
[116] => [BreakStatement:(48,16)-(48,22):file.cs] break;
[117] => [SwitchSection:(49,12)-(50,22):file.cs] default: break;
[118] => [DefaultSwitchLabel:(49,12)-(49,20):file.cs] default:
[120] => [LocalDeclarationStatement:(53,8)-(53,32):file.cs] <IdentifierName> <VariableDeclarator>;
[121] => [VariableDeclaration:(53,8)-(53,31):file.cs] var <identifier> <EqualsValueClause>
[123] => [VariableDeclarator:(53,12)-(53,31):file.cs] <identifier> = <ImplicitArrayCreationExpression>
[124] => [EqualsValueClause:(53,14)-(53,31):file.cs] = new [] <ArrayInitializerExpression>
[125] => [ImplicitArrayCreationExpression:(53,16)-(53,31):file.cs] new [] {<...>}
[126] => [ArrayInitializerExpression:(53,23)-(53,31):file.cs] {<...>}
[129] => [LocalDeclarationStatement:(54,8)-(54,37):file.cs] <ArrayType> <VariableDeclarator>;
[130] => [VariableDeclaration:(54,8)-(54,36):file.cs] <PredefinedType><ArrayRankSpecifier> <identifier> <EqualsValueClause>
[131] => [ArrayType:(54,8)-(54,13):file.cs] int[<OmittedArraySizeExpression>] 
[133] => [ArrayRankSpecifier:(54,11)-(54,13):file.cs] [] 
[134] => [OmittedArraySizeExpression:(54,12)-(54,12):file.cs] 
[135] => [VariableDeclarator:(54,14)-(54,36):file.cs] <identifier> = <ArrayCreationExpression>
[136] => [EqualsValueClause:(54,16)-(54,36):file.cs] = new <ArrayType> <ArrayInitializerExpression>
[137] => [ArrayCreationExpression:(54,18)-(54,36):file.cs] new <PredefinedType><ArrayRankSpecifier> {<...>}
[145] => [LockStatement:(56,8)-(56,30):file.cs] lock(new <PredefinedType><ArgumentList>) { }
[146] => [ObjectCreationExpression:(56,13)-(56,25):file.cs] new object()
[147] => [PredefinedType:(56,17)-(56,23):file.cs] object
[150] => [ExpressionStatement:(58,8)-(58,43):file.cs] <identifier> = <numeric_literal> switch { <...>};
[151] => [SimpleAssignmentExpression:(58,8)-(58,42):file.cs] <identifier> = <numeric_literal> switch { <...>}
[153] => [SwitchExpression:(58,12)-(58,42):file.cs] <numeric_literal> switch { <...>}
[155] => [SwitchExpressionArm:(58,24)-(58,32):file.cs] <NumericLiteralExpression> => <numeric_literal>
[156] => [ConstantPattern:(58,24)-(58,26):file.cs] <numeric_literal> 
[159] => [SwitchExpressionArm:(58,34)-(58,40):file.cs] _ => <numeric_literal> 
[160] => [DiscardPattern:(58,34)-(58,35):file.cs] _ 
";

        var actual = string.Join( "\r\n", uniqueStrings );

        Assert.Equal( expected.Trim(), actual.Trim() );
    }
}