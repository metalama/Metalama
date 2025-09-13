internal partial class Target
{
  [TheAspect]
  private partial int P1 { get; set; }
  private partial int P1
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
  private partial int P2 { get; set; }
  [TheAspect]
  private partial int P2
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
  private partial int P3 { get; }
  private partial int P3
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return 0;
    }
  }
}