using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceIndexerAbstract;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        var @interface = builder.IntroduceInterface("ITest");
        var interfaceIndexer = @interface.IntroduceIndexer(typeof(int), nameof(TestIndexerGet), nameof(TestIndexerSet));

        // Implementation type
        var implementation = builder.IntroduceClass("TestImplementation");
        implementation.ImplementInterface(@interface.Declaration);
        var constructor = implementation.IntroduceConstructor(nameof(Constructor));
        implementation.IntroduceIndexer(typeof(int), nameof(TestIndexerImplementationGet), nameof(TestIndexerImplementationSet));

        var usage = builder.IntroduceClass("TestUsage");
        usage.IntroduceMethod(nameof(TestUsageMethod), args: new { T = @interface.Declaration, indexer = interfaceIndexer.Declaration, implementationConstructor = constructor.Declaration });
    }

    [Template]
    public void Constructor()
    {
    }

    [Template]
    public extern int TestIndexerGet();

    [Template]
    public extern void TestIndexerSet(int value);

    [Template]
    public int TestIndexerImplementationGet()
    {
        Console.WriteLine("Implementation");
        return 0;
    }

    [Template]
    public void TestIndexerImplementationSet(int value)
    {
        Console.WriteLine("Implementation");
    }

    [Template]
    public T TestUsageMethod<[CompileTime] T>(T instance, [CompileTime] IIndexer indexer, [CompileTime] IConstructor implementationConstructor)
    {
        indexer.With(instance).SetValue(indexer.With(instance).GetValue(42) + 1, 42);
        return implementationConstructor.Invoke();
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }