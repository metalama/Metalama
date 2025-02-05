[Override]
[Introduction]
internal class TargetClass
{
  public string this[string x]
  {
    get
    {
      global::System.Console.WriteLine("Override");
      Console.WriteLine("Original");
      return x;
    }
    set
    {
      global::System.Console.WriteLine("Override");
      Console.WriteLine("Original");
      x = value;
      return;
    }
  }
  public global::System.Object? this[global::System.Int32 index]
  {
    get
    {
      global::System.Console.WriteLine("Override");
      global::System.Console.WriteLine("Introduced.");
      return default(global::System.Object? );
    }
    set
    {
      global::System.Console.WriteLine("Override");
      global::System.Console.WriteLine("Introduced.");
      return;
    }
  }
}