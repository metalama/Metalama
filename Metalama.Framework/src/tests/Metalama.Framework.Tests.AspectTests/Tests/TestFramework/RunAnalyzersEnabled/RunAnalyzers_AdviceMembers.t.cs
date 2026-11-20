public partial class Customer
{
  [Log]
  public void Save()
  {
    this._messageWriter?.Write("Entering Customer.Save().");
    Console.WriteLine("Saving.");
    return;
  }
  private readonly global::Metalama.Framework.Tests.AspectTests.Tests.TestFramework.RunAnalyzersEnabled.RunAnalyzers_AdviceMembers.IMessageWriter? _messageWriter;
}