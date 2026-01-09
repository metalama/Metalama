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
#pragma warning disable CS0162 // Unreachable code detected
namespace Metalama.Framework.Tests.TemplateTests.Return.ReturnInCompileTimeSwitch
{
  [CompileTime]
  internal class Aspect
  {
    // Return inside a compile-time switch case should terminate compile-time flow.
    private SyntaxNode __Template(ITemplateSyntaxFactory templateSyntaxFactory)
    {
      List<StatementOrTrivia> __s1 = new List<StatementOrTrivia>();
      bool __skip1 = false;
      switch (meta.Target.Method.Parameters.Count)
      {
        case 0:
          // return null;
          templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword)))));
          __skip1 = true;
          break;
        // Compile-time flow should stop here.
        default:
          if (__skip1)
            goto __next1;
          break;
          __next1:
            ;
          break;
      }
      if (__skip1)
        goto __next2;
      Aspect.ThrowIfReached();
      __next2:
        ;
      if (__skip1)
        goto __next3;
      // return meta.Proceed();
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.DynamicReturnStatement(templateSyntaxFactory.GetUserExpression(templateSyntaxFactory.Proceed("Proceed")), false)));
      __next3:
        ;
      return SyntaxFactory.Block(default, templateSyntaxFactory.ToStatementList(__s1));
    }
    [CompileTime]
    private static void ThrowIfReached() => throw new InvalidOperationException("Compile-time flow should have stopped at return.");
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