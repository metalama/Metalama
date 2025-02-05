[Introduction]
internal class TargetClass
{
  [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.CopyAttributes.FooAttribute(1)]
  [return: global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.CopyAttributes.FooAttribute(2)]
  public static global::System.Int32 operator +([global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.CopyAttributes.FooAttribute(3)] global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.CopyAttributes.TargetClass x, [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.CopyAttributes.FooAttribute(4)] global::System.Int32 y)
  {
    return (global::System.Int32)(y + 42);
  }
  [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.CopyAttributes.FooAttribute(1)]
  [return: global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.CopyAttributes.FooAttribute(2)]
  public static implicit operator global::System.Int32([global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.CopyAttributes.FooAttribute(3)] global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.CopyAttributes.TargetClass x)
  {
    return 42;
  }
  [global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.CopyAttributes.FooAttribute(1)]
  [return: global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.CopyAttributes.FooAttribute(2)]
  public static global::System.Int32 operator -([global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.CopyAttributes.FooAttribute(3)] global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.CopyAttributes.TargetClass x)
  {
    return 42;
  }
}