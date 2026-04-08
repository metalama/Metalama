using System;
// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.
#pragma warning disable SA1649, SA1402
using Metalama.Framework.Fabrics;
namespace Metalama.Patterns.Contracts.AspectTests.Fabric_Project_Required_String
{
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  internal class Fabric : ProjectFabric
  {
    public override void AmendProject(IProjectAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  public class TestClass
  {
    // [Required] on string parameter should NOT trigger LAMA5003 because [Required]
    // checks for empty/whitespace strings, which [NotNull] does not.
    public void PrintString([Required] string foo)
    {
      if (string.IsNullOrWhiteSpace(foo))
      {
        if (foo == null !)
        {
          throw new ArgumentNullException("foo", "The 'foo' parameter is required.");
        }
        else
        {
          throw new ArgumentException("The 'foo' parameter is required.", "foo");
        }
      }
    }
    private string _name = default !;
    // [Required] on string property should also be silently skipped.
    [Required]
    public string Name
    {
      get
      {
        return _name;
      }
      set
      {
        if (string.IsNullOrWhiteSpace(value))
        {
          if (value == null !)
          {
            throw new ArgumentNullException("value", "The 'Name' property is required.");
          }
          else
          {
            throw new ArgumentException("The 'Name' property is required.", "value");
          }
        }
        _name = value;
      }
    }
    // [Required] on string field.
    private string _title = default !;
    [Required]
    public string Title
    {
      get
      {
        return _title;
      }
      set
      {
        if (string.IsNullOrWhiteSpace(value))
        {
          if (value == null !)
          {
            throw new ArgumentNullException("value", "The 'Title' property is required.");
          }
          else
          {
            throw new ArgumentException("The 'Title' property is required.", "value");
          }
        }
        _title = value;
      }
    }
  }
}
