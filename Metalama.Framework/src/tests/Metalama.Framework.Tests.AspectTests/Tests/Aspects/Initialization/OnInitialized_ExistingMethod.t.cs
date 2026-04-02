[TheAspect]
public class TargetCode
{
  [OnInitialized]
  public virtual TargetCode OnInitialized(InitializationContext context = default)
  {
    global::System.Console.WriteLine("From aspect!");
    Console.WriteLine("Hand-authored!");
    return this;
  }
}