[Aspect]
class TargetCode
{
  int P { get; set; }
  private void Format(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.ArgsScope.UnsafeStringBuilder stringBuilder, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Subtemplates.ArgsScope.IFormatterRepository formatterRepository)
  {
    formatterRepository.Get<global::System.Int32>().Format(stringBuilder, this.P);
  }
}