[Introduction]
public class TargetClass
{
  class TestType : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_IntroducedType.IInterface
  {
    global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_IntroducedType.IInterface.Property
    {
      get
      {
        global::System.Console.WriteLine("This is introduced interface member.");
        return (global::System.Int32)42;
      }
      set
      {
        global::System.Console.WriteLine("This is introduced interface member.");
      }
    }
    global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_IntroducedType.IInterface.InterfaceMethod(global::System.Int32 i)
    {
      global::System.Console.WriteLine("This is introduced interface member.");
      return i;
    }
  }
}
