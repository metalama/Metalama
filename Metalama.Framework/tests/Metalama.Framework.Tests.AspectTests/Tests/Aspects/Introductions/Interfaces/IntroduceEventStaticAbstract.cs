#if TEST_OPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
#endif

#if NET6_0_OR_GREATER
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;
using System;
using System.Linq;

#pragma warning disable CS0626

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Interfaces.IntroduceEventStaticAbstract;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var @interface = builder.IntroduceInterface( "ITest");
        @interface.IntroduceEvent( nameof(TestEvent) );

        // Implementation type.
        var implementation = builder.IntroduceClass("TestImplementation");
        implementation.ImplementInterface( @interface.Declaration);
        var implementationEvent = implementation.IntroduceEvent( nameof(TestEventImplementation), buildEvent: b => { b.Name = nameof(TestEvent); });

        // Usage type.
        var usage = builder.Advice.IntroduceClass(
            builder.Target,
            "TestUsage",
            buildType: b =>
            {
                var typeParam = b.AddTypeParameter("T");
                typeParam.AddTypeConstraint(@interface.Declaration);
            });

        // Method that uses the interface.
        builder.Advice.IntroduceMethod(
            usage.Declaration, 
            nameof(TestUsageMethod), 
            args: new 
            { 
                genericParameter = usage.Declaration.TypeParameters.Single(),
                interfaceEvent = @interface.Declaration.Events.Single(),
                implementationEvent = implementationEvent.Declaration,
            });
    }

    [Template]
    public static extern event EventHandler TestEvent;

    [Template]
    public static event EventHandler TestEventImplementation
    {
        add
        {
            Console.WriteLine("Implementation");
        }

        remove
        {
            Console.WriteLine("Implementation");
        }
    }

    [Template]
    public static void TestUsageMethod([CompileTime] ITypeParameter genericParameter, [CompileTime] IEvent interfaceEvent, [CompileTime] IEvent implementationEvent)
    {
        // Calling static members of type parameters is not currently supported (generic parameter does not "gain" members from constraints).
        ExpressionBuilder builder = new ExpressionBuilder();
        builder.AppendTypeName(genericParameter);
        builder.AppendVerbatim($".{interfaceEvent.Name}");
        builder.AppendVerbatim($" += ");
        builder.AppendExpression((EventHandler)((s, ea) => { Console.WriteLine("Handler"); }));
        meta.InsertStatement(builder.ToExpression());

        implementationEvent.Add((EventHandler)((s, ea) => { Console.WriteLine("Handler"); }));
    }
}

// <target>
[IntroductionAttribute]
public class TargetType { }
#endif