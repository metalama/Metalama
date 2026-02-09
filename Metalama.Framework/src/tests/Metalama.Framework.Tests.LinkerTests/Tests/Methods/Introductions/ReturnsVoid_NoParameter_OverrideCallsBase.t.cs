internal class Target
{
  private void Foo_Override()
  {
    Console.WriteLine("Before");
    Console.WriteLine("After");
  }
  public void Foo()
  {
    this.Foo_Override();
  }
}
