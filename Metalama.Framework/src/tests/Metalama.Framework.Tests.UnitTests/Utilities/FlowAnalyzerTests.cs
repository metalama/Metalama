// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

public sealed class FlowAnalyzerTests : UnitTestClass
{
    [Fact]
    public void NeverContinues_ReturnStatement_ReturnsTrue()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M()
                                {
                                    return;
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var returnStatement = methodBody.Statements[0];

        Assert.True( returnStatement.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_ThrowStatement_ReturnsTrue()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M()
                                {
                                    throw new System.Exception();
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var throwStatement = methodBody.Statements[0];

        Assert.True( throwStatement.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_EmptyBlock_ReturnsFalse()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M()
                                {
                                    {
                                    }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var block = methodBody.Statements[0];

        Assert.False( block.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_BlockWithReturn_ReturnsTrue()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M()
                                {
                                    {
                                        return;
                                    }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var block = methodBody.Statements[0];

        Assert.True( block.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_BlockWithMultipleStatementsEndingInReturn_ReturnsTrue()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M()
                                {
                                    {
                                        var x = 1;
                                        System.Console.WriteLine(x);
                                        return;
                                    }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var block = methodBody.Statements[0];

        Assert.True( block.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_IfWithoutElse_ReturnsFalse()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M(bool condition)
                                {
                                    if (condition)
                                    {
                                        return;
                                    }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var ifStatement = methodBody.Statements[0];

        Assert.False( ifStatement.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_IfElseBothReturn_ReturnsTrue()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M(bool condition)
                                {
                                    if (condition)
                                    {
                                        return;
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var ifStatement = methodBody.Statements[0];

        Assert.True( ifStatement.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_IfElseOneReturns_ReturnsFalse()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M(bool condition)
                                {
                                    if (condition)
                                    {
                                        return;
                                    }
                                    else
                                    {
                                        var x = 1;
                                    }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var ifStatement = methodBody.Statements[0];

        Assert.False( ifStatement.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_IfElseBothThrow_ReturnsTrue()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M(bool condition)
                                {
                                    if (condition)
                                    {
                                        throw new System.Exception("A");
                                    }
                                    else
                                    {
                                        throw new System.Exception("B");
                                    }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var ifStatement = methodBody.Statements[0];

        Assert.True( ifStatement.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_SwitchWithoutDefault_ReturnsFalse()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M(int value)
                                {
                                    switch (value)
                                    {
                                        case 1:
                                            return;
                                        case 2:
                                            return;
                                    }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var switchStatement = methodBody.Statements[0];

        Assert.False( switchStatement.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_SwitchWithDefaultAllReturn_ReturnsTrue()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M(int value)
                                {
                                    switch (value)
                                    {
                                        case 1:
                                            return;
                                        case 2:
                                            return;
                                        default:
                                            return;
                                    }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var switchStatement = methodBody.Statements[0];

        Assert.True( switchStatement.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_SwitchWithDefaultOneSectionContinues_ReturnsFalse()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M(int value)
                                {
                                    switch (value)
                                    {
                                        case 1:
                                            return;
                                        case 2:
                                            var x = 1;
                                            break;
                                        default:
                                            return;
                                    }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var switchStatement = methodBody.Statements[0];

        Assert.False( switchStatement.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_SwitchWithDefaultAllThrow_ReturnsTrue()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M(int value)
                                {
                                    switch (value)
                                    {
                                        case 1:
                                            throw new System.Exception("1");
                                        case 2:
                                            throw new System.Exception("2");
                                        default:
                                            throw new System.Exception("default");
                                    }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var switchStatement = methodBody.Statements[0];

        Assert.True( switchStatement.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_ExpressionStatement_ReturnsFalse()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M()
                                {
                                    System.Console.WriteLine("test");
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var expressionStatement = methodBody.Statements[0];

        Assert.False( expressionStatement.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_StatementList_EmptyList_ReturnsFalse()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M()
                                {
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;

        Assert.False( methodBody.Statements.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_StatementList_WithReturn_ReturnsTrue()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M()
                                {
                                    var x = 1;
                                    return;
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;

        Assert.True( methodBody.Statements.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_StatementList_OnlyNormalStatements_ReturnsFalse()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M()
                                {
                                    var x = 1;
                                    var y = 2;
                                    System.Console.WriteLine(x + y);
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;

        Assert.False( methodBody.Statements.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_NestedIfElse_BothBranchesReturn_ReturnsTrue()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M(bool a, bool b)
                                {
                                    if (a)
                                    {
                                        if (b)
                                        {
                                            return;
                                        }
                                        else
                                        {
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var outerIf = methodBody.Statements[0];

        Assert.True( outerIf.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_SwitchWithMultipleCasesPerSection_ReturnsTrue()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M(int value)
                                {
                                    switch (value)
                                    {
                                        case 1:
                                        case 2:
                                            return;
                                        case 3:
                                        case 4:
                                            throw new System.Exception();
                                        default:
                                            return;
                                    }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var switchStatement = methodBody.Statements[0];

        Assert.True( switchStatement.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_ComplexNestedControl_ReturnsTrue()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                int M(int value, bool flag)
                                {
                                    switch (value)
                                    {
                                        case 1:
                                            if (flag)
                                            {
                                                return 10;
                                            }
                                            else
                                            {
                                                throw new System.Exception();
                                            }
                                        case 2:
                                            {
                                                var x = 1;
                                                return x * 2;
                                            }
                                        default:
                                            return -1;
                                    }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var switchStatement = methodBody.Statements[0];

        Assert.True( switchStatement.NeverContinues() );
    }

    [Fact]
    public void NeverContinues_SwitchEmptyDefaultSection_ReturnsFalse()
    {
        using var context = this.CreateTestContext();

        const string code = """
                            class C
                            {
                                void M(int value)
                                {
                                    switch (value)
                                    {
                                        case 1:
                                            return;
                                        default:
                                            break;
                                    }
                                }
                            }
                            """;

        var compilation = context.CreateCSharpCompilation( code );
        var syntaxTree = compilation.SyntaxTrees.First();
        var methodBody = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!;
        var switchStatement = methodBody.Statements[0];

        Assert.False( switchStatement.NeverContinues() );
    }
}
