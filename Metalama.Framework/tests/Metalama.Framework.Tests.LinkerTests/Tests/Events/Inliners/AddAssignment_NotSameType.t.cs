public class Target : Base
{
  protected override event EventHandler Foo
  {
    add
    {
      Console.WriteLine("Before");
      base.Foo += value;
      Console.WriteLine("After");
    }
    remove
    {
    }
  }
}