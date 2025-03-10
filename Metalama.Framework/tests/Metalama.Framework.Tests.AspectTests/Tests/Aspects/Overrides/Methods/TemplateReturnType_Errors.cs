// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading.Tasks;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.TemplateReturnType_Errors;

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
    public async Task<dynamic?> Template()
    {
        Console.WriteLine( "dynamic" );

        return await meta.ProceedAsync();
    }
}

// <target>
internal class TargetClass
{
    [Task]
    [TaskDynamic]
    public void SyncVoid()
    {
        Console.WriteLine( "This is the original method." );
    }

    public async void AsyncVoid()
    {
        await Task.Yield();
        Console.WriteLine( "This is the original method." );
    }

    [Task]
    [TaskDynamic]
    public int Int()
    {
        Console.WriteLine( "This is the original method." );

        return 42;
    }

    [Task]
    public Task<int> SyncTaskInt()
    {
        Console.WriteLine( "This is the original method." );

        return Task.FromResult( 42 );
    }

    [Task]
    public async Task<int> AsyncTaskInt()
    {
        await Task.Yield();
        Console.WriteLine( "This is the original method." );

        return 42;
    }

    [Task]
    public ValueTask<int> SyncValueTaskInt()
    {
        Console.WriteLine( "This is the original method." );

        return new ValueTask<int>( 42 );
    }

    [Task]
    public async ValueTask<int> AsyncValueTaskInt()
    {
        await Task.Yield();
        Console.WriteLine( "This is the original method." );

        return 42;
    }
}