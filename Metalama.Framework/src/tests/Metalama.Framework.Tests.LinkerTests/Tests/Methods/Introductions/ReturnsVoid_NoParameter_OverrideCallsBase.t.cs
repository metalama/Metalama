internal class Target
{
  public void Foo()
  {
    this.Foo_Override();
  }
  private void Foo_Override()
  {
    Console.WriteLine("Before");
    Console.WriteLine("After");
  }
}
