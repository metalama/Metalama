using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;
using Metalama.Framework.CompileTimeContracts;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Metalama.Framework.Serialization;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug580_DynamicParameterInInterpolatedString;
internal class LogAttribute : OverrideMethodAspect
{
  public override object? OverrideMethod() => throw new System.NotSupportedException("Template code cannot be directly executed.");
  public virtual SyntaxNode __OverrideMethod(ITemplateSyntaxFactory templateSyntaxFactory)
  {
    List<StatementOrTrivia> __s1 = new List<StatementOrTrivia>();
    foreach (var parameter in meta.Target.Parameters)
      if (parameter.RefKind != Code.RefKind.Out)
      {
        // Console.WriteLine( $" {parameter.Name} = {parameter.Value}" );
        templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.ToStatement(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.QualifiedName(SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)), SyntaxFactory.Token(SyntaxKind.ColonColonToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("System"))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Console")))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("WriteLine")))), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, templateSyntaxFactory.RenderInterpolatedString(SyntaxFactory.InterpolatedStringExpression(SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken), SyntaxFactory.List<InterpolatedStringContentSyntax>(new InterpolatedStringContentSyntax[] { SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, "  ", "  ", default)), SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, $"{parameter.Name}", $"{parameter.Name}", default)), SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, " = ", " = ", default)), templateSyntaxFactory.FixInterpolationSyntax(SyntaxFactory.Interpolation(SyntaxFactory.Token(SyntaxKind.OpenBraceToken), templateSyntaxFactory.GetDynamicSyntax(parameter.Value), null, null, SyntaxFactory.Token(SyntaxKind.CloseBraceToken))) }), SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken))))))))));
      }
    // return meta.Proceed();
    templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.DynamicReturnStatement(templateSyntaxFactory.GetUserExpression(templateSyntaxFactory.Proceed("Proceed")), false)));
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