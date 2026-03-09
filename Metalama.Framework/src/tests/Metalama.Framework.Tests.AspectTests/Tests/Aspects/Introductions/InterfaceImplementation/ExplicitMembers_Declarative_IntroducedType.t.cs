[Introduction]
public class TargetClass
{
  class TestType : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_Declarative_IntroducedType.IInterface
  {
    global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_Declarative_IntroducedType.IInterface.Property
    {
      get
      {
        global::System.Console.WriteLine("This is introduced interface member.");
        return default(global::System.Int32);
      }
      set
      {
        global::System.Console.WriteLine("This is introduced interface member.");
      }
    }
    global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_Declarative_IntroducedType.IInterface.InterfaceMethod()
    {
      global::System.Console.WriteLine("This is introduced interface member.");
      return default(global::System.Int32);
    }
  }
}
