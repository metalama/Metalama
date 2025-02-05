namespace Metalama.Extensions.DependencyInjection.AspectTests.Aspect.LazyOptional_DesignTime
{
  partial class TargetClass
  {
    private Func<IFormatProvider> _formatProviderFunc;
    private IFormatProvider? _formatProviderCache;
    public TargetClass(Func<IFormatProvider>? formatProvider = null) : this()
    {
    }
    public TargetClass(int x, IFormatProvider existingParameter, Func<IFormatProvider>? formatProvider = null) : this(x, existingParameter)
    {
    }
  }
}