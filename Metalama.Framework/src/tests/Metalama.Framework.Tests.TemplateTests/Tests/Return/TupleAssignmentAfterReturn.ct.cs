// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.
using System.Collections.Generic;
using Metalama.Framework.Aspects;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.Templating;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
#pragma warning disable CS0162 // Unreachable code detected
namespace Metalama.Framework.Tests.TemplateTests.Return.TupleAssignmentAfterReturn
{
  [CompileTime]
  internal class Aspect
  {
    // Test tuple assignment to existing variables: (x, y) = tuple;
    private SyntaxNode __Template(ITemplateSyntaxFactory templateSyntaxFactory)
    {
      List<StatementOrTrivia> __s1 = new List<StatementOrTrivia>();
      bool __skip1 = false;
      if (meta.Target.Method.Parameters.Count == 0)
      {
        // return null;
        templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword)))));
        __skip1 = true;
      }
      SyntaxToken firstName = templateSyntaxFactory.GetUniqueIdentifier("first");
      if (__skip1)
        goto __next1;
      // var first = "initial";
      templateSyntaxFactory.AddStatement(__s1, SyntaxFactory.LocalDeclarationStatement(default(SyntaxList<AttributeListSyntax>), default(SyntaxToken), default(SyntaxToken), default(SyntaxTokenList), SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("var")), SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(templateSyntaxFactory.EscapeIdentifier(firstName), default(BracketedArgumentListSyntax), SyntaxFactory.EqualsValueClause(SyntaxFactory.Token(SyntaxKind.EqualsToken), SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("\"initial\"", "initial")))))), SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
      __next1:
        ;
      SyntaxToken secondName = templateSyntaxFactory.GetUniqueIdentifier("second");
      if (__skip1)
        goto __next2;
      // var second = "initial";
      templateSyntaxFactory.AddStatement(__s1, SyntaxFactory.LocalDeclarationStatement(default(SyntaxList<AttributeListSyntax>), default(SyntaxToken), default(SyntaxToken), default(SyntaxTokenList), SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("var")), SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(templateSyntaxFactory.EscapeIdentifier(secondName), default(BracketedArgumentListSyntax), SyntaxFactory.EqualsValueClause(SyntaxFactory.Token(SyntaxKind.EqualsToken), SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("\"initial\"", "initial")))))), SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
      __next2:
        ;
      if (__skip1)
        goto __next3;
      // (first, second) = GetTuple();
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.ToStatement(templateSyntaxFactory.RewriteAssignmentExpression(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.TupleExpression(SyntaxFactory.Token(SyntaxKind.OpenParenToken), SyntaxFactory.SeparatedList<ArgumentSyntax>(new ArgumentSyntax[] { SyntaxFactory.Argument(null, default, SyntaxFactory.IdentifierName(templateSyntaxFactory.EscapeIdentifier(firstName))).WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("first")), SyntaxFactory.Token(SyntaxKind.ColonToken))), SyntaxFactory.Argument(null, default, SyntaxFactory.IdentifierName(templateSyntaxFactory.EscapeIdentifier(secondName))).WithNameColon(SyntaxFactory.NameColon(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("second")), SyntaxFactory.Token(SyntaxKind.ColonToken))) }), SyntaxFactory.Token(SyntaxKind.CloseParenToken)), SyntaxFactory.Token(SyntaxKind.EqualsToken), templateSyntaxFactory.Serialize<(string, string)>(Aspect.GetTuple())))));
      __next3:
        ;
      if (__skip1)
        goto __next4;
      // return $"{first},{second}";
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(templateSyntaxFactory.RenderInterpolatedString(SyntaxFactory.InterpolatedStringExpression(SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken), SyntaxFactory.List<InterpolatedStringContentSyntax>(new InterpolatedStringContentSyntax[] { templateSyntaxFactory.FixInterpolationSyntax(SyntaxFactory.Interpolation(SyntaxFactory.Token(SyntaxKind.OpenBraceToken), SyntaxFactory.IdentifierName(templateSyntaxFactory.EscapeIdentifier(firstName)), null, null, SyntaxFactory.Token(SyntaxKind.CloseBraceToken))), SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, ",", ",", default)), templateSyntaxFactory.FixInterpolationSyntax(SyntaxFactory.Interpolation(SyntaxFactory.Token(SyntaxKind.OpenBraceToken), SyntaxFactory.IdentifierName(templateSyntaxFactory.EscapeIdentifier(secondName)), null, null, SyntaxFactory.Token(SyntaxKind.CloseBraceToken))) }), SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken))))));
      __next4:
        ;
      return SyntaxFactory.Block(default, templateSyntaxFactory.ToStatementList(__s1));
    }
    [CompileTime]
    private static (string, string) GetTuple()
    {
      return ("a", "b");
    }
  }
  internal class TargetCode
  {
    // <target>
    private object? Method()
    {
      return null;
    }
  }
}