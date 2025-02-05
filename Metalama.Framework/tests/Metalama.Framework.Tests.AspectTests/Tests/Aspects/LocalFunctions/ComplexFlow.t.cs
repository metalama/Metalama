internal class TargetClass
{
  [OuterAspect]
  [InnerAspect]
  private int Method(int z)
  {
    int OuterLocalFunction()
    {
      if (z == 27)
      {
        // The outer method is inlining into the middle of the method.
        int InnerLocalFunction()
        {
          if (z == 42)
          {
            // The inliner is replacing return statement, i.e. no return replacements have to be used.
            // All branches of this if statement need to return from the local function.
            if (z == 42)
            {
              // The inlined body has a return from the middle.
              return 27;
            }
            Console.WriteLine("Original");
            return 42;
          }
          global::System.Console.WriteLine("Inner");
          return (global::System.Int32)42;
        }
        _ = (global::System.Int32)InnerLocalFunction();
      }
      global::System.Console.WriteLine("Outer");
      return (global::System.Int32)27;
    }
    return (global::System.Int32)OuterLocalFunction();
  }
}