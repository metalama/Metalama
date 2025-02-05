class Target : Base
{
  public override int Foo
  {
    get
    {
      Console.WriteLine("Before");
      return base.Foo;
    }
  }
}