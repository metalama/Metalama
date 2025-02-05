using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.TemplateReturnType;

public class VoidAttribute : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(Template) );
    }

    [Template]
    public void Template()
    {
        Console.WriteLine( "void" );
        meta.Proceed();
    }
}

public class DynamicAttribute : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(Template) );
    }

    [Template]
    public dynamic? Template()
    {
        Console.WriteLine( "dynamic" );

        return meta.Proceed();
    }
}

public class TaskAttribute : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(Template) );
    }

    [Template]
    public async Task Template()
    {
        await Task.Yield();
        Console.WriteLine( "Task" );
        await meta.ProceedAsync();
    }
}

public class TaskDynamicAttribute : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        base.BuildAspect( builder );

        builder.Override( nameof(Template) );
    }

    [Template]
    public Task<dynamic?> Template()
    {
        Console.WriteLine( "Task<dynamic>" );

        return meta.ProceedAsync();
    }
}

// <target>
internal class TargetClass
{
    [Void]
    [Dynamic]
    public void SyncVoid()
    {
        Console.WriteLine( "This is the original method." );
    }

    [Void]
    [Dynamic]
    [Task]
    [TaskDynamic]
    public async void AsyncVoid()
    {
        await Task.Yield();
        Console.WriteLine( "This is the original method." );
    }

    [Dynamic]
    public int Int()
    {
        Console.WriteLine( "This is the original method." );

        return 42;
    }

    [Dynamic]
    [Task]
    [TaskDynamic]
    public Task SyncTask()
    {
        Console.WriteLine( "This is the original method." );

        return Task.CompletedTask;
    }

    [Dynamic]
    [Task]
    [TaskDynamic]
    public async Task AsyncTask()
    {
        await Task.Yield();
        Console.WriteLine( "This is the original method." );
    }

    [Dynamic]
    [TaskDynamic]
    public Task<int> SyncTaskInt()
    {
        Console.WriteLine( "This is the original method." );

        return Task.FromResult( 42 );
    }

    [Dynamic]
    [TaskDynamic]
    public async Task<int> AsyncTaskInt()
    {
        await Task.Yield();
        Console.WriteLine( "This is the original method." );

        return 42;
    }

    [Dynamic]
    [Task]
    [TaskDynamic]
    public ValueTask SyncValueTask()
    {
        Console.WriteLine( "This is the original method." );

        return new ValueTask();
    }

    [Dynamic]
    [Task]
    [TaskDynamic]
    public async ValueTask AsyncValueTask()
    {
        await Task.Yield();
        Console.WriteLine( "This is the original method." );
    }

    [Dynamic]
    [TaskDynamic]
    public ValueTask<int> SyncValueTaskInt()
    {
        Console.WriteLine( "This is the original method." );

        return new ValueTask<int>( 42 );
    }

    [Dynamic]
    [TaskDynamic]
    public async ValueTask<int> AsyncValueTaskInt()
    {
        await Task.Yield();
        Console.WriteLine( "This is the original method." );

        return 42;
    }
}