class Target : Base
{
  public override int Foo
  {
    get
    {
      Console.WriteLine("Before");
      int x;
      x = base.Foo;
      Console.WriteLine("After");
      return x;
    }
  }
}