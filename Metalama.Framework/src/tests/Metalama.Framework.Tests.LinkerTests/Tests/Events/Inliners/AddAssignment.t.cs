public class Target
{
  private EventHandler? field;
  event EventHandler Foo
  {
    add
    {
      Console.WriteLine("Before");
      Console.WriteLine("Original");
      this.field += value;
      Console.WriteLine("After");
    }
    remove
    {
    }
  }
}