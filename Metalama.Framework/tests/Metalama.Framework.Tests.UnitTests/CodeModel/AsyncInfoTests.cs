// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Testing.UnitTesting;
#if NET5_0_OR_GREATER
using System.Collections.Generic;
#endif
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

#pragma warning disable VSTHRD200

public sealed class AsyncInfoTests : UnitTestClass
{
    [Fact]
    public void NonAsync()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{
    int Method() { return 42; }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var asyncInfo = compilation.Types.Single().Methods.Single().GetAsyncInfo();

        Assert.False( asyncInfo.IsAsync );
        Assert.False( asyncInfo.IsAwaitable );
        Assert.False( asyncInfo.IsAwaitableOrVoid );
        Assert.False( asyncInfo.HasMethodBuilder );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(int) ), asyncInfo.ResultType );
    }

    [Fact]
    public void VoidNonAsync()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
class C
{
    void Method() {}
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var asyncInfo = compilation.Types.Single().Methods.Single().GetAsyncInfo();

        Assert.False( asyncInfo.IsAsync );
        Assert.False( asyncInfo.IsAwaitable );
        Assert.True( asyncInfo.IsAwaitableOrVoid );
        Assert.False( asyncInfo.HasMethodBuilder );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(void) ), asyncInfo.ResultType );
    }

    [Fact]
    public void TaskNonAsync()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Threading.Tasks;
class C
{
    Task Method() { return Task.CompletedTask; }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var asyncInfo = compilation.Types.Single().Methods.Single().GetAsyncInfo();

        Assert.False( asyncInfo.IsAsync );
        Assert.True( asyncInfo.IsAwaitable );
        Assert.True( asyncInfo.IsAwaitableOrVoid );
        Assert.True( asyncInfo.HasMethodBuilder );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(void) ), asyncInfo.ResultType );
    }

    [Fact]
    public void TaskTNonAsync()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Threading.Tasks;
class C
{
    Task<int> Method() { return Task<int>.FromResult(42); }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var asyncInfo = compilation.Types.Single().Methods.Single().GetAsyncInfo();

        Assert.False( asyncInfo.IsAsync );
        Assert.True( asyncInfo.IsAwaitable );
        Assert.True( asyncInfo.IsAwaitableOrVoid );
        Assert.True( asyncInfo.HasMethodBuilder );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(int) ), asyncInfo.ResultType );
    }

    [Fact]
    public void TaskAsync()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Threading.Tasks;
class C
{
    async Task Method() { }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var asyncInfo = compilation.Types.Single().Methods.Single().GetAsyncInfo();

        Assert.True( asyncInfo.IsAsync );
        Assert.True( asyncInfo.IsAwaitable );
        Assert.True( asyncInfo.IsAwaitableOrVoid );
        Assert.True( asyncInfo.HasMethodBuilder );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(void) ), asyncInfo.ResultType );
    }

    [Fact]
    public void TaskTAsync()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Threading.Tasks;
class C
{
    async Task<int> Method() { return 42; }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var asyncInfo = compilation.Types.Single().Methods.Single().GetAsyncInfo();

        Assert.True( asyncInfo.IsAsync );
        Assert.True( asyncInfo.IsAwaitable );
        Assert.True( asyncInfo.IsAwaitableOrVoid );
        Assert.True( asyncInfo.HasMethodBuilder );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(int) ), asyncInfo.ResultType );
    }

    [Fact]
    public void VoidAsync()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Threading.Tasks;
class C
{
    async void Method() { }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var asyncInfo = compilation.Types.Single().Methods.Single().GetAsyncInfo();

        Assert.True( asyncInfo.IsAsync );
        Assert.False( asyncInfo.IsAwaitable );
        Assert.True( asyncInfo.IsAwaitableOrVoid );
        Assert.False( asyncInfo.HasMethodBuilder );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(void) ), asyncInfo.ResultType );
    }

#if NET5_0_OR_GREATER
    [Fact]
    public void ValueTaskNonAsync()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Threading.Tasks;
class C
{
    ValueTask Method() { return ValueTask.CompletedTask; }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var asyncInfo = compilation.Types.Single().Methods.Single().GetAsyncInfo();

        Assert.False( asyncInfo.IsAsync );
        Assert.True( asyncInfo.IsAwaitable );
        Assert.True( asyncInfo.IsAwaitableOrVoid );
        Assert.True( asyncInfo.HasMethodBuilder );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(void) ), asyncInfo.ResultType );
    }

    [Fact]
    public void ValueTaskTNonAsync()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Threading.Tasks;
class C
{
    ValueTask<int> Method() { return ValueTask.FromResult<int>(42); }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var asyncInfo = compilation.Types.Single().Methods.Single().GetAsyncInfo();

        Assert.False( asyncInfo.IsAsync );
        Assert.True( asyncInfo.IsAwaitable );
        Assert.True( asyncInfo.IsAwaitableOrVoid );
        Assert.True( asyncInfo.HasMethodBuilder );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(int) ), asyncInfo.ResultType );
    }

    [Fact]
    public void ValueTaskAsync()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Threading.Tasks;
class C
{
    async ValueTask Method() { }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var asyncInfo = compilation.Types.Single().Methods.Single().GetAsyncInfo();

