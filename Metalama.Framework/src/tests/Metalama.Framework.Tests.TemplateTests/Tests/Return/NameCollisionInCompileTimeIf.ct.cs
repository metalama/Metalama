// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Metalama.Framework.Aspects;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.Templating;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
#pragma warning disable CS0162 // Unreachable code detected
namespace Metalama.Framework.Tests.TemplateTests.Return.NameCollisionInCompileTimeIf
{
  [CompileTime]
  internal class Aspect
  {
    // Test: When the same variable name is declared in separate if/else branches,
    // the hoisting mechanism should handle them correctly by recognizing them as different symbols.
    private SyntaxNode __Template(ITemplateSyntaxFactory templateSyntaxFactory)
    {
      List<StatementOrTrivia> __s1 = new List<StatementOrTrivia>();
      bool __skip1 = false;
      if (meta.Target.Method.Name.Length < 0)
      {
        // return null;
        templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword)))));
        __skip1 = true;
      }
      if (__skip1)
        goto __next3;
      if (meta.Target.Method.Name.Length < 5)
      {
        var method = meta.Target.Method;
        // method.Invoke();
        templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.ToStatement(templateSyntaxFactory.GetDynamicSyntax(method.Invoke())));
        // return null;
        templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword)))));
        __skip1 = true;
      }
      else
      {
        if (__skip1)
          goto __next1;
        var method = meta.Target.Method;
        __next1:
          ;
        Unsafe.SkipInit(out method);
        if (__skip1)
          goto __next2;
        // method.Invoke();
        templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.ToStatement(templateSyntaxFactory.GetDynamicSyntax(method.Invoke())));
        __next2:
          ;
      }
      __next3:
        ;
      if (__skip1)
        goto __next4;
      // return meta.Proceed();
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.DynamicReturnStatement(templateSyntaxFactory.GetUserExpression(templateSyntaxFactory.Proceed("Proceed")), false)));
      __next4:
        ;
      return SyntaxFactory.Block(default, templateSyntaxFactory.ToStatementList(__s1));
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