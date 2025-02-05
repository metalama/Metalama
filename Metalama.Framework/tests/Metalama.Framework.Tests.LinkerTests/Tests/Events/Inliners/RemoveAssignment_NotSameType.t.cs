public class Target : Base
{
  protected override event EventHandler Foo
  {
    add
    {
    }
    remove
    {
      Console.WriteLine("Before");
      base.Foo -= value;
      Console.WriteLine("After");
    }
  }
}