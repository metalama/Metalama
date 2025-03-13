using Flashtrace.Formatters;
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.
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