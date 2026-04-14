[TheAspect]
public class Connection
{
  public Connection([AspectGenerated] InitializationContext context = default)
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public Connection(string connectionString, [AspectGenerated] InitializationContext context = default)
  {
    _ = connectionString;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public Connection(string host, int port, [AspectGenerated] InitializationContext context = default) : this($"{host}:{port}", context.Descend(InitializationSlot.OnConstructed))
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed on Connection!");
  }
}
public class SecureConnection : Connection
{
  public SecureConnection(string connectionString, string certificate, [AspectGenerated] InitializationContext context = default) : base(connectionString, context.Descend(InitializationSlot.OnConstructed))
  {
    _ = certificate;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public SecureConnection(string host, int port, string certificate, [AspectGenerated] InitializationContext context = default) : base(host, port, context.Descend(InitializationSlot.OnConstructed))
  {
    _ = certificate;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  protected override void OnConstructed(InitializationContext context = default)
  {
    base.OnConstructed(context);
    Console.WriteLine("OnConstructed on SecureConnection!");
  }
}