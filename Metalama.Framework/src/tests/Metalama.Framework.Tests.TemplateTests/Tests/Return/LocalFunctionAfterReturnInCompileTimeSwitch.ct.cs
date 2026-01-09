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
namespace Metalama.Framework.Tests.TemplateTests.Return.LocalFunctionAfterReturnInCompileTimeSwitch
{
  [CompileTime]
  internal class Aspect
  {
    // Local function defined after a return in compile-time switch should be generated.
    private SyntaxNode __Template(ITemplateSyntaxFactory templateSyntaxFactory)
    {
      List<StatementOrTrivia> __s1 = new List<StatementOrTrivia>();
      bool __skip1 = false;
      SyntaxToken LocalFuncName = templateSyntaxFactory.GetUniqueIdentifier("LocalFunc");
      switch (meta.Target.Method.Parameters.Count)
      {
        case 0:
          // return meta.Proceed();
          templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.DynamicReturnStatement(templateSyntaxFactory.GetUserExpression(templateSyntaxFactory.Proceed("Proceed")), false)));
          __skip1 = true;
          break;
        case 1:
          if (__skip1)
            goto __next1;
          // return LocalFunc( meta.Proceed() );
          templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(templateSyntaxFactory.EscapeIdentifier(LocalFuncName)), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, templateSyntaxFactory.GetDynamicSyntax(templateSyntaxFactory.Proceed("Proceed"))))))))));
          __next1:
            ;
          __skip1 = true;
          break;
        default:
          if (__skip1)
            goto __next2;
          // return null;
          templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression, SyntaxFactory.Token(SyntaxKind.NullKeyword)))));
          __next2:
            ;
          __skip1 = true;
          break;
      }
      SyntaxToken inputName = templateSyntaxFactory.GetUniqueIdentifier("input");
      // object? LocalFunc( object? input ) { Console.WriteLine( "LocalFunc called" ); return input; }
      templateSyntaxFactory.AddStatement(__s1, SyntaxFactory.LocalFunctionStatement(default(SyntaxList<AttributeListSyntax>), default(SyntaxTokenList), SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)), SyntaxFactory.Token(SyntaxKind.QuestionToken)), templateSyntaxFactory.EscapeIdentifier(LocalFuncName), null, SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(SyntaxFactory.Parameter(default(SyntaxList<AttributeListSyntax>), default(SyntaxTokenList), SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)), SyntaxFactory.Token(SyntaxKind.QuestionToken)), templateSyntaxFactory.EscapeIdentifier(inputName), null))), default(SyntaxList<TypeParameterConstraintClauseSyntax>), SyntaxFactory.Block(default, new Func<SyntaxList<StatementSyntax>>(delegate
      {
        List<StatementOrTrivia> __s2 = new List<StatementOrTrivia>();
        bool __skip2 = false;
        ITemplateSyntaxFactory localTemplateSyntaxFactory1 = templateSyntaxFactory.ForLocalFunction("Y:global::System.Object?!", null, false);
        // Console.WriteLine( "LocalFunc called" );
        localTemplateSyntaxFactory1.AddStatement(__s2, localTemplateSyntaxFactory1.ToStatement(localTemplateSyntaxFactory1.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(localTemplateSyntaxFactory1.AddSimplifierAnnotations(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, localTemplateSyntaxFactory1.AddSimplifierAnnotations(SyntaxFactory.QualifiedName(SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)), SyntaxFactory.Token(SyntaxKind.ColonColonToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("System"))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Console")))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("WriteLine")))), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("\"LocalFunc called\"", "LocalFunc called")))))))));
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
    private object? Method(int x)
    {
      return null;
    }
  }
}