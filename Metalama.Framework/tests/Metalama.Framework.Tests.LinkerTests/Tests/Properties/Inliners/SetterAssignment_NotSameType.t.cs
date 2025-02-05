class Target : Base
{
  public override int Foo
  {
    set
    {
      Console.WriteLine("Before");
      base.Foo = value;
      Console.WriteLine("After");
    }
  }
}