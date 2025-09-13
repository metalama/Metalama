internal partial class Target
{
  [TheAspect]
  private partial int this[int i] { get; set; }
  private partial int this[int i]
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return 0;
    }
    set
    {
      global::System.Console.WriteLine("This is aspect code.");
      throw new Exception();
    }
  }
  private partial int this[string s] { get; set; }
  [TheAspect]
  private partial int this[string s]
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return 0;
    }
    set
    {
      global::System.Console.WriteLine("This is aspect code.");
      throw new Exception();
    }
  }
  [TheAspect]
  private partial int this[long i] { get; }
  private partial int this[long i]
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return 0;
    }
  }
}