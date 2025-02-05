public class Target
{
  private EventHandler? field;
  public event EventHandler? Foo
  {
    add
    {
    }
    remove
    {
      Console.WriteLine("Before");
      Console.WriteLine("Original");
      this.field -= value;
      Console.WriteLine("After");
    }
  }
}