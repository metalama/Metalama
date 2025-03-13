using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33813;
[CompileTime]
public interface IBaseInterface
{
  int Foo { get; init; }
}
[CompileTime]
public interface IInterface : IBaseInterface
{
  int Bar { get; init; }
}
public class Aspect : TypeAspect, IInterface
{
  public int Foo { get; init; }
  public int Bar { get; init; }
}