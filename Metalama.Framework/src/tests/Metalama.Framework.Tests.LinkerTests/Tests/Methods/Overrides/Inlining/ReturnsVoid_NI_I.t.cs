internal class Target
{
  private void Foo()
  {
    this.Foo_Override();
  }
  private void Foo_Override()
  {
    Console.WriteLine("Before");
    Console.WriteLine("Original");
    Console.WriteLine("After");
  }
}