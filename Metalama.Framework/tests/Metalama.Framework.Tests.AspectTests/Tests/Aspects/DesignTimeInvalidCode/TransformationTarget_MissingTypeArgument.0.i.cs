// Error CS0246 on `T`: `The type or namespace name 'T' could not be found (are you missing a using directive or an assembly reference?)`
// Error CS0516 on `this`: `Constructor 'TargetCode.TargetCode(List<List<T>>, int, int, int)' cannot call itself`
namespace Metalama.Framework.Tests.PublicPipeline.Aspects.DesignTimeInvalidCode.TransformationTarget_MissingTypeArgument
{
  partial class TargetCode
  {
    public TargetCode(global::System.Collections.Generic.List<global::System.Collections.Generic.List<T>> x, global::System.Int32 z, global::System.Int32 z2, global::System.Int32 TestParameter = 1) : this(x, z, z2)
    {
    }
  }
}