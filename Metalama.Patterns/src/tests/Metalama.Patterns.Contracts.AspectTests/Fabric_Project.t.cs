using System;
using Metalama.Framework.Fabrics;
namespace Metalama.Patterns.Contracts.AspectTests.Fabric_Project
{
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  internal class Fabric : ProjectFabric
  {
    public override void AmendProject(IProjectAmender amender) => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
  }
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
  public class PublicClass
  {
    public PublicClass(string nonNullableParam, string? nullableParam)
    {
      if (nonNullableParam == null !)
      {
        throw new ArgumentNullException("nonNullableParam", "The 'nonNullableParam' parameter must not be null.");
      }
    }
    private string _publicProperty = default !;
    public string PublicProperty
    {
      get
      {
        return _publicProperty;
      }
      set
      {
        if (value == null !)
        {
          throw new ArgumentNullException("value", "The 'PublicProperty' property must not be null.");
        }
        _publicProperty = value;
      }
    }
    internal string InternalProperty { get; set; }
    public string? PublicNullableProperty { get; set; }
    private string _publicField = default !;
    public string PublicField
    {
      get
      {
        return _publicField;
      }
      set
      {
        if (value == null !)
        {
          throw new ArgumentNullException("value", "The 'PublicField' property must not be null.");
        }
        _publicField = value;
      }
    }
    internal string InternalField;
    public string? PublicNullableField;
    public string this[string key]
    {
      get
      {
        if (key == null !)
        {
          throw new ArgumentNullException("key", "The 'key' parameter must not be null.");
        }
        return key;
      }
      set
      {
        if (key == null !)
        {
          throw new ArgumentNullException("key", "The 'key' parameter must not be null.");
        }
        if (value == null !)
        {
          throw new ArgumentNullException("value", "The indexer must not be null.");
        }
        _ = value;
      }
    }
    public void PublicMethod(string nonNullableParam, string? nullableParam)
    {
      if (nonNullableParam == null !)
      {
        throw new ArgumentNullException("nonNullableParam", "The 'nonNullableParam' parameter must not be null.");
      }
    }
    internal void InternalMethod(string nonNullableParam, string? nullableParam)
    {
    }
    private void PrivateMethod(string nonNullableParam, string? nullableParam)
    {
    }
    protected void ProtectedMethod(string nonNullableParam, string? nullableParam)
    {
      if (nonNullableParam == null !)
      {
        throw new ArgumentNullException("nonNullableParam", "The 'nonNullableParam' parameter must not be null.");
      }
    }
    private protected void PrivateProtectedMethod(string nonNullableParam, string? nullableParam)
    {
    }
    protected internal void ProtectedInternalMethod(string nonNullableParam, string? nullableParam)
    {
      if (nonNullableParam == null !)
      {
        throw new ArgumentNullException("nonNullableParam", "The 'nonNullableParam' parameter must not be null.");
      }
    }
  }
}