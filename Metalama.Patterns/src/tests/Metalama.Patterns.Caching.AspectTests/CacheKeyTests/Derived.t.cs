using Flashtrace.Formatters;
// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.
using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Caching.Formatters;
namespace Metalama.Patterns.Caching.AspectTests.CacheKeyTests.Derived;
public class BaseClass : IFormattable<CacheKeyFormatting>
{
  [CacheKey]
  public string Id { get; }
  public string? Description { get; }
  void IFormattable<CacheKeyFormatting>.Format(UnsafeStringBuilder stringBuilder, IFormatterRepository formatterRepository)
  {
    this.FormatCacheKey(stringBuilder, formatterRepository);
  }
  protected virtual void FormatCacheKey(UnsafeStringBuilder stringBuilder, IFormatterRepository formatterRepository)
  {
    stringBuilder.Append(GetType().FullName);
    if (formatterRepository.Role is CacheKeyFormatting)
    {
      stringBuilder.Append(" ");
      formatterRepository.Get<string>().Format(stringBuilder, Id);
    }
  }
}
public class DerivedClass : BaseClass
{
  [CacheKey]
  public int SubId { get; }
  protected override void FormatCacheKey(UnsafeStringBuilder stringBuilder, IFormatterRepository formatterRepository)
  {
    base.FormatCacheKey(stringBuilder, formatterRepository);
    if (formatterRepository.Role is CacheKeyFormatting)
    {
      stringBuilder.Append(" ");
      formatterRepository.Get<int>().Format(stringBuilder, SubId);
    }
  }
}