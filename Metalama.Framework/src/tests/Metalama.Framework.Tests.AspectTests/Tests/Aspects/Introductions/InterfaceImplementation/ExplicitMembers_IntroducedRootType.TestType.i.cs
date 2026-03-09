namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_IntroducedRootType
{
  class TestType : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_IntroducedRootType.IInterface
  {
    global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_IntroducedRootType.IInterface.Property
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
    global::System.Int32 global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.ExplicitMembers_IntroducedRootType.IInterface.InterfaceMethod(global::System.Int32 i)
    {
      global::System.Console.WriteLine("This is introduced interface member.");
      return i;
    }
  }
}
