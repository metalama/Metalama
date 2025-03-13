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
            // The inliner is replacing local declaration, i.e. return replacements need to be used.
            // All branches of this if statement need to return from the local function.
            global::System.Int32 x;
            if (z == 42)
            {
              // The inlined body has a return from the middle.
              x = 27;
              goto __aspect_return_1;
            }
            Console.WriteLine("Original");
            x = 42;
            goto __aspect_return_1;
            __aspect_return_1:
              global::System.Console.WriteLine("Inner");
            return (global::System.Int32)x;
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