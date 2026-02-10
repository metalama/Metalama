using System;
using Metalama.Framework.Aspects;
using Microsoft.CodeAnalysis;
using Metalama.Framework.CompileTimeContracts;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Metalama.Framework.Serialization;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug580_DynamicInInterpolatedString;
internal class LogAttribute : OverrideMethodAspect
{
  public override object? OverrideMethod() => throw new System.NotSupportedException("Template code cannot be directly executed.");
  public virtual SyntaxNode __OverrideMethod(ITemplateSyntaxFactory templateSyntaxFactory)
  {
    List<StatementOrTrivia> __s1 = new List<StatementOrTrivia>();
    SyntaxToken resultName = templateSyntaxFactory.GetUniqueIdentifier("result");
    // var result = meta.Proceed();
    templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.DynamicLocalDeclaration(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("var")), templateSyntaxFactory.EscapeIdentifier(resultName), templateSyntaxFactory.GetUserExpression(templateSyntaxFactory.Proceed("Proceed")), false)));
    // Console.WriteLine( $"Method returned: {result}" );
    templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.ToStatement(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.QualifiedName(SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)), SyntaxFactory.Token(SyntaxKind.ColonColonToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("System"))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Console")))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("WriteLine")))), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, templateSyntaxFactory.RenderInterpolatedString(SyntaxFactory.InterpolatedStringExpression(SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken), SyntaxFactory.List<InterpolatedStringContentSyntax>(new InterpolatedStringContentSyntax[] { SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, "Method returned: ", "Method returned: ", default)), templateSyntaxFactory.FixInterpolationSyntax(SyntaxFactory.Interpolation(SyntaxFactory.Token(SyntaxKind.OpenBraceToken), SyntaxFactory.IdentifierName(templateSyntaxFactory.EscapeIdentifier(resultName)), null, null, SyntaxFactory.Token(SyntaxKind.CloseBraceToken))) }), SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken))))))))));
    if (meta.Target.Parameters.Count > 0)
    {
      SyntaxToken result2Name = templateSyntaxFactory.GetUniqueIdentifier("result2");
      // var result2 = meta.Target.Parameters[0].Value;
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.DynamicLocalDeclaration(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("var")), templateSyntaxFactory.EscapeIdentifier(result2Name), templateSyntaxFactory.GetUserExpression(meta.Target.Parameters[0].Value), false)));
      // Console.WriteLine( $" First param value: {result2}" );
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.ToStatement(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.QualifiedName(SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)), SyntaxFactory.Token(SyntaxKind.ColonColonToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("System"))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Console")))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("WriteLine")))), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, templateSyntaxFactory.RenderInterpolatedString(SyntaxFactory.InterpolatedStringExpression(SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken), SyntaxFactory.List<InterpolatedStringContentSyntax>(new InterpolatedStringContentSyntax[] { SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, "  First param value: ", "  First param value: ", default)), templateSyntaxFactory.FixInterpolationSyntax(SyntaxFactory.Interpolation(SyntaxFactory.Token(SyntaxKind.OpenBraceToken), SyntaxFactory.IdentifierName(templateSyntaxFactory.EscapeIdentifier(result2Name)), null, null, SyntaxFactory.Token(SyntaxKind.CloseBraceToken))) }), SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken))))))))));
    }
    else
    {
      SyntaxToken result2Name_1 = templateSyntaxFactory.GetUniqueIdentifier("result2");
      // var result2 = meta.Proceed();
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.AddSimplifierAnnotations(templateSyntaxFactory.DynamicLocalDeclaration(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("var")), templateSyntaxFactory.EscapeIdentifier(result2Name_1), templateSyntaxFactory.GetUserExpression(templateSyntaxFactory.Proceed("Proceed")), false)));
      // Console.WriteLine( $" No params, result2: {result2}" );
      templateSyntaxFactory.AddStatement(__s1, templateSyntaxFactory.ToStatement(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.InvocationExpression(templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, templateSyntaxFactory.AddSimplifierAnnotations(SyntaxFactory.QualifiedName(SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)), SyntaxFactory.Token(SyntaxKind.ColonColonToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("System"))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("Console")))), SyntaxFactory.Token(SyntaxKind.DotToken), SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("WriteLine")))), SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(SyntaxFactory.Argument(null, default, templateSyntaxFactory.RenderInterpolatedString(SyntaxFactory.InterpolatedStringExpression(SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken), SyntaxFactory.List<InterpolatedStringContentSyntax>(new InterpolatedStringContentSyntax[] { SyntaxFactory.InterpolatedStringText(SyntaxFactory.Token(default, SyntaxKind.InterpolatedStringTextToken, "  No params, result2: ", "  No params, result2: ", default)), templateSyntaxFactory.FixInterpolationSyntax(SyntaxFactory.Interpolation(SyntaxFactory.Token(SyntaxKind.OpenBraceToken), SyntaxFactory.IdentifierName(templateSyntaxFactory.EscapeIdentifier(result2Name_1)), null, null, SyntaxFactory.Token(SyntaxKind.CloseBraceToken))) }), SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken))))))))));
    }
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