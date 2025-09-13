internal class Target
{
  private void Foo()
  {
    this.Foo_Override();
  }
  private void Foo_Source()
  {
    Console.WriteLine("Original");
  }
  private void Foo_Override()
  {
    Console.WriteLine("Before");
    this.Foo_Source();
    Console.WriteLine("After");
  }
}