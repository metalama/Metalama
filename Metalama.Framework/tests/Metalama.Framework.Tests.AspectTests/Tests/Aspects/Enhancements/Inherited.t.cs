namespace Targets
{
  [Test]
  internal class A
  {
    private void Foo1()
    {
    // Current='A'HasAspect=True
    // BaseType='object', BelToCurProj=False, HasAspect=n/a
    // BasePred = A
    }
    private void Foo2()
    {
    // BaseType='object', BelToCurProj=False, HasAspect=n/a
    // BasePred = A
    }
  }
  internal class B : A
  {
    private void Foo1()
    {
    // Current='B'HasAspect=True
    // BaseType='A', BelToCurProj=True, HasAspect=True
    // BasePred = A
    }
    private void Foo2()
    {
    // BaseType='A', BelToCurProj=True, HasAspect=True
    // BasePred = A
    }
  }
  internal class C : B
  {
    private void Foo1()
    {
    // Current='C'HasAspect=True
    // BaseType='B', BelToCurProj=True, HasAspect=True
    // BasePred = A
    }
    private void Foo2()
    {
    // BaseType='B', BelToCurProj=True, HasAspect=True
    // BasePred = A
    }
  }
  internal class D : C
  {
    private void Foo1()
    {
    // Current='D'HasAspect=True
    // BaseType='C', BelToCurProj=True, HasAspect=True
    // BasePred = A
    }
    private void Foo2()
    {
    // BaseType='C', BelToCurProj=True, HasAspect=True
    // BasePred = A
    }
  }
}