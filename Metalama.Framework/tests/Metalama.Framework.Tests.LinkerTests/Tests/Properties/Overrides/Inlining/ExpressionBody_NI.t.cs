class Target
{
  int Foo
  {
    get
    {
      Console.WriteLine("Before1");
      var x = this.Foo_Source;
      Console.WriteLine("After1");
      return x;
    }
  }
  private int Foo_Source => 42;
}