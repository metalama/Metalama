namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.CrossAssembly_TypeOf
{
  // Inherits TypeOfAspect from BaseClass transitively across assemblies.
  // This triggers deserialization of TransitiveAspectsManifest.
  public class DerivedClass : BaseClass
  {
  }
}