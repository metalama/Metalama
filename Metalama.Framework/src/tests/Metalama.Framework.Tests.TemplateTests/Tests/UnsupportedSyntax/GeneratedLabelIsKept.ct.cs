// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.
using Metalama.Framework.Aspects;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.Templating;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
namespace Metalama.Framework.Tests.AspectTests.Templating.UnsupportedSyntax.GeneratedLabelIsKept
{
  [CompileTime]
  internal class Aspect
  {
    // The template compiler rewriter generates a labeled statement and a goto statement of its own for a run-time
    // statement that follows a return inside a compile-time condition. That generation does not pass through the
    // template annotator, so the rejection of a label in a template does not affect it. The expected compiled
    // template beside this file is what pins it. See issue #1947.
    private SyntaxNode __Template(ITemplateSyntaxFactory templateSyntaxFactory)
    {
      List<StatementOrTrivia> __s1 = new List<StatementOrTrivia>();
      bool __skip1 = false;
      if (meta.Target.Method.Name.Length > 0)
      {
        // return meta.Proceed();
        templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.DynamicReturnStatement(templateSyntaxFactory.GetUserExpression(templateSyntaxFactory.Proceed("Proceed")), false)));
        __skip1 = true;
      }
      if (__skip1)
        goto __next1;
      // Console.WriteLine( "After the compile-time condition." );
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.ToStatement(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.QualifiedName(SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)), SyntaxFactory.Token(SyntaxKind.ColonColonToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("System"))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Console")))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("WriteLine")))), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("\"After the compile-time condition.\"", "After the compile-time condition.")))))))));
      __next1:
        ;
      if (__skip1)
        goto __next2;
      // return null;
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword)))));
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
      return "test";
    }
  }
}