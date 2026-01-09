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
namespace Metalama.Framework.Tests.TemplateTests.Return.TupleDeconstructionAfterReturn
{
  [CompileTime]
  internal class Aspect
  {
    // Test tuple deconstruction with ParenthesizedVariableDesignation: var (x, y) = tuple;
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
      SyntaxToken secondName = templateSyntaxFactory.GetUniqueIdentifier("second");
      if (__skip1)
        goto __next1;
      // var (first, second) = GetTuple();
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.ToStatement(templateSyntaxFactory.RewriteAssignmentExpression(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.DeclarationExpression(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("var")), SyntaxFactory.ParenthesizedVariableDesignation(SyntaxFactory.Token(SyntaxKind.OpenParenToken), SyntaxFactory.SeparatedList<VariableDesignationSyntax>(new VariableDesignationSyntax[] { SyntaxFactory.SingleVariableDesignation(templateSyntaxFactory.EscapeIdentifier(firstName)), SyntaxFactory.SingleVariableDesignation(templateSyntaxFactory.EscapeIdentifier(secondName)) }), SyntaxFactory.Token(SyntaxKind.CloseParenToken))), SyntaxFactory.Token(SyntaxKind.EqualsToken), templateSyntaxFactory.Serialize<(string, string)>(Aspect.GetTuple())))));
      __next1:
        ;
      if (__skip1)
        goto __next2;
      // return $"{first},{second}";
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(templateSyntaxFactory.RenderInterpolatedString(SyntaxFactory.InterpolatedStringExpression(SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken), SyntaxFactory.List<InterpolatedStringContentSyntax>(new InterpolatedStringContentSyntax[] { templateSyntaxFactory.FixInterpolationSyntax(SyntaxFactory.Interpolation(SyntaxFactory.Token(SyntaxKind.OpenBraceToken), SyntaxFactory.IdentifierName(templateSyntaxFactory.EscapeIdentifier(firstName)), null, null, SyntaxFactory.Token(SyntaxKind.CloseBraceToken))), SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, ",", ",", default)), templateSyntaxFactory.FixInterpolationSyntax(SyntaxFactory.Interpolation(SyntaxFactory.Token(SyntaxKind.OpenBraceToken), SyntaxFactory.IdentifierName(templateSyntaxFactory.EscapeIdentifier(secondName)), null, null, SyntaxFactory.Token(SyntaxKind.CloseBraceToken))) }), SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken))))));
      __next2:
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