// Warning TEST0005 on ``: `The shared index contains references to: F.`
// Warning TEST0002 on `F`: `Reference to 'F' from 'Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.SharedIndex.Requirements_InvocationAndDefaultKinds.C.M()' (Invocation), index restricted to declaration roots: False.`
// Warning TEST0002 on `F`: `Reference to 'F' from 'Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.SharedIndex.Requirements_InvocationAndDefaultKinds.C.M()' (Default), index restricted to declaration roots: False.`
// Warning TEST0002 on `F`: `Reference to 'F' from 'Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.SharedIndex.Requirements_InvocationAndDefaultKinds.D.Value' (Invocation), index restricted to declaration roots: False.`
[TheAspect]
internal class C
{
  private void M()
  {
    A.F();
    A.G();
    Func<int> f = A.F;
  }
}