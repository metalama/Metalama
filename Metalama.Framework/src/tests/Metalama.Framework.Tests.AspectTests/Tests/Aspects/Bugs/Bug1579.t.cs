[RegisterMessageHandler]
public partial class Handler<TMessage>
  where TMessage : IMessage
{
  static Handler()
  {
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug1579.MessageRouter.Register(typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug1579.Handler<TMessage>), typeof(TMessage));
  }
}