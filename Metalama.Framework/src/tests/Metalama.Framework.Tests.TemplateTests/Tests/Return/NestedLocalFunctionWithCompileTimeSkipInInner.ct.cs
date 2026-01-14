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
namespace Metalama.Framework.Tests.TemplateTests.Return.NestedLocalFunctionWithCompileTimeSkipInInner
{
  [CompileTime]
  internal class Aspect
  {
    // Tests nested local function where the compile-time condition is inside the inner local function.
    private SyntaxNode __Template(ITemplateSyntaxFactory templateSyntaxFactory)
    {
      List<StatementOrTrivia> __s1 = new List<StatementOrTrivia>();
      SyntaxToken OuterFuncName = templateSyntaxFactory.GetUniqueIdentifier("OuterFunc");
      // return OuterFunc( meta.Proceed() );
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(templateSyntaxFactory.EscapeIdentifier(OuterFuncName)), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, templateSyntaxFactory.GetDynamicSyntax(templateSyntaxFactory.Proceed("Proceed"))))))))));
      SyntaxToken inputName = templateSyntaxFactory.GetUniqueIdentifier("input");
      // object? OuterFunc( object? input ) { return InnerFunc( input ); // Inner local function with compile-time condition i...
      templateSyntaxFactory.AddStatement(__s1, SyntaxFactory.LocalFunctionStatement(default(SyntaxList<AttributeListSyntax>), default(SyntaxTokenList), SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)), SyntaxFactory.Token(SyntaxKind.QuestionToken)), templateSyntaxFactory.EscapeIdentifier(OuterFuncName), null, SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(SyntaxFactory.Parameter(default(SyntaxList<AttributeListSyntax>), default(SyntaxTokenList), SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)), SyntaxFactory.Token(SyntaxKind.QuestionToken)), templateSyntaxFactory.EscapeIdentifier(inputName), null))), default(SyntaxList<TypeParameterConstraintClauseSyntax>), SyntaxFactory.Block(default, new Func<SyntaxList<StatementSyntax>>(delegate
      {
        List<StatementOrTrivia> __s2 = new List<StatementOrTrivia>();
        ITemplateSyntaxFactory localTemplateSyntaxFactory1 = templateSyntaxFactory.ForLocalFunction("Y:global::System.Object?!", null, false);
        SyntaxToken InnerFuncName = localTemplateSyntaxFactory1.GetUniqueIdentifier("InnerFunc");
        // return InnerFunc( input );
        localTemplateSyntaxFactory1.AddStatement(__s2, localTemplateSyntaxFactory1.AddSimplifierAnnotations(localTemplateSyntaxFactory1.ReturnStatement(localTemplateSyntaxFactory1.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(localTemplateSyntaxFactory1.EscapeIdentifier(InnerFuncName)), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, SyntaxFactory.IdentifierName(localTemplateSyntaxFactory1.EscapeIdentifier(inputName))))))))));
        SyntaxToken valueName = localTemplateSyntaxFactory1.GetUniqueIdentifier("value");
        // object? InnerFunc( object? value ) { // Compile-time condition inside inner local function if ( meta.Target.Method.Na...
        localTemplateSyntaxFactory1.AddStatement(__s2, SyntaxFactory.LocalFunctionStatement(default(SyntaxList<AttributeListSyntax>), default(SyntaxTokenList), SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)), SyntaxFactory.Token(SyntaxKind.QuestionToken)), localTemplateSyntaxFactory1.EscapeIdentifier(InnerFuncName), null, SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(SyntaxFactory.Parameter(default(SyntaxList<AttributeListSyntax>), default(SyntaxTokenList), SyntaxFactory.NullableType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)), SyntaxFactory.Token(SyntaxKind.QuestionToken)), localTemplateSyntaxFactory1.EscapeIdentifier(valueName), null))), default(SyntaxList<TypeParameterConstraintClauseSyntax>), SyntaxFactory.Block(default, new Func<SyntaxList<StatementSyntax>>(delegate
        {
          List<StatementOrTrivia> __s3 = new List<StatementOrTrivia>();
          bool __skip3 = false;
          ITemplateSyntaxFactory localTemplateSyntaxFactory2 = localTemplateSyntaxFactory1.ForLocalFunction("Y:global::System.Object?!", null, false);
          if (meta.Target.Method.Name == "Method")
          {
            // Console.WriteLine( "InnerFunc called" );
            localTemplateSyntaxFactory2.AddStatement(__s3, localTemplateSyntaxFactory2.ToStatement(localTemplateSyntaxFactory2.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(localTemplateSyntaxFactory2.AddSimplifierAnnotations(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, localTemplateSyntaxFactory2.AddSimplifierAnnotations(SyntaxFactory.QualifiedName(SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)), SyntaxFactory.Token(SyntaxKind.ColonColonToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("System"))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Console")))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("WriteLine")))), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("\"InnerFunc called\"", "InnerFunc called")))))))));
            // return value;
            localTemplateSyntaxFactory2.AddStatement(__s3, localTemplateSyntaxFactory2.AddSimplifierAnnotations(localTemplateSyntaxFactory2.ReturnStatement(SyntaxFactory.IdentifierName(localTemplateSyntaxFactory2.EscapeIdentifier(valueName)))));
            __skip3 = true;
          }
          if (__skip3)
            goto __next1;
          // return value;
          localTemplateSyntaxFactory2.AddStatement(__s3, localTemplateSyntaxFactory2.AddSimplifierAnnotations(localTemplateSyntaxFactory2.ReturnStatement(SyntaxFactory.IdentifierName(localTemplateSyntaxFactory2.EscapeIdentifier(valueName)))));
          __next1:
            ;
          return localTemplateSyntaxFactory1.ToStatementList(__s3);
        })()), null, default(SyntaxToken)));
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