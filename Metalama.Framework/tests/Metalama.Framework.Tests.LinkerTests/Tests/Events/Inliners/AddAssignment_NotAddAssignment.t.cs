public class Target
{
  private EventHandler? field;
  event EventHandler Foo
  {
    add
    {
      Console.WriteLine("Before");
      this.Foo_Source -= null;
      Console.WriteLine("After");
    }
    remove
    {
    }
  }
  private event EventHandler Foo_Source
  {
    add
    {
      Console.WriteLine("Original");
      this.field += value;
    }
    remove
    {
    }
  }
}