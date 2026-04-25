[TestAspect]
internal partial class Target<T>
{
  private global::System.Object? [] GetTypes()
  {
    return (global::System.Object? [])new object? []
    {
      typeof(global::System.Collections.Generic.List<>),
      typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug1579_ToTypeOfExpression.Target<T>),
      typeof(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug1579_ToTypeOfExpression.Target<>)
    };
  }
}