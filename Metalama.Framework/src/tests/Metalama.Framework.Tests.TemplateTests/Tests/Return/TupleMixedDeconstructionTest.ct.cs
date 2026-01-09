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
namespace Metalama.Framework.Tests.TemplateTests.Return.TupleMixedDeconstructionTest
{
  [CompileTime]
  internal class Aspect
  {
    // Test mixed tuple deconstruction: (var first, var (second, third))
    // This combines TupleExpression syntax with nested ParenthesizedVariableDesignation
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
      (var first, var(second, third)) = Aspect.GetNestedTuple();
      __next1:
        ;
      Unsafe.SkipInit(out first);
      Unsafe.SkipInit(out second);
      Unsafe.SkipInit(out third);
      if (__skip1)
        goto __next2;
      // return $"{first},{second},{third}";
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(templateSyntaxFactory.RunTimeExpression(templateSyntaxFactory.StringLiteralExpression($"{first},{second},{third}"), "Y:global::System.String!"))));
      __next2:
        ;
      return SyntaxFactory.Block(default, templateSyntaxFactory.ToStatementList(__s1));
    }
    [CompileTime]
    private static (string, (string, string)) GetNestedTuple()
    {
      return ("a", ("b", "c"));
    }
  }
  internal class TargetCode
  {
    // <target>
    private object? Method(int x)
    {
      return null;
    }
  }
}