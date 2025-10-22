// Error CS0246 on `T`: `The type or namespace name 'T' could not be found (are you missing a using directive or an assembly reference?)`
// Error CS1503 on `x`: `Argument 1: cannot convert from 'System.Collections.Generic.List<System.Collections.Generic.List<T>>' to 'System.Collections.Generic.List<System.Collections.Generic.List<T>>'`
namespace Metalama.Framework.Tests.PublicPipeline.Aspects.DesignTimeInvalidCode.TransformationTarget_MissingTypeArgument
{
  partial class TargetCode
  {
    public TargetCode(global::System.Collections.Generic.List<global::System.Collections.Generic.List<T>> x, global::System.Int32 z, global::System.Int32 z2, [global::Metalama.Framework.RunTime.AspectGeneratedAttribute] global::System.Int32 TestParameter) : this(x, z, z2)
    {
    }
  }
}