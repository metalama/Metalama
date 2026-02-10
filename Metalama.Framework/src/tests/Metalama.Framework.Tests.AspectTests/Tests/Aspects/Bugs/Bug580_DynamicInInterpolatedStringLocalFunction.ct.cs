using System;
using Metalama.Framework.Aspects;
using Microsoft.CodeAnalysis;
using Metalama.Framework.CompileTimeContracts;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Metalama.Framework.Serialization;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug580_DynamicInInterpolatedStringLocalFunction;
internal class LogAttribute : OverrideMethodAspect
{
  public override object? OverrideMethod() => throw new System.NotSupportedException("Template code cannot be directly executed.");
  public virtual SyntaxNode __OverrideMethod(ITemplateSyntaxFactory templateSyntaxFactory)
  {
    List<StatementOrTrivia> __s1 = new List<StatementOrTrivia>();
    SyntaxToken LogResultName = templateSyntaxFactory.GetUniqueIdentifier("LogResult");
    SyntaxToken resultName = templateSyntaxFactory.GetUniqueIdentifier("result");
    // var result = meta.Proceed();
    templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.DynamicLocalDeclaration(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("var")), templateSyntaxFactory.EscapeIdentifier(resultName), templateSyntaxFactory.GetUserExpression(templateSyntaxFactory.Proceed("Proceed")), false)));
    // void LogResult() { Console.WriteLine( $"Method returned: {result}" ); }
    templateSyntaxFactory.AddStatement(__s1, SyntaxFactory.LocalFunctionStatement(default(SyntaxList<AttributeListSyntax>), default(SyntaxTokenList), SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), templateSyntaxFactory.EscapeIdentifier(LogResultName), null, SyntaxFactory.ParameterList(default(SeparatedSyntaxList<ParameterSyntax>)), default(SyntaxList<TypeParameterConstraintClauseSyntax>), SyntaxFactory.Block(default, new Func<SyntaxList<StatementSyntax>>(delegate
    {
      List<StatementOrTrivia> __s2 = new List<StatementOrTrivia>();
      ITemplateSyntaxFactory localTemplateSyntaxFactory1 = templateSyntaxFactory.ForLocalFunction("Y:void!", null, false);
      // Console.WriteLine( $"Method returned: {result}" );
      localTemplateSyntaxFactory1.AddStatement(__s2, localTemplateSyntaxFactory1.ToStatement(localTemplateSyntaxFactory1.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(localTemplateSyntaxFactory1.AddSimplifierAnnotations(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, localTemplateSyntaxFactory1.AddSimplifierAnnotations(SyntaxFactory.QualifiedName(SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)), SyntaxFactory.Token(SyntaxKind.ColonColonToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("System"))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Console")))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("WriteLine")))), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, localTemplateSyntaxFactory1.RenderInterpolatedString(SyntaxFactory.InterpolatedStringExpression(SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken), SyntaxFactory.List<InterpolatedStringContentSyntax>(new InterpolatedStringContentSyntax[] { SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, "Method returned: ", "Method returned: ", default)), localTemplateSyntaxFactory1.FixInterpolationSyntax(SyntaxFactory.Interpolation(SyntaxFactory.Token(SyntaxKind.OpenBraceToken), SyntaxFactory.IdentifierName(localTemplateSyntaxFactory1.EscapeIdentifier(resultName)), null, null, SyntaxFactory.Token(SyntaxKind.CloseBraceToken))) }), SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken))))))))));
      return templateSyntaxFactory.ToStatementList(__s2);
    })()), null, default(SyntaxToken)));
    // LogResult();
    templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.ToStatement(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(templateSyntaxFactory.EscapeIdentifier(LogResultName)), SyntaxFactory.ArgumentList(default(SeparatedSyntaxList<ArgumentSyntax>))))));
    // return result;
    templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(templateSyntaxFactory.EscapeIdentifier(resultName)))));
    return SyntaxFactory.Block(default, templateSyntaxFactory.ToStatementList(__s1));
  }
  public LogAttribute()
  {
  }
  protected LogAttribute(IArgumentsReader reader)
  {
  }
  public class Serializer : ReferenceTypeSerializer
  {
    public Serializer()
    {
    }
    public override object CreateInstance(Type type, IArgumentsReader constructorArguments)
    {
      return new LogAttribute(constructorArguments);
    }
    public override void SerializeObject(object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments)
    {
    }
    public override void DeserializeFields(object obj, IArgumentsReader initializationArguments)
    {
    }
  }
}
