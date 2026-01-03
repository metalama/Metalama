// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.
using System;
using System.Collections.Generic;
using Metalama.Framework.Aspects;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.Templating;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
#pragma warning disable CS0162 // Unreachable code detected
namespace Metalama.Framework.Tests.TemplateTests.Return.ReturnInCompileTimeIfInRunTimeIf
{
  [CompileTime]
  internal class Aspect
  {
    // Compile-time if nested inside run-time if:
    // - The run-time if is emitted to output.
    // - Inside the run-time if, compile-time condition is evaluated.
    // - If compile-time condition is true, the return is emitted inside run-time if.
    // - Compile-time flow should CONTINUE past the run-time if because
    //   the run-time if might not execute at runtime.
    private SyntaxNode __Template(ITemplateSyntaxFactory templateSyntaxFactory)
    {
      List<StatementOrTrivia> __s1 = new List<StatementOrTrivia>();
      bool __skip1 = false;
      var p = meta.Target.Parameters[0];
      // if ( p.Value == null ) { // Compile-time condition that is always true. if ( meta.Target.Method.Name.Length > 0 ) { r...
      templateSyntaxFactory.AddStatement(__s1, SyntaxFactory.IfStatement(default(SyntaxList<AttributeListSyntax>), SyntaxFactory.Token(SyntaxKind.IfKeyword), SyntaxFactory.Token(SyntaxKind.OpenParenToken), SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, templateSyntaxFactory.GetDynamicSyntax(p.Value), SyntaxFactory.Token(SyntaxKind.EqualsEqualsToken), SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword))), SyntaxFactory.Token(SyntaxKind.CloseParenToken), SyntaxFactory.Block(default, new Func<SyntaxList<StatementSyntax>>(delegate
      {
        List<StatementOrTrivia> __s2 = new List<StatementOrTrivia>();
        bool __skip2 = false;
        if (meta.Target.Method.Name.Length > 0)
        {
          // return null;
          templateSyntaxFactory.AddStatement(__s2, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword)))));
          __skip2 = true;
        }
        return templateSyntaxFactory.ToStatementList(__s2);
      })()), null));
      // return meta.Proceed();
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.DynamicReturnStatement(templateSyntaxFactory.GetUserExpression(templateSyntaxFactory.Proceed("Proceed")), false)));
      return SyntaxFactory.Block(default, templateSyntaxFactory.ToStatementList(__s1));
    }
  }
  internal class TargetCode
  {
    // <target>
    private object? Method(object? a)
    {
      return a;
    }
  }
}