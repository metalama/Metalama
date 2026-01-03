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
namespace Metalama.Framework.Tests.TemplateTests.Return.LocalFunctionInsideRunTimeElse
{
  [CompileTime]
  internal class Aspect
  {
    // Tests local function defined inside a run-time else block.
    private SyntaxNode __Template(ITemplateSyntaxFactory templateSyntaxFactory)
    {
      List<StatementOrTrivia> __s1 = new List<StatementOrTrivia>();
      bool __skip1 = false;
      var p = meta.Target.Parameters[0];
      // if ( p.Value != null ) { return meta.Proceed(); } else { // Compile-time condition inside run-time else if ( meta.Tar...
      templateSyntaxFactory.AddStatement(__s1, SyntaxFactory.IfStatement(default(SyntaxList<AttributeListSyntax>), SyntaxFactory.Token(SyntaxKind.IfKeyword), SyntaxFactory.Token(SyntaxKind.OpenParenToken), SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression, templateSyntaxFactory.GetDynamicSyntax(p.Value), SyntaxFactory.Token(SyntaxKind.ExclamationEqualsToken), SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword))), SyntaxFactory.Token(SyntaxKind.CloseParenToken), SyntaxFactory.Block(default, new Func<SyntaxList<StatementSyntax>>(delegate
      {
        List<StatementOrTrivia> __s2 = new List<StatementOrTrivia>();
        bool __skip2 = false;
        // return meta.Proceed();
        templateSyntaxFactory.AddStatement(__s2, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.DynamicReturnStatement(templateSyntaxFactory.GetUserExpression(templateSyntaxFactory.Proceed("Proceed")), false)));
        return templateSyntaxFactory.ToStatementList(__s2);
      })()), SyntaxFactory.ElseClause(SyntaxFactory.Token(SyntaxKind.ElseKeyword), SyntaxFactory.Block(default, new Func<SyntaxList<StatementSyntax>>(delegate
      {
        List<StatementOrTrivia> __s3 = new List<StatementOrTrivia>();
        bool __skip3 = false;
        SyntaxToken LocalFuncName = templateSyntaxFactory.GetUniqueIdentifier("LocalFunc");
        if (meta.Target.Method.Name == "Method")
        {
          // return LocalFunc( "null input" );
          templateSyntaxFactory.AddStatement(__s3, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(templateSyntaxFactory.EscapeIdentifier(LocalFuncName)), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("\"null input\"", "null input"))))))))));
          __skip3 = true;
          if (!__skip3)
          {
            // return null;
            templateSyntaxFactory.AddStatement(__s3, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword)))));
          }
          __skip3 = true;
        }
        SyntaxToken inputName = templateSyntaxFactory.GetUniqueIdentifier("input");
        // object? LocalFunc( object? input ) { global::System.Console.WriteLine( input ); return input; }
        templateSyntaxFactory.AddStatement(__s3, SyntaxFactory.LocalFunctionStatement(default(SyntaxList<AttributeListSyntax>), default(SyntaxTokenList), SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)), SyntaxFactory.Token(SyntaxKind.QuestionToken)), templateSyntaxFactory.EscapeIdentifier(LocalFuncName), null, SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(SyntaxFactory.Parameter(default(SyntaxList<AttributeListSyntax>), default(SyntaxTokenList), SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)), SyntaxFactory.Token(SyntaxKind.QuestionToken)), templateSyntaxFactory.EscapeIdentifier(inputName), null))), default(SyntaxList<TypeParameterConstraintClauseSyntax>), SyntaxFactory.Block(default, new Func<SyntaxList<StatementSyntax>>(delegate
        {
          List<StatementOrTrivia> __s4 = new List<StatementOrTrivia>();
          bool __skip4 = false;
          ITemplateSyntaxFactory localTemplateSyntaxFactory1 = templateSyntaxFactory.ForLocalFunction("Y:global::System.Object?!", null, false);
          // global::System.Console.WriteLine( input );
          localTemplateSyntaxFactory1.AddStatement(__s4, localTemplateSyntaxFactory1.ToStatement(localTemplateSyntaxFactory1.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(localTemplateSyntaxFactory1.AddSimplifierAnnotations(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, localTemplateSyntaxFactory1.AddSimplifierAnnotations(localTemplateSyntaxFactory1.AddSimplifierAnnotations(SyntaxFactory.QualifiedName(SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)), SyntaxFactory.Token(SyntaxKind.ColonColonToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("System"))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Console"))))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("WriteLine")))), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, SyntaxFactory.IdentifierName(localTemplateSyntaxFactory1.EscapeIdentifier(inputName)))))))));
          // return input;
          localTemplateSyntaxFactory1.AddStatement(__s4, localTemplateSyntaxFactory1.AddSimplifierAnnotations(localTemplateSyntaxFactory1.ReturnStatement(SyntaxFactory.IdentifierName(localTemplateSyntaxFactory1.EscapeIdentifier(inputName)))));
          return templateSyntaxFactory.ToStatementList(__s4);
        })()), null, default(SyntaxToken)));
        return templateSyntaxFactory.ToStatementList(__s3);
      })()))));
      // return null;
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword)))));
      return SyntaxFactory.Block(default, templateSyntaxFactory.ToStatementList(__s1));
    }
  }
  internal class TargetCode
  {
    // <target>
    private object? Method(object? x)
    {
      return x;
    }
  }
}