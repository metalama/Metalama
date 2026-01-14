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
namespace Metalama.Framework.Tests.TemplateTests.Return.ReturnInNestedRunTimeIf
{
  [CompileTime]
  internal class Aspect
  {
    public bool Property { get; set; }
    // Run-time if with return - the if statement and return are both emitted
    // to the output. Compile-time flow continues because the run-time condition
    // might not be true at run-time.
    private SyntaxNode __Template(ITemplateSyntaxFactory templateSyntaxFactory)
    {
      List<StatementOrTrivia> __s1 = new List<StatementOrTrivia>();
      bool __skip1 = false;
      if (this.Property)
      {
        // return meta.Proceed();
        templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.DynamicReturnStatement(templateSyntaxFactory.GetUserExpression(templateSyntaxFactory.Proceed("Proceed")), false)));
        __skip1 = true;
      }
      if (__skip1)
        goto __next1;
      // if ( meta.Target.Parameters[0].Value == null ) { if ( meta.Target.Parameters[1].Value == null ) { Console.WriteLine( ...
      templateSyntaxFactory.AddStatement(__s1, SyntaxFactory.IfStatement(default(SyntaxList<AttributeListSyntax>), SyntaxFactory.Token(SyntaxKind.IfKeyword), SyntaxFactory.Token(SyntaxKind.OpenParenToken), SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, templateSyntaxFactory.GetDynamicSyntax(meta.Target.Parameters[0].Value), SyntaxFactory.Token(SyntaxKind.EqualsEqualsToken), SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword))), SyntaxFactory.Token(SyntaxKind.CloseParenToken), SyntaxFactory.Block(default, new Func<SyntaxList<StatementSyntax>>(delegate
      {
        List<StatementOrTrivia> __s2 = new List<StatementOrTrivia>();
        // if ( meta.Target.Parameters[1].Value == null ) { Console.WriteLine( $"{meta.Target.Parameters[1]} is null." ); return...
        templateSyntaxFactory.AddStatement(__s2, SyntaxFactory.IfStatement(default(SyntaxList<AttributeListSyntax>), SyntaxFactory.Token(SyntaxKind.IfKeyword), SyntaxFactory.Token(SyntaxKind.OpenParenToken), SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, templateSyntaxFactory.GetDynamicSyntax(meta.Target.Parameters[1].Value), SyntaxFactory.Token(SyntaxKind.EqualsEqualsToken), SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword))), SyntaxFactory.Token(SyntaxKind.CloseParenToken), SyntaxFactory.Block(default, new Func<SyntaxList<StatementSyntax>>(delegate
        {
          List<StatementOrTrivia> __s3 = new List<StatementOrTrivia>();
          // Console.WriteLine( $"{meta.Target.Parameters[1]} is null." );
          templateSyntaxFactory.AddStatement(__s3, templateSyntaxFactory.ToStatement(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.QualifiedName(SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)), SyntaxFactory.Token(SyntaxKind.ColonColonToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("System"))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Console")))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("WriteLine")))), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, templateSyntaxFactory.RunTimeExpression(templateSyntaxFactory.StringLiteralExpression($"{(meta.Target.Parameters[1])} is null."), "Y:global::System.String!"))))))));
          // return null;
          templateSyntaxFactory.AddStatement(__s3, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword)))));
          return templateSyntaxFactory.ToStatementList(__s3);
        })()), null));
        // Console.WriteLine( $"{meta.Target.Parameters[0]} is null." );
        templateSyntaxFactory.AddStatement(__s2, templateSyntaxFactory.ToStatement(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.QualifiedName(SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)), SyntaxFactory.Token(SyntaxKind.ColonColonToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("System"))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Console")))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("WriteLine")))), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, templateSyntaxFactory.RunTimeExpression(templateSyntaxFactory.StringLiteralExpression($"{(meta.Target.Parameters[0])} is null."), "Y:global::System.String!"))))))));
        return templateSyntaxFactory.ToStatementList(__s2);
      })()), null));
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
    private object? Method(object? a, object? b)
    {
      return a;
    }
  }
}