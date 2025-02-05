#if TEST_OPTIONS
// @Include(_Common.cs)
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Options.BaseType;

[MyOptions( "Attribute.C" )]
[ShowOptionsAspect]
public class C { }

[ShowOptionsAspect]
public class D : C { }