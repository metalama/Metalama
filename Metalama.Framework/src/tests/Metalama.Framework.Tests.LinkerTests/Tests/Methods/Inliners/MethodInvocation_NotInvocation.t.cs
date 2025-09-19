internal class Target
{
  private void Foo()
  {
    Console.WriteLine("Before");
    var action = (Action)this.Foo_Source;
    Console.WriteLine("After");
  }
  private void Foo_Source()
  {
    Console.WriteLine("Original");
  }
}