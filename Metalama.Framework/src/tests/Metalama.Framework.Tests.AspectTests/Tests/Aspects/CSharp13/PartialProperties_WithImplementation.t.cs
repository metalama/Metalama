partial class Target
{
  [TheAspect]
  partial int P1 { get; set; }
  partial int P1
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
  partial int P2 { get; set; }
  [TheAspect]
  partial int P2
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
  partial int P3 { get; }
  partial int P3
  {
    get
    {
      global::System.Console.WriteLine("This is aspect code.");
      return 0;
    }
  }
}