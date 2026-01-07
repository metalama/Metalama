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
namespace Metalama.Framework.Tests.AspectTests.Templating.LocalVariables.CompileTimeVariableInSwitch;
[CompileTime]
internal class Aspect
{
  private SyntaxNode __Template(ITemplateSyntaxFactory templateSyntaxFactory)
  {
    List<StatementOrTrivia> __s1 = new List<StatementOrTrivia>();
    bool __skip1 = false;
    // switch (meta.Target.Parameters["x"].Value) { case 42: var method = meta.Target.Method; method.Invoke(0); break; }
    templateSyntaxFactory.AddStatement(__s1, SyntaxFactory.SwitchStatement(default(SyntaxList<AttributeListSyntax>), SyntaxFactory.Token(SyntaxKind.SwitchKeyword), SyntaxFactory.Token(SyntaxKind.OpenParenToken), templateSyntaxFactory.GetDynamicSyntax(meta.Target.Parameters["x"].Value), SyntaxFactory.Token(SyntaxKind.CloseParenToken), SyntaxFactory.Token(SyntaxKind.OpenBraceToken), default(SyntaxList<SwitchSectionSyntax>).AddRange(new SwitchSectionSyntax[] { SyntaxFactory.SwitchSection(default(SyntaxList<SwitchLabelSyntax>).AddRange(new SwitchLabelSyntax[] { SyntaxFactory.CaseSwitchLabel(SyntaxFactory.Token(SyntaxKind.CaseKeyword), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("42", 42)), SyntaxFactory.Token(SyntaxKind.ColonToken)) }), new Func<SyntaxList<StatementSyntax>>(delegate
    {
      List<StatementOrTrivia> __s2 = new List<StatementOrTrivia>();
      bool __skip2 = false;
      var method = meta.Target.Method;
      // method.Invoke(0);
      templateSyntaxFactory.AddStatement(__s2, templateSyntaxFactory.ToStatement(templateSyntaxFactory.GetDynamicSyntax(method.Invoke(templateSyntaxFactory.RunTimeExpression(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("0", 0)), "Y:global::System.Int32!")))));
      // break;
      templateSyntaxFactory.AddStatement(__s2, SyntaxFactory.BreakStatement(default(SyntaxList<AttributeListSyntax>), SyntaxFactory.Token(SyntaxKind.BreakKeyword), SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
      return templateSyntaxFactory.ToStatementList(__s2);
    })()) }), SyntaxFactory.Token(SyntaxKind.CloseBraceToken)));
    // return meta.Proceed();
    templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.DynamicReturnStatement(templateSyntaxFactory.GetUserExpression(templateSyntaxFactory.Proceed("Proceed")), false)));
    return SyntaxFactory.Block(default, templateSyntaxFactory.ToStatementList(__s1));
  }
}
internal class TargetCode
{
  private void Method(int x)
  {
  }
}