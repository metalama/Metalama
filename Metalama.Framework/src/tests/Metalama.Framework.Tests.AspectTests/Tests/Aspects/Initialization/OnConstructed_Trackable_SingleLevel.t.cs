[TrackableAspect]
public class TargetCode : IInitializable
{
  public string Color { get; init; } = "Red";
  public TargetCode([AspectGenerated] InitializationContext context)
  {
    ObjectTracker.Register(this, ObjectStatus.Constructing);
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public TargetCode() : this(context: default)
  {
  }
  public virtual void Initialize(InitializationContext context = default)
  {
    ObjectTracker.Register(this, ObjectStatus.Initialized);
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    ObjectTracker.Register(this, ObjectStatus.Constructed);
  }
}