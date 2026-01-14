// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.
using System.Collections.Generic;
using Metalama.Framework.Aspects;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.Templating;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
#pragma warning disable CS0162 // Unreachable code detected
namespace Metalama.Framework.Tests.TemplateTests.Return.SameVariableNameInDifferentBranches
{
  [CompileTime]
  internal class Aspect
  {
    // Test: When the same variable name is used in multiple if/else if/else branches,
    // the hoisting mechanism should handle them correctly as different symbols.
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
        goto __next1;
      if (meta.Target.Method.Name.Length < 5)
      {
        var result = meta.CompileTime("short");
        // meta.InsertComment( result );
        templateSyntaxFactory.AddComments(__s1, result);
        // return null;
        templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword)))));
        __skip1 = true;
      }
      else
      {
        if (meta.Target.Method.Name.Length > 10)
        {
          var result = meta.CompileTime("long");
          // meta.InsertComment( result );
          templateSyntaxFactory.AddComments(__s1, result);
          // return null;
          templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword)))));
          __skip1 = true;
        }
        else
        {
          var result = meta.CompileTime("medium");
          // meta.InsertComment( result );
          templateSyntaxFactory.AddComments(__s1, result);
        }
      }
      __next1:
        ;
      if (__skip1)
        goto __next2;
      // return meta.Proceed();
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.DynamicReturnStatement(templateSyntaxFactory.GetUserExpression(templateSyntaxFactory.Proceed("Proceed")), false)));
      __next2:
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