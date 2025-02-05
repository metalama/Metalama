class Target
{
  int field;
  int Foo
  {
    set
    {
      Console.WriteLine("Before");
      Console.WriteLine("Original");
      this.field = value;
      Console.WriteLine("After");
    }
  }
}