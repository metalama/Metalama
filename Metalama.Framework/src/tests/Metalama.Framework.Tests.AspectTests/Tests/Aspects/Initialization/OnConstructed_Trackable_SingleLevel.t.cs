[TrackableAspect]
public class TargetCode : global::Metalama.Framework.RunTime.Initialization.IInitializable
{
  public string Color { get; init; } = "Red";
  public TargetCode([global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default(global::Metalama.Framework.RunTime.Initialization.InitializationContext))
  {
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Trackable_SingleLevel.ObjectTracker.Register(this, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Trackable_SingleLevel.ObjectStatus.Constructing);
    if (!context.IsHandled(global::Metalama.Framework.RunTime.Initialization.InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public virtual void Initialize(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Trackable_SingleLevel.ObjectTracker.Register(this, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Trackable_SingleLevel.ObjectStatus.Initialized);
  }
  public virtual void OnConstructed(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Trackable_SingleLevel.ObjectTracker.Register(this, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Trackable_SingleLevel.ObjectStatus.Constructed);
  }
}