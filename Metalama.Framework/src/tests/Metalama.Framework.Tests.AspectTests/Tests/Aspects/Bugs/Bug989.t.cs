[TheAspect]
public class C
{
  public override global::System.Boolean Equals(global::System.Object? other)
  {
    return (global::System.Boolean)(other is global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug989.C typed && this.Equals(typed));
  }
}