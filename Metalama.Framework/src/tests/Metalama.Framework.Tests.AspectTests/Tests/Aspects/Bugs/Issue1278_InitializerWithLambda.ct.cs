using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Metalama.Framework.Aspects;
using System;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Issue1278_InitializerWithLambda;
[CompileTime]
public enum Permission
{
  NotSet,
  Read,
  Write
}
public class TheAspect : TypeAspect
{
  public static Permission RequestedPermission { get; set; } = Permission.Read;
  public SyntaxNode __IntroducedProperty(ITemplateSyntaxFactory templateSyntaxFactory)
  {
    List<StatementOrTrivia> __s1 = new List<StatementOrTrivia>();
    bool __skip1 = false;
    // returnmeta.RunTime( () => { if ( RequestedPermission == Permission.NotSet ) { return 0; } else { var requestedPermiss...
    templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(templateSyntaxFactory.SimplifyAnonymousFunction(SyntaxFactory.ParenthesizedLambdaExpression(default(SyntaxList<AttributeListSyntax>), default(SyntaxTokenList), null, SyntaxFactory.ParameterList(default(SeparatedSyntaxList<ParameterSyntax>)), SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken), SyntaxFactory.Block(default, new Func<SyntaxList<StatementSyntax>>(delegate
    {
      List<StatementOrTrivia> __s2 = new List<StatementOrTrivia>();
      bool __skip2 = false;
      ITemplateSyntaxFactory localTemplateSyntaxFactory1 = templateSyntaxFactory.ForLocalFunction("Y:global::System.Int32!", null, false);
      if (TheAspect.RequestedPermission == Permission.NotSet)
      {
        // return 0;
        localTemplateSyntaxFactory1.AddStatement(__s2, localTemplateSyntaxFactory1.AddSimplifierAnnotations(localTemplateSyntaxFactory1.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("0", 0)))));
        __skip2 = true;
      }
      else
      {
        if (__skip2)
          goto __next1;
        var requestedPermission = TheAspect.RequestedPermission != Permission.NotSet ? TheAspect.RequestedPermission : Permission.Read;
        __next1:
          ;
        Unsafe.SkipInit(out requestedPermission);
        if (__skip2)
          goto __next2;
        if (requestedPermission == Permission.Write)
        {
          // return 1;
          localTemplateSyntaxFactory1.AddStatement(__s2, localTemplateSyntaxFactory1.AddSimplifierAnnotations(localTemplateSyntaxFactory1.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("1", 1)))));
          __skip2 = true;
        }
        __next2:
          ;
        if (__skip2)
          goto __next3;
        // return 2;
        localTemplateSyntaxFactory1.AddStatement(__s2, localTemplateSyntaxFactory1.AddSimplifierAnnotations(localTemplateSyntaxFactory1.ReturnStatement(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal("2", 2)))));
        __next3:
          ;
        __skip2 = true;
      }
      return templateSyntaxFactory.ToStatementList(__s2);
    })()), null)))));
    return SyntaxFactory.Block(default, templateSyntaxFactory.ToStatementList(__s1));
  }
  public TheAspect()
  {
  }
  protected TheAspect(IArgumentsReader reader)
  {
  }
  public class Serializer : ReferenceTypeSerializer
  {
    public Serializer()
    {
    }
    public override object CreateInstance(Type type, IArgumentsReader constructorArguments)
    {
      return new TheAspect(constructorArguments);
    }
    public override void SerializeObject(object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments)
    {
    }
    public override void DeserializeFields(object obj, IArgumentsReader initializationArguments)
    {
    }
  }
}