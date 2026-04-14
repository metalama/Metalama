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