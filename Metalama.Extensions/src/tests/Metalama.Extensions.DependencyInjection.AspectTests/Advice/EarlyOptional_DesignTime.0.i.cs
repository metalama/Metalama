namespace Metalama.Extensions.DependencyInjection.AspectTests.Advice.EarlyOptional_DesignTime
{
  partial class TargetClass
  {
    private IFormatProvider? _formatProvider;
    public TargetClass(IFormatProvider? formatProvider = null) : this()
    {
    }
  }
}