        Assert.True( asyncInfo.IsAsync );
        Assert.True( asyncInfo.IsAwaitable );
        Assert.True( asyncInfo.IsAwaitableOrVoid );
        Assert.True( asyncInfo.HasMethodBuilder );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(void) ), asyncInfo.ResultType );
    }

    [Fact]
    public void ValueTaskTAsync()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Threading.Tasks;
class C
{
    async ValueTask<int> Method() { return 42; }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var asyncInfo = compilation.Types.Single().Methods.Single().GetAsyncInfo();

        Assert.True( asyncInfo.IsAsync );
        Assert.True( asyncInfo.IsAwaitable );
        Assert.True( asyncInfo.IsAwaitableOrVoid );
        Assert.True( asyncInfo.HasMethodBuilder );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(int) ), asyncInfo.ResultType );
    }
#endif

    [Fact]
    public void CustomAwaitable()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
class C
{
    MyAwaitable Method() => new MyAwaitable();

    public struct MyAwaitable
    {
        public MyAwaiter GetAwaiter() => new MyAwaiter();
    }

    public struct MyAwaiter : INotifyCompletion
    {
        public bool IsCompleted => true;
        public int GetResult() => 42;
        public void OnCompleted(Action action) {}
    }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var asyncInfo = compilation.Types.Single().Methods.Single().GetAsyncInfo();

        Assert.False( asyncInfo.IsAsync );
        Assert.True( asyncInfo.IsAwaitable );
        Assert.True( asyncInfo.IsAwaitableOrVoid );
        Assert.False( asyncInfo.HasMethodBuilder );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(int) ), asyncInfo.ResultType );
    }

#if NET5_0_OR_GREATER
    [Fact]
    public void CustomTaskLikeAsync()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
class C
{
    async MyTask Method() {}

    [AsyncMethodBuilder(typeof(MyTaskBuilder))]
    public struct MyTask
    {
        public MyAwaiter GetAwaiter() => new MyAwaiter();
    }

    public struct MyAwaiter : INotifyCompletion
    {
        public bool IsCompleted => true;
        public int GetResult() => 42;
        public void OnCompleted(Action action) {}
    }

    class MyTaskBuilder
    {
        public static MyTaskBuilder Create() => null;
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        public void SetResult() { }
        public void SetException(Exception exception) { }
        public MyTask Task => default(MyTask);
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var asyncInfo = compilation.Types.Single().Methods.Single().GetAsyncInfo();

        Assert.True( asyncInfo.IsAsync );
        Assert.True( asyncInfo.IsAwaitable );
        Assert.True( asyncInfo.IsAwaitableOrVoid );
        Assert.True( asyncInfo.HasMethodBuilder );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(int) ), asyncInfo.ResultType );
    }

    [Fact]
    public void CustomTaskLikeNonAsync()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
class C
{
    MyTask Method() => new MyTask();

    [AsyncMethodBuilder(typeof(MyTaskBuilder))]
    public struct MyTask
    {
        public MyAwaiter GetAwaiter() => new MyAwaiter();
    }

    public struct MyAwaiter : INotifyCompletion
    {
        public bool IsCompleted => true;
        public int GetResult() => 42;
        public void OnCompleted(Action action) {}
    }

    class MyTaskBuilder
    {
        public static MyTaskBuilder Create() => null;
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine { }
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        public void SetResult() { }
        public void SetException(Exception exception) { }
        public MyTask Task => default(MyTask);
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine { }
    }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var asyncInfo = compilation.Types.Single().Methods.Single().GetAsyncInfo();

        Assert.False( asyncInfo.IsAsync );
        Assert.True( asyncInfo.IsAwaitable );
        Assert.True( asyncInfo.IsAwaitableOrVoid );
        Assert.True( asyncInfo.HasMethodBuilder );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(int) ), asyncInfo.ResultType );
    }

    [Fact]
    public void AsyncEnumerableYield()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections.Generic;
class C
{
    async IAsyncEnumerable<int> Enumerable1() { yield return 1; }
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var asyncInfo = compilation.Types.Single().Methods.Single().GetAsyncInfo();

        Assert.True( asyncInfo.IsAsync );
        Assert.False( asyncInfo.IsAwaitable );
        Assert.False( asyncInfo.IsAwaitableOrVoid );
        Assert.False( asyncInfo.HasMethodBuilder );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(IAsyncEnumerable<int>) ), asyncInfo.ResultType, compilation.Comparers.Default );
    }

    [Fact]
    public void AsyncEnumerableNoYield()
    {
        using var testContext = this.CreateTestContext();

        const string code = @"
using System.Collections.Generic;
using System.Threading;
class C
{
    IAsyncEnumerable<int> Enumerable1() => null;
}
";

        var compilation = testContext.CreateCompilationModel( code );
        var asyncInfo = compilation.Types.Single().Methods.Single().GetAsyncInfo();

        Assert.False( asyncInfo.IsAsync );
        Assert.False( asyncInfo.IsAwaitable );
        Assert.False( asyncInfo.IsAwaitableOrVoid );
        Assert.False( asyncInfo.HasMethodBuilder );
        Assert.Equal( compilation.Factory.GetTypeByReflectionType( typeof(IAsyncEnumerable<int>) ), asyncInfo.ResultType, compilation.Comparers.Default );
    }
#endif
}