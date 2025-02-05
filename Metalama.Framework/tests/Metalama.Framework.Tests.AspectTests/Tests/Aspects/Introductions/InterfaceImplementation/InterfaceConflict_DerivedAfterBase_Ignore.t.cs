[Introduction]
public class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.InterfaceConflict_DerivedAfterBase_Ignore.IBaseInterface, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.InterfaceConflict_DerivedAfterBase_Ignore.IDerivedInterface
{
  public global::System.Int32 Bar()
  {
    global::System.Console.WriteLine("This is introduced interface member by Derived (should be Derived).");
    return default(global::System.Int32);
  }
  public global::System.Int32 Foo()
  {
    global::System.Console.WriteLine("This is introduced interface member by Base (should be Base).");
    return default(global::System.Int32);
  }
}