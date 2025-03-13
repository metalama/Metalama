internal class Yack
{
  private IGreetingService? _service1;
  [Import]
  private IGreetingService? _service
  {
    get
    {
      var value = this._service1;
      if (value == null)
      {
        value = ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug28905.IGreetingService? )global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug28905.ServiceLocator.ServiceProvider.GetService(typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug28905.IGreetingService)));
        this._service1 = value ?? throw new global::System.InvalidOperationException("Cannot get a service of type IGreetingService?.");
      }
      return (global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug28905.IGreetingService? )value;
    }
    set
    {
      this._service1 = value;
    }
  }
}