internal class Target
{
  [Log]
  private void M()
  {
    var errorColour = global::System.ConsoleColor.Red;
    var resultColour = global::System.ConsoleColor.Green;
    global::System.Console.WriteLine($"Target.M() started.");
    try
    {
      object result = null;
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30076.LoggingHelper.Log($"{$"Target.M() succeeded."}", resultColour);
      return;
    }
    catch (global::System.Exception e)
    {
      global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30076.LoggingHelper.Log($"{$"Target.M() failed: {e.Message}"}", errorColour);
      throw;
    }
  }
}