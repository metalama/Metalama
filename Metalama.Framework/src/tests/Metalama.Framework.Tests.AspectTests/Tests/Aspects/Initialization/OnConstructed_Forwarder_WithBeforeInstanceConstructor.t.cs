public class Document : ISecurable, IInitializable
{
  public Document([AspectGenerated] InitializationContext context)
  {
    Console.WriteLine("BeforeInstanceConstructor Document");
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public Document() : this(context: default)
  {
  }
  public virtual void Initialize(InitializationContext context = default)
  {
    Console.WriteLine("AfterObjectInitializer Document");
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("AfterLastInstanceConstructor Document");
  }
}
public class Report : ISecurable, IInitializable
{
  public Report([AspectGenerated] InitializationContext context)
  {
    Console.WriteLine("BeforeInstanceConstructor Report");
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public Report(string title, [AspectGenerated] InitializationContext context = default)
  {
    Console.WriteLine("BeforeInstanceConstructor Report");
    _ = title;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public Report() : this(context: default)
  {
  }
  public virtual void Initialize(InitializationContext context = default)
  {
    Console.WriteLine("AfterObjectInitializer Report");
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("AfterLastInstanceConstructor Report");
  }
}
public class WeightedReport : Report
{
  public WeightedReport([AspectGenerated] InitializationContext context) : base(context.Descend(InitializationSlot.OnConstructed))
  {
    Console.WriteLine("BeforeInstanceConstructor WeightedReport");
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public WeightedReport() : this(context: default)
  {
  }
  public override void Initialize(InitializationContext context = default)
  {
    base.Initialize(context);
    Console.WriteLine("AfterObjectInitializer WeightedReport");
  }
  protected override void OnConstructed(InitializationContext context = default)
  {
    base.OnConstructed(context);
    Console.WriteLine("AfterLastInstanceConstructor WeightedReport");
  }
}