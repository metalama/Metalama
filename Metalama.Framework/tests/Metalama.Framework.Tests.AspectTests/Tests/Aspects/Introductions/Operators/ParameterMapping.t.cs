[Introduction]
internal class TargetClass
{
  public static global::System.Int32 operator +(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.ParameterMapping.TargetClass y, global::System.Int32 x)
  {
    return (global::System.Int32)(y.ToString().Length + x);
  }
  public static implicit operator global::System.Int32(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.ParameterMapping.TargetClass y)
  {
    return (global::System.Int32)y.ToString().Length;
  }
  public static global::System.Int32 operator -(global::Metalama.Framework.IntegrationTests.Aspects.Introductions.Operators.ParameterMapping.TargetClass y)
  {
    return (global::System.Int32)y.ToString().Length;
  }
}