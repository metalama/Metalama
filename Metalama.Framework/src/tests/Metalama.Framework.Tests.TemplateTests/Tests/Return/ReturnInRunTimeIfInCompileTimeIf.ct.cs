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
namespace Metalama.Framework.Tests.TemplateTests.Return.ReturnInRunTimeIfInCompileTimeIf
{
  [CompileTime]
  internal class Aspect
  {
    // Run-time if nested inside compile-time if:
    // - Compile-time condition is evaluated. If true, the run-time if is emitted.
    // - The compile-time flow should CONTINUE past the compile-time if because
    //   even though compile-time took this branch, the inner run-time if might not execute.
    private SyntaxNode __Template(ITemplateSyntaxFactory templateSyntaxFactory)
    {
      List<StatementOrTrivia> __s1 = new List<StatementOrTrivia>();
      bool __skip1 = false;
      var p = meta.Target.Parameters[0];
      if (meta.Target.Method.Name.Length > 0)
      {
        // if ( p.Value == null ) { return null; }
        templateSyntaxFactory.AddStatement(__s1, SyntaxFactory.IfStatement(default(SyntaxList<AttributeListSyntax>), SyntaxFactory.Token(SyntaxKind.IfKeyword), SyntaxFactory.Token(SyntaxKind.OpenParenToken), SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, templateSyntaxFactory.GetDynamicSyntax(p.Value), SyntaxFactory.Token(SyntaxKind.EqualsEqualsToken), SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword))), SyntaxFactory.Token(SyntaxKind.CloseParenToken), SyntaxFactory.Block(default, new Func<SyntaxList<StatementSyntax>>(delegate
        {
          List<StatementOrTrivia> __s2 = new List<StatementOrTrivia>();
          bool __skip2 = false;
          // return null;
          templateSyntaxFactory.AddStatement(__s2, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword)))));
          return templateSyntaxFactory.ToStatementList(__s2);
        })()), null));
      }
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