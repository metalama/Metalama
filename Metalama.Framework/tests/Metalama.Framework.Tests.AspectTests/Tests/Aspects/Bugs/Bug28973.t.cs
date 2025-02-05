internal class TargetCode
{
  private readonly IFormatProvider _formatProvider = default !;
  [Import]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
  private IFormatProvider FormatProvider
  {
    get
    {
      return (global::System.IFormatProvider)global::Metalama.Framework.Tests.AspectTests.Aspects.Bugs.Bug28973.ServiceLocator.ServiceProvider.GetService(typeof(global::System.IFormatProvider));
    }
    init
    {
      this._formatProvider = value;
    }
  }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}