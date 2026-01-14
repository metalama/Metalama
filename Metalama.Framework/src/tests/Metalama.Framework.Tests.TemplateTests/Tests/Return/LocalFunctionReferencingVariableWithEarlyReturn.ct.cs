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
namespace Metalama.Framework.Tests.TemplateTests.Return.LocalFunctionReferencingVariableWithEarlyReturn
{
  [CompileTime]
  internal class Aspect
  {
    // Edge case: Local function references a variable declared before it,
    // and there's an early return in a compile-time conditional.
    private SyntaxNode __Template(ITemplateSyntaxFactory templateSyntaxFactory)
    {
      List<StatementOrTrivia> __s1 = new List<StatementOrTrivia>();
      bool __skip1 = false;
      SyntaxToken LocalFuncName = templateSyntaxFactory.GetUniqueIdentifier("LocalFunc");
      SyntaxToken messageName = templateSyntaxFactory.GetUniqueIdentifier("message");
      // var message = "Hello";
      templateSyntaxFactory.AddStatement(__s1, SyntaxFactory.LocalDeclarationStatement(default(SyntaxList<AttributeListSyntax>), default(SyntaxToken), default(SyntaxToken), default(SyntaxTokenList), SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("var")), SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(templateSyntaxFactory.EscapeIdentifier(messageName), default(BracketedArgumentListSyntax), SyntaxFactory.EqualsValueClause(SyntaxFactory.Token(SyntaxKind.EqualsToken), SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("\"Hello\"", "Hello")))))), SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
      if (meta.Target.Method.Name.Length > 0)
      {
        // return LocalFunc( meta.Proceed() );
        templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(templateSyntaxFactory.EscapeIdentifier(LocalFuncName)), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, templateSyntaxFactory.GetDynamicSyntax(templateSyntaxFactory.Proceed("Proceed"))))))))));
        __skip1 = true;
      }
      if (__skip1)
        goto __next1;
      // return null;
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword)))));
      __next1:
        ;
      SyntaxToken inputName = templateSyntaxFactory.GetUniqueIdentifier("input");
      // object? LocalFunc( object? input ) { Console.WriteLine( message ); return input; }
      templateSyntaxFactory.AddStatement(__s1, SyntaxFactory.LocalFunctionStatement(default(SyntaxList<AttributeListSyntax>), default(SyntaxTokenList), SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)), SyntaxFactory.Token(SyntaxKind.QuestionToken)), templateSyntaxFactory.EscapeIdentifier(LocalFuncName), null, SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(SyntaxFactory.Parameter(default(SyntaxList<AttributeListSyntax>), default(SyntaxTokenList), SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)), SyntaxFactory.Token(SyntaxKind.QuestionToken)), templateSyntaxFactory.EscapeIdentifier(inputName), null))), default(SyntaxList<TypeParameterConstraintClauseSyntax>), SyntaxFactory.Block(default, new Func<SyntaxList<StatementSyntax>>(delegate
      {
        List<StatementOrTrivia> __s2 = new List<StatementOrTrivia>();
        ITemplateSyntaxFactory localTemplateSyntaxFactory1 = templateSyntaxFactory.ForLocalFunction("Y:global::System.Object?!", null, false);
        // Console.WriteLine( message );
        localTemplateSyntaxFactory1.AddStatement(__s2, localTemplateSyntaxFactory1.ToStatement(localTemplateSyntaxFactory1.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(localTemplateSyntaxFactory1.AddSimplifierAnnotations(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, localTemplateSyntaxFactory1.AddSimplifierAnnotations(SyntaxFactory.QualifiedName(SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)), SyntaxFactory.Token(SyntaxKind.ColonColonToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("System"))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Console")))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("WriteLine")))), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, SyntaxFactory.IdentifierName(localTemplateSyntaxFactory1.EscapeIdentifier(messageName)))))))));
        // return input;
        localTemplateSyntaxFactory1.AddStatement(__s2, localTemplateSyntaxFactory1.AddSimplifierAnnotations(localTemplateSyntaxFactory1.ReturnStatement(SyntaxFactory.IdentifierName(localTemplateSyntaxFactory1.EscapeIdentifier(inputName)))));
        return templateSyntaxFactory.ToStatementList(__s2);
      })()), null, default(SyntaxToken)));
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