// Warning TEST0003 on `Compute`: `The redirection of 'Source.Compute( 21 )' was refused with ArgumentException: The template provider 'Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.TemplateRedirection.Fabric_ProjectFabric_Refused.Fabric' is a project fabric or a namespace fabric, which cannot provide templates. Declare the template in an aspect, a type fabric, or a class that implements ITemplateProvider.`
internal class Program
{
  public static int Execute() => Source.Compute(21);
}