using System;
namespace Metalama.Framework.Tests.AspectTests.Aspects.Misc.SpecialDeclaratons
{
  internal class Aspect : Attribute
  {
  }
  internal class C
  {
    public string this[string key]
    {
      get
      {
        return string.Empty;
      }
      set
      {
      }
    }
    public static C operator +(C a, C b) => new();
    public static explicit operator int (C c) => 0;
    ~C()
    {
    }
  }
}