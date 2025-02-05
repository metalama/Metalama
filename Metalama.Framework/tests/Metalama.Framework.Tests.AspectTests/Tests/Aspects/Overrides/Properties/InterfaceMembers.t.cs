public interface Interface
{
  [Override]
  private int PrivateProperty
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Original implementation");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Original implementation");
    }
  }
  private static int _staticAutoProperty;
  [Override]
  public static int StaticAutoProperty
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      return global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.InterfaceMembers.Interface._staticAutoProperty;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Properties.InterfaceMembers.Interface._staticAutoProperty = value;
    }
  }
  [Override]
  public static int StaticProperty
  {
    get
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Original implementation");
      return 42;
    }
    set
    {
      global::System.Console.WriteLine("Override.");
      Console.WriteLine("Original implementation");
    }
  }
}
public class TargetClass : Interface
{
}