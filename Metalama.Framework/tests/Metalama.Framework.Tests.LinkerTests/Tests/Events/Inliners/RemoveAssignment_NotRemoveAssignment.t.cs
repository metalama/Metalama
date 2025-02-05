public class Target
{
  private EventHandler? field;
  event EventHandler Foo
  {
    add
    {
    }
    remove
    {
      Console.WriteLine("Before");
      this.Foo_Source += null;
      Console.WriteLine("After");
    }
  }
  private event EventHandler Foo_Source
  {
    add
    {
    }
    remove
    {
      Console.WriteLine("Original");
      this.field -= value;
    }
  }
}