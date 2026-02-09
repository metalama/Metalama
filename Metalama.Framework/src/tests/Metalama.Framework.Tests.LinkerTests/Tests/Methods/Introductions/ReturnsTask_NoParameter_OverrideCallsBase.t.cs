internal class Target
{
  private async Task Foo_Override()
  {
    Console.WriteLine("Before");
    Console.WriteLine("After");
  }
  public Task Foo()
  {
    return this.Foo_Override();
  }
}