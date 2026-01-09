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
namespace Metalama.Framework.Tests.TemplateTests.Return.MultipleOutVarsInIfAfterReturn
{
  [CompileTime]
  internal class Aspect
  {
    // Test that multiple out variables declared in nested if statements after a compile-time return
    // are properly handled with Unsafe.SkipInit
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
      if (__skip1)
        goto __next1;
      if (Aspect.TryGetFirst(out var first))
      {
        if (Aspect.TryGetSecond(out var second))
        {
          // return $"{first},{second}";
          templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(templateSyntaxFactory.RunTimeExpression(templateSyntaxFactory.StringLiteralExpression($"{first},{second}"), "Y:global::System.String!"))));
          __skip1 = true;
        }
      }
      __next1:
        ;
      Unsafe.SkipInit(out first);
      if (__skip1)
        goto __next2;
      // return meta.Proceed();
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.DynamicReturnStatement(templateSyntaxFactory.GetUserExpression(templateSyntaxFactory.Proceed("Proceed")), false)));
      __next2:
        ;
      return SyntaxFactory.Block(default, templateSyntaxFactory.ToStatementList(__s1));
    }
    [CompileTime]
    private static bool TryGetFirst(out string value)
    {
      value = "first";
      return true;
    }
    [CompileTime]
    private static bool TryGetSecond(out string value)
    {
      value = "second";
      return true;
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