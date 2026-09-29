# User-facing API: samples

> Part of the [call-site interceptors design](README.md). Previous: [05c-api-templates.md](05c-api-templates.md) | Next: [06a-call-site-model.md](06a-call-site-model.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

### 5.8 Samples

Each sample shows the compile-time code, the run-time source and the expected transformed code. The formatter decides the exact layout. Unchanged members are omitted with a comment. Each sample becomes an aspect test with a verified baseline (section [12](12-test-plan.md#12-test-plan)) and a documentation sample (section [13](13-documentation-plan.md#13-documentation-plan)).

#### Sample 1. Fabric: log calls to an external library method

Compile-time code:

```csharp
using Metalama.Extensions.Interceptors;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using System;
using System.IO;

namespace Contoso.Orders;

internal sealed class LogFileAccessFabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.InterceptMethods(
            typeof(File),
            [nameof(File.ReadAllText), nameof(File.WriteAllText)],
            nameof(this.LogFileAccess),
            InterceptorPlacement.CallingType() );
    }

    [Template]
    private dynamic? LogFileAccess()
    {
        var method = meta.MethodInterception.Method;
        Console.WriteLine( $"{method.DeclaringType.Name}.{method.Name}: {meta.MethodInterception.ArgumentParameters[0].Value}" );

        return meta.Proceed();
    }
}
```

Run-time source:

```csharp
namespace Contoso.Orders;

public sealed class OrderArchive
{
    public string Load( string path ) => File.ReadAllText( path );

    public string LoadBackup( string path ) => File.ReadAllText( path + ".bak" );

    public void Save( string path, string content )
    {
        File.WriteAllText( path, content );
    }
}
```

Transformed code:

```csharp
namespace Contoso.Orders;

public sealed class OrderArchive
{
    public string Load( string path ) => ReadAllText_Interceptor( path );

    public string LoadBackup( string path ) => ReadAllText_Interceptor( path + ".bak" );

    public void Save( string path, string content )
    {
        WriteAllText_Interceptor( path, content );
    }

    private static string ReadAllText_Interceptor( string path )
    {
        Console.WriteLine( $"File.ReadAllText: {path}" );

        return File.ReadAllText( path );
    }

    private static void WriteAllText_Interceptor( string path, string? contents )
    {
        Console.WriteLine( $"File.WriteAllText: {path}" );
        File.WriteAllText( path, contents );
    }
}
```

What it shows: the query surface, a declaring type with two method names, the template shorthand, a static interceptor in the calling type, parameter names copied from the intercepted method, and two call sites of `ReadAllText` that share one method.

#### Sample 2. Namespace fabric: replace static methods for testability

Run-time source:

```csharp
using System.Diagnostics;

namespace Contoso.Billing;

public static class SystemHooks
{
    public static Func<Guid>? NewGuid { get; set; }

    public static Func<long>? GetTimestamp { get; set; }
}

public sealed record Invoice( Guid Id, decimal Amount, long Timestamp );

public sealed class InvoiceFactory
{
    public Invoice Create( decimal amount ) => new( Guid.NewGuid(), amount, Stopwatch.GetTimestamp() );
}
```

Compile-time code:

```csharp
using Metalama.Extensions.Interceptors;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using System.Diagnostics;
using System.Linq;

namespace Contoso.Billing;

internal sealed class TestabilityFabric : NamespaceFabric
{
    public override void AmendNamespace( INamespaceAmender amender )
    {
        var provider = new SystemHookProvider();

        amender.InterceptMethods( typeof(Guid), nameof(Guid.NewGuid), provider );
        amender.InterceptMethods( typeof(Stopwatch), nameof(Stopwatch.GetTimestamp), provider );
    }
}

[CompileTime]
internal sealed class SystemHookProvider : IMethodInterceptorProvider, ITemplateProvider
{
    public InterceptorResult GetInterceptor( MethodInterceptionContext context )
    {
        var hooks = TypeFactory.GetNamedType( typeof(SystemHooks) );
        var hook = hooks.Properties.OfName( context.InterceptedMethod.Name ).SingleOrDefault();

        if ( hook == null )
        {
            return InterceptorResult.Skip;
        }

        return InterceptorResult.Template(
            nameof(this.UseHook),
            InterceptorPlacement.InType( hooks ),
            args: new { hook } );
    }

    [Template]
    private dynamic? UseHook( [CompileTime] IProperty hook )
    {
        var replacement = hook.Value;

        return replacement != null ? replacement() : meta.Proceed();
    }
}
```

Transformed code:

```csharp
public static class SystemHooks
{
    public static Func<Guid>? NewGuid { get; set; }

    public static Func<long>? GetTimestamp { get; set; }

    internal static Guid NewGuid_Interceptor()
    {
        var replacement = SystemHooks.NewGuid;

        return replacement != null ? replacement() : Guid.NewGuid();
    }

    internal static long GetTimestamp_Interceptor()
    {
        var replacement = SystemHooks.GetTimestamp;

        return replacement != null ? replacement() : Stopwatch.GetTimestamp();
    }
}

public sealed class InvoiceFactory
{
    public Invoice Create( decimal amount ) => new( SystemHooks.NewGuid_Interceptor(), amount, SystemHooks.GetTimestamp_Interceptor() );
}
```

What it shows: a namespace scope, two registrations that share one provider object (the index receives the names `NewGuid` and `GetTimestamp`, section [9.5.3](09-premium-engine.md#953-registration-index-and-index-requirements)), a class-based interceptor provider that is its own template provider (rule 2 of section [5.3.10](05a-api-registration.md#5310-default-template-provider)), an explicit placement in another type, and a declaration passed as a compile-time template argument.

#### Sample 3. Type aspect through IAdviser: redirect to an existing method

Run-time source:

```csharp
namespace Contoso.Configuration;

public static class ResilientFile
{
    public static string ReadAllText( string path )
    {
        for ( var attempt = 1; ; attempt++ )
        {
            try
            {
                return File.ReadAllText( path );
            }
            catch ( IOException ) when ( attempt < 3 )
            {
                Thread.Sleep( 50 * attempt );
            }
        }
    }
}

[UseResilientFileAccess]
public sealed class ConfigurationLoader
{
    public string LoadText( string path ) => File.ReadAllText( path );

    public string[] LoadLines( string path ) => File.ReadAllLines( path );
}
```

Compile-time code:

```csharp
using Metalama.Extensions.Interceptors;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using System;
using System.IO;
using System.Linq;

namespace Contoso.Configuration;

public sealed class UseResilientFileAccessAttribute : TypeAspect
{
    private static readonly DiagnosticDefinition<IMethod> _noResilientVariant = new(
        "CONTOSO002",
        Severity.Warning,
        "There is no resilient variant of '{0}'. The call is not redirected." );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.InterceptMethods(
            typeof(File),
            [nameof(File.ReadAllText), nameof(File.ReadAllLines), nameof(File.ReadAllBytes)],
            this.RedirectToResilientFile );
    }

    private InterceptorResult RedirectToResilientFile( MethodInterceptionContext context )
    {
        var intercepted = context.InterceptedMethod;

        var replacement = TypeFactory.GetNamedType( typeof(ResilientFile) )
            .Methods
            .OfExactSignature( intercepted.Name, intercepted.Parameters.Select( p => p.Type ).ToList() );

        if ( replacement == null )
        {
            context.Diagnostics.Report( _noResilientVariant.WithArguments( intercepted ) );

            return InterceptorResult.Skip;
        }

        return InterceptorResult.ExistingMethod( replacement );
    }
}
```

`OfExactSignature( string, IReadOnlyList<IType>, ... )` exists (FW27 `Code\MethodCollectionExtensions.cs:82`).

Transformed code and diagnostics:

```csharp
public sealed class ConfigurationLoader
{
    public string LoadText( string path ) => ResilientFile.ReadAllText( path );

    public string[] LoadLines( string path ) => File.ReadAllLines( path );
}

// Warning CONTOSO002 on `ReadAllLines`: `There is no resilient variant of 'File.ReadAllLines(string)'. The call is not redirected.`
```

What it shows: the adviser surface with a delegate to a method of the aspect, a declaring type with a list of names, an existing-method result, and a skip with a diagnostic at the call site. The provider receives every overload of the three names and chooses by signature, which replaces a predicate over members (section [5.3.3](05a-api-registration.md#533-target-selection-for-members)). The call to `File.ReadAllText` inside `ResilientFile.ReadAllText` is not in the scope, because the aspect is applied to `ConfigurationLoader` only.

#### Sample 4. Method aspect: measure awaits

Compile-time code:

```csharp
using Metalama.Extensions.Interceptors;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Diagnostics;

namespace Contoso.Catalog;

public sealed class MeasureAwaitsAttribute : MethodAspect
{
    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        builder.InterceptAwaits( this.MeasureAwait );
    }

    private InterceptorResult MeasureAwait( AwaitInterceptionContext context )
    {
        if ( context.Resumption == AwaitResumption.Unknown )
        {
            // The resumption is Unknown for a custom awaitable, and for an await of task.ConfigureAwait( flag ) with a
            // variable flag. This test is the recommended first statement of an await provider.
            return InterceptorResult.Skip;
        }

        var operation = context.AwaitedMethod?.ToDisplayString( CodeDisplayFormat.MinimallyQualified )
                        ?? context.AwaitableType.ToDisplayString( CodeDisplayFormat.MinimallyQualified );

        return InterceptorResult.Template( nameof(this.Measure), InterceptorPlacement.CallingType(), args: new { operation } );
    }

    [Template]
    private dynamic? Measure( [CompileTime] string operation )
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return meta.Proceed();
        }
        finally
        {
            Console.WriteLine( $"await {operation}: {stopwatch.ElapsedMilliseconds} ms" );
        }
    }
}
```

Run-time source:

```csharp
namespace Contoso.Catalog;

public sealed class CatalogClient
{
    private readonly HttpClient _http = new();

    [MeasureAwaits]
    public async Task<int> CountProductsAsync( string url )
    {
        var json = await this._http.GetStringAsync( url ).ConfigureAwait( false );
        await Task.Delay( 10 );
        await this.FlushAsync();

        return json.Length;
    }

    private Task FlushAsync() => Task.CompletedTask;
}
```

Transformed code:

```csharp
public sealed class CatalogClient
{
    private readonly HttpClient _http = new();

    public async Task<int> CountProductsAsync( string url )
    {
        var json = await Await_Interceptor( this._http.GetStringAsync( url ).ConfigureAwait( false ) ).ConfigureAwait( false );
        await Await_Interceptor1( Task.Delay( 10 ) );
        await Await_Interceptor2( this.FlushAsync() );

        return json.Length;
    }

    private Task FlushAsync() => Task.CompletedTask;

    private static async ValueTask<string> Await_Interceptor( ConfiguredTaskAwaitable<string> awaitable )
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await awaitable;
        }
        finally
        {
            Console.WriteLine( $"await HttpClient.GetStringAsync(string?): {stopwatch.ElapsedMilliseconds} ms" );
        }
    }

    private static async ValueTask Await_Interceptor1( Task awaitable )
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await awaitable;

            return;
        }
        finally
        {
            Console.WriteLine( $"await Task.Delay(int): {stopwatch.ElapsedMilliseconds} ms" );
        }
    }

    private static async ValueTask Await_Interceptor2( Task awaitable )
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await awaitable;

            return;
        }
        finally
        {
            Console.WriteLine( $"await CatalogClient.FlushAsync(): {stopwatch.ElapsedMilliseconds} ms" );
        }
    }
}
```

What it shows: the await verb on the adviser surface, which presents every await of the scope to the provider, because an await registration has no target selection (section [5.3.4](05a-api-registration.md#534-await-registrations)); the recommended first statement of an await provider, which skips the awaits whose resumption is Unknown, including the awaits of custom awaitables; a template referenced by its name, as every await template is (RC60); a non-async template, whose `meta.Proceed()` gives the awaited value, and which the engine turns into an `async` interceptor; the `ValueTask<R>` and `ValueTask` shapes; the adaptive rewrite of section [7.5](07-await-interception.md#75-the-adaptive-rewrite-challenge-to-b8-adopted): the first await was configured with `ConfigureAwait(false)`, so the call site receives the suffix, and the other awaits captured the context, so they receive none; a skip for awaits whose resumption is Unknown; and grouping by argument value: `Await_Interceptor1` and `Await_Interceptor2` have the same signature and differ only by the value of the `operation` argument, so they are two groups.

#### Sample 5. Local-function placement that uses the caller's parameters

Compile-time code:

```csharp
using Metalama.Extensions.Interceptors;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Contoso.Polling;

public sealed class PropagateCancellationAttribute : MethodAspect
{
    public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
    {
        builder.MustSatisfy(
            m => m.Parameters.OfParameterType<CancellationToken>().Any(),
            m => $"{m} must have a CancellationToken parameter" );
    }

    public override void BuildAspect( IAspectBuilder<IMethod> builder )
    {
        // Every overload of Task.Delay matches. The provider keeps the overloads with one parameter.
        builder.InterceptMethods(
            typeof(Task),
            nameof(Task.Delay),
            context => context.InterceptedMethod.Parameters.Count == 1
                ? InterceptorResult.Template( nameof(this.DelayWithCallerToken), InterceptorPlacement.LocalFunction() )
                : InterceptorResult.Skip );
    }

    [Template]
    private dynamic? DelayWithCallerToken()
    {
        var cancellationToken = meta.MethodInterception.Origin.Parameters.OfParameterType<CancellationToken>().First();

        return Task.Delay( meta.MethodInterception.ArgumentParameters[0].Value, cancellationToken.Value );
    }
}
```

Run-time source:

```csharp
namespace Contoso.Polling;

public sealed class Poller
{
    private readonly IProbe _probe;

    public Poller( IProbe probe )
    {
        this._probe = probe;
    }

    [PropagateCancellation]
    public async Task PollAsync( Uri endpoint, CancellationToken cancellationToken )
    {
        while ( !cancellationToken.IsCancellationRequested )
        {
            await this._probe.PingAsync( endpoint );
            await Task.Delay( 1000 );
        }
    }
}
```

Transformed code:

```csharp
public sealed class Poller
{
    // Constructor unchanged.

    public async Task PollAsync( Uri endpoint, CancellationToken cancellationToken )
    {
        while ( !cancellationToken.IsCancellationRequested )
        {
            await this._probe.PingAsync( endpoint );
            await Delay_Interceptor( 1000 );
        }

        Task Delay_Interceptor( int millisecondsDelay )
        {
            return Task.Delay( millisecondsDelay, cancellationToken );
        }
    }
}
```

What it shows: R12, and a lambda provider that filters the overloads of a name by signature, because a registration selects every overload of its names (section [5.3.3](05a-api-registration.md#533-target-selection-for-members)). The lambda has the default template provider of the registration, the aspect (section [5.3.10](05a-api-registration.md#5310-default-template-provider)). The template does not call `meta.Proceed()`. It calls another overload with a captured parameter of the origin, found through `meta.MethodInterception.Origin` (FW27 `Code\Collections\ParameterListExtensions.cs:24`). The local function is appended to the root block of the body (section [10.5.4](10b-oss-linker-and-templates.md#1054-local-function-injection)).

#### Sample 6. Instance interceptor in the calling hierarchy that uses the caller's this

Run-time source:

```csharp
namespace Contoso.Messaging;

public interface IMessageBus
{
    void Publish( object message );
}

public interface IAuditLog
{
    void Record( string source, string action, object details );
}

public abstract class ServiceBase
{
    protected ServiceBase( IAuditLog audit )
    {
        this.Audit = audit;
    }

    protected IAuditLog Audit { get; }

    protected abstract string ServiceName { get; }
}

[AuditPublishedMessages]
public sealed class OrderService : ServiceBase
{
    private readonly IMessageBus _bus;

    public OrderService( IMessageBus bus, IAuditLog audit ) : base( audit )
    {
        this._bus = bus;
    }

    protected override string ServiceName => "Orders";

    public void PlaceOrder( Order order )
    {
        this._bus.Publish( new OrderPlaced( order.Id ) );
    }
}
```

Variant 6a, an aspect with an instance method in the calling type:

```csharp
using Metalama.Extensions.Interceptors;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace Contoso.Messaging;

public sealed class AuditPublishedMessagesAttribute : TypeAspect
{
    public override void BuildEligibility( IEligibilityBuilder<INamedType> builder )
        => builder.MustSatisfy( t => t.Is( typeof(ServiceBase) ), t => $"{t} must derive from ServiceBase" );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.InterceptMethods(
            typeof(IMessageBus),
            nameof(IMessageBus.Publish),
            nameof(this.AuditPublish),
            InterceptorPlacement.CallingType(),
            configure: b => b.IsStatic = false );
    }

    [Template]
    private void AuditPublish( object message )
    {
        meta.This.Audit.Record( meta.This.ServiceName, "Publish", message );
        meta.Proceed();
    }
}
```

Transformed code for 6a:

```csharp
public sealed class OrderService : ServiceBase
{
    // Field, constructor and ServiceName unchanged.

    public void PlaceOrder( Order order )
    {
        this.Publish_Interceptor( this._bus, new OrderPlaced( order.Id ) );
    }

    private void Publish_Interceptor( [NotNull] IMessageBus receiver, object message )
    {
        this.Audit.Record( this.ServiceName, "Publish", message );
        receiver.Publish( message );
    }
}
```

The run-time template parameter `message` is bound by name to `IMessageBus.Publish( object message )`. The receiver `this._bus` has the type `IMessageBus`, which is not in the hierarchy of `OrderService`, so rule R2 does not apply. By default, the interceptor would be static (rule R1). The `configure` function sets `IsStatic` to `false`, which selects rule R3: the interceptor is an instance member of the calling type, `meta.This` is the caller, and the receiver is the first parameter (section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping)). The receiver parameter has the type of the target's containing type, `IMessageBus` (section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping)). It carries `[NotNull]` (rules table row 2), because the target framework of the sample declares `NotNullAttribute` (section [6.4.5](06b-signatures-and-validation.md#645-attributes)). The lambda passed to `configure` captures nothing, so it satisfies `[Durable]`.

Variant 6b, a fabric with one shared instance method in the base type:

```csharp
internal sealed class AuditFabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender
            .SelectTypesDerivedFrom( typeof(ServiceBase) )
            .InterceptMethods(
                typeof(IMessageBus),
                nameof(IMessageBus.Publish),
                nameof(this.AuditPublish),
                InterceptorPlacement.InType( typeof(ServiceBase) ),
                configure: b => b.IsStatic = false );
    }

    [Template]
    private void AuditPublish( object message )
    {
        meta.This.Audit.Record( meta.This.ServiceName, "Publish", message );
        meta.Proceed();
    }
}
```

Transformed code for 6b, with a second derived class `InvoiceService` that also publishes:

```csharp
public abstract class ServiceBase
{
    // Constructor and properties unchanged.

    private protected void Publish_Interceptor( [NotNull] IMessageBus receiver, object message )
    {
        this.Audit.Record( this.ServiceName, "Publish", message );
        receiver.Publish( message );
    }
}

public sealed class OrderService : ServiceBase
{
    public void PlaceOrder( Order order )
    {
        this.Publish_Interceptor( this._bus, new OrderPlaced( order.Id ) );
    }
}

public sealed class InvoiceService : ServiceBase
{
    public void Issue( Invoice invoice )
    {
        this.Publish_Interceptor( this._bus, new InvoiceIssued( invoice.Id ) );
    }
}
```

What it shows: the overload that takes a declaring type and a method name, R13 for the calling type and for a base type through rule R3 of section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping), the `configure` function that selects an instance method, `meta.This` as the caller's `this`, and the grouping rule of section [5.6.7](05b-api-providers-contexts-results.md#567-grouping-identity-as-seen-by-users): one fabric owner gives one shared method, while variant 6a produces one method per aspect instance. Section [6.4.3](06b-signatures-and-validation.md#643-worked-examples-of-the-receiver-mapping) shows the other receiver-mapping rules on the same types.

#### Sample 7. Skip plus diagnostic

Run-time source:

```csharp
namespace Contoso.Workers;

public static class SleepMonitor
{
    public static void Sleep( int millisecondsTimeout )
    {
        Metrics.RecordBlockingSleep( millisecondsTimeout );
        Thread.Sleep( millisecondsTimeout );
    }
}

public sealed class Worker
{
    public void Process()
    {
        Thread.Sleep( 100 );
    }

    public async Task ProcessAsync()
    {
        Thread.Sleep( 100 );
        await Task.Yield();
    }
}
```

Compile-time code:

```csharp
using Metalama.Extensions.Interceptors;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Fabrics;
using System;
using System.Linq;
using System.Threading;

namespace Contoso.Workers;

internal sealed class BlockingCallsFabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
        => amender.InterceptMethods( typeof(Thread), nameof(Thread.Sleep), new BlockingSleepProvider() );
}

[CompileTime]
internal sealed class BlockingSleepProvider : IMethodInterceptorProvider
{
    private static readonly DiagnosticDefinition<IDeclaration> _sleepInAsyncCode = new(
        "CONTOSO001",
        Severity.Error,
        "Thread.Sleep must not be called in the asynchronous code of '{0}'. Use 'await Task.Delay' instead." );

    private static readonly DiagnosticDefinition<IDeclaration> _sleepInTests = new(
        "CONTOSO002",
        Severity.Hidden,
        "Thread.Sleep is not monitored in '{0}' because tests may block." );

    public InterceptorResult GetInterceptor( MethodInterceptionContext context )
    {
        if ( context.CallingType.Is( typeof(SleepMonitor) ) )
        {
            return InterceptorResult.Skip;
        }

        if ( context.IsInAsyncFunction )
        {
            context.Diagnostics.Report( _sleepInAsyncCode.WithArguments( context.Origin ) );

            return InterceptorResult.Skip;
        }

        if ( context.CallingNamespace.FullName.EndsWith( ".Tests", StringComparison.Ordinal ) )
        {
            context.Diagnostics.Report( _sleepInTests.WithArguments( context.Origin ) );

            return InterceptorResult.Skip;
        }

        var replacement = TypeFactory.GetNamedType( typeof(SleepMonitor) )
            .Methods
            .OfExactSignature( nameof(SleepMonitor.Sleep), context.InterceptedMethod.Parameters.Select( p => p.Type ).ToList() );

        return replacement != null ? InterceptorResult.ExistingMethod( replacement ) : InterceptorResult.Skip;
    }
}
```

Transformed code and diagnostics:

```csharp
public sealed class Worker
{
    public void Process()
    {
        SleepMonitor.Sleep( 100 );
    }

    public async Task ProcessAsync()
    {
        Thread.Sleep( 100 );
        await Task.Yield();
    }
}

// Error CONTOSO001 on `Sleep`: `Thread.Sleep must not be called in the asynchronous code of 'Worker.ProcessAsync()'. Use 'await Task.Delay' instead.`
```

What it shows: R14. The same provider reports an error and skips in some contexts, skips silently in others, skips with a hidden diagnostic that explains the skip in test namespaces, and redirects in the remaining ones. A provider explains a skip with a diagnostic of its own, not with a special result (RC42). Without the first test, the call inside `SleepMonitor.Sleep` would be redirected to `SleepMonitor.Sleep` itself, and the engine would report LAMA1018. The diagnostics are reported by the build. They also appear in the IDE when decision PO26 is accepted, because the analyzer then evaluates providers (section [9.7.4](09-premium-engine.md#974-phase-b-analyzer)).

#### Sample 8. Cross-project: a library intercepts calls to its own API in consumers

Library `Contoso.Storage`, run-time code:

```csharp
namespace Contoso.Storage;

public sealed class BlobClient
{
    public void Upload( string name, byte[] content ) { /* ... */ }
}

public static class StorageTelemetry
{
    public static void RecordUpload( string name, int length ) { /* ... */ }
}
```

Library `Contoso.Storage`, compile-time code:

```csharp
using Metalama.Extensions.Interceptors;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

namespace Contoso.Storage;

public sealed class StorageFabric : TransitiveProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.InterceptMethods(
            typeof(BlobClient),
            nameof(BlobClient.Upload),
            nameof(this.TrackUpload),
            InterceptorPlacement.CallingType() );
    }

    [Template]
    private void TrackUpload( string name, byte[] content )
    {
        StorageTelemetry.RecordUpload( name, content.Length );
        meta.Proceed();
    }
}
```

Consumer project, run-time source:

```csharp
namespace Contoso.Reports;

public sealed class ReportPublisher
{
    private readonly BlobClient _client = new();

    public void Publish( Report report ) => this._client.Upload( report.Name, report.Content );
}
```

Consumer project, transformed code:

```csharp
public sealed class ReportPublisher
{
    private readonly BlobClient _client = new();

    public void Publish( Report report ) => Upload_Interceptor( this._client, report.Name, report.Content );

    private static void Upload_Interceptor( [NotNull] BlobClient receiver, string name, byte[] content )
    {
        StorageTelemetry.RecordUpload( name, content.Length );
        receiver.Upload( name, content );
    }
}
```

What it shows: baseline B10. A `TransitiveProjectFabric` runs in each referencing project and not in its own project (FW27 `Fabrics\TransitiveProjectFabric.cs:9`). The scope is the consumer compilation, the target is an external method, and the placement is a consumer type named through `CallingType()`. No serialization is involved.

#### Sample 9. Enforce ConfigureAwait(false) semantics in a namespace (mode Awaitable)

This is the equivalent of Fody.ConfigureAwait, which rewrites every await of a scope and leaves explicit `ConfigureAwait` calls unchanged.

```csharp
using Metalama.Extensions.Interceptors;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;

namespace Shop.Services;

internal sealed class Fabric : NamespaceFabric
{
    public override void AmendNamespace( INamespaceAmender amender )
        => amender.InterceptAwaits( new ConfigureAwaitFalseProvider() );
}

[CompileTime]
internal sealed class ConfigureAwaitFalseProvider : IAwaitInterceptorProvider, ITemplateProvider
{
    public InterceptorResult GetInterceptor( AwaitInterceptionContext context )
    {
        if ( context.Resumption == AwaitResumption.Unknown )
        {
            return InterceptorResult.Skip;
        }

        // Only the awaits of tasks and value tasks that are not configured. Task.Yield() and explicit ConfigureAwait
        // calls are left unchanged.
        if ( context.AwaitableKind is not (AwaitableKind.Task or AwaitableKind.ValueTask) )
        {
            return InterceptorResult.Skip;
        }

        return InterceptorResult.Template( nameof(this.NoContext), InterceptorPlacement.CallingType() )
            .WithAwaitRewriteOptions(
                new AwaitRewriteOptions { Mode = AwaitInterceptionMode.Awaitable },
                context.ConfiguredAwaitableType );
    }

    [Template]
    private dynamic? NoContext()
    {
        return meta.Proceed().ConfigureAwait( false );
    }
}
```

Source:

```csharp
namespace Shop.Services;

internal sealed class OrderService( IOrderRepository repository, IAuditLog audit )
{
    public async Task<Order> LoadAsync( int id )
    {
        var order = await repository.GetAsync( id );
        await audit.WriteAsync( order );
        await Task.Yield();
        order.Lines = await repository.GetLinesAsync( id ).ConfigureAwait( true );
        return order;
    }
}
```

Transformed code:

```csharp
internal sealed class OrderService( IOrderRepository repository, IAuditLog audit )
{
    public async Task<Order> LoadAsync( int id )
    {
        var order = await Await_Interceptor( repository.GetAsync( id ) );
        await Await_Interceptor1( audit.WriteAsync( order ) );
        await Task.Yield();
        order.Lines = await repository.GetLinesAsync( id ).ConfigureAwait( true );
        return order;
    }

    private static ConfiguredTaskAwaitable<Order> Await_Interceptor( Task<Order> awaitable )
    {
        return awaitable.ConfigureAwait( false );
    }

    private static ConfiguredTaskAwaitable Await_Interceptor1( Task awaitable )
    {
        return awaitable.ConfigureAwait( false );
    }
}
```

The interceptor return type is not in the task family, so rule 1 of section [7.5](07-await-interception.md#75-the-adaptive-rewrite-challenge-to-b8-adopted) applies: the call site awaits what the interceptor returns, and `LoadAsync` resumes without the captured context, which is the purpose of the sample. The registration presents every await of the namespace. The provider skips `Task.Yield()` and the explicitly configured await, because it tests `AwaitableKind`; an earlier version of this sample used a kind filter of the registration, which RC58 removed. The template `NoContext` is not `async`, which mode `Awaitable` requires, so `meta.Proceed()` returns the original awaitable (section [7.9.1](07-await-interception.md#791-accepted-template-shapes)).

#### Sample 10. Tracing around suspensions (PostSharp OnYield and OnResume)

Variant 10a, approximate, in mode `Await`:

```csharp
[CompileTime]
internal sealed class TraceTemplates : ITemplateProvider
{
    [Template]
    public dynamic? Trace( [CompileTime] string member )
    {
        var isCompleted = meta.AwaitInterception.IsCompleted;
        var suspended = isCompleted != null ? !isCompleted.Value : true;

        if ( suspended )
        {
            AwaitTrace.OnYield( member );
        }

        try
        {
            var result = meta.Proceed();

            if ( suspended )
            {
                AwaitTrace.OnResume( member );
            }

            return result;
        }
        catch ( Exception e )
        {
            AwaitTrace.OnException( member, e );

            throw;
        }
    }
}
```

Expected interceptor for `A = Task<Order>`:

```csharp
private static async ValueTask<Order> Await_Interceptor( Task<Order> awaitable )
{
    var suspended = !awaitable.IsCompleted;

    if ( suspended )
    {
        AwaitTrace.OnYield( "LoadAsync" );
    }

    try
    {
        var result = await awaitable;

        if ( suspended )
        {
            AwaitTrace.OnResume( "LoadAsync" );
        }

        return result;
    }
    catch ( Exception e )
    {
        AwaitTrace.OnException( "LoadAsync", e );

        throw;
    }
}
```

`OnResume` runs on the context where the calling method resumes immediately afterwards (section [7.5](07-await-interception.md#75-the-adaptive-rewrite-challenge-to-b8-adopted)). This is an approximation: the operation can complete between the `IsCompleted` test and the await, and `OnYield` is then reported without a suspension. The template is not `async`, so `meta.Proceed()` gives the awaited value, and the engine generates an `async` interceptor with the default task type (section [7.9.1](07-await-interception.md#791-accepted-template-shapes)). The provider that returns this template skips the awaits whose resumption is Unknown, as in sample 4.

Variant 10a', the same tracing with an `async` template. The template awaits the original awaitable itself, and its declared return type selects the task type of the interceptor (RC60):

```csharp
[Template]
public async Task<dynamic?> TraceAsync( [CompileTime] string member )
{
    var result = await meta.ProceedAsync();

    AwaitTrace.OnResume( member );

    return result;
}
```

Expected interceptor for `A = Task<Order>`:

```csharp
private static async Task<Order> Await_Interceptor( Task<Order> awaitable )
{
    var result = await awaitable;

    AwaitTrace.OnResume( "LoadAsync" );

    return result;
}
```

`meta.ProceedAsync()` returns the awaitable `A` in an `async` template, and `meta.Proceed()` does the same (section [7.9.2](07-await-interception.md#792-meaning-of-metaproceed-and-metaproceedasync)). The template declares `Task<dynamic?>`, so the interceptor returns `Task<Order>` instead of the default `ValueTask<Order>`. A template declared `async AnyAwaitable<dynamic?>` would let the engine choose the default task type, `ValueTask<Order>` (section [7.9.1](07-await-interception.md#791-accepted-template-shapes), RC67). The interceptor type equals `A`, so the call site is `await Await_Interceptor( repository.GetAsync( id ) )` without a suffix (section [7.5](07-await-interception.md#75-the-adaptive-rewrite-challenge-to-b8-adopted), rule 1).

Variant 10b, exact, in mode `Awaitable` with an existing run-time wrapper:

```csharp
public static class TracedAwait
{
    public static TracedAwaitable<T> Await<T>( Task<T> task, [CallerMemberName] string member = "" ) => new( task, member );
}

public readonly struct TracedAwaitable<T>( Task<T> task, string member )
{
    public TracedAwaiter<T> GetAwaiter() => new( task.GetAwaiter(), member );
}

public readonly struct TracedAwaiter<T>( TaskAwaiter<T> inner, string member ) : ICriticalNotifyCompletion
{
    public bool IsCompleted => inner.IsCompleted;

    public void OnCompleted( Action continuation )
    {
        AwaitTrace.OnYield( member );
        var name = member;
        inner.OnCompleted( () => { AwaitTrace.OnResume( name ); continuation(); } );
    }

    public void UnsafeOnCompleted( Action continuation )
    {
        AwaitTrace.OnYield( member );
        var name = member;
        inner.UnsafeOnCompleted( () => { AwaitTrace.OnResume( name ); continuation(); } );
    }

    public T GetResult() => inner.GetResult();
}
```

The provider returns `InterceptorResult.ExistingMethod( tracedAwaitType.Methods.OfName( "Await" ).Single() )` for the awaits whose `AwaitableKind` is `Task` and whose `AwaitableType` is a construction of `Task<TResult>`, and it skips the other awaits, because the registration presents every await of the scope (section [5.3.4](05a-api-registration.md#534-await-registrations)). The expected call site is `var order = await TracedAwait.Await( repository.GetAsync( id ), member: "LoadAsync" );`, where the member name is materialized from the source await (section [6.7](06b-signatures-and-validation.md#67-caller-information-materialization)). `TracedAwaitable<T>` is not in the task family, so no adaptation applies, and the calling method resumes exactly where `TaskAwaiter<T>` schedules it. The cost is one closure and one delegate per suspension.

#### Sample 11. Method references: the lambda form and the method-group form

Compile-time code:

```csharp
using Metalama.Extensions.Interceptors;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using System;

namespace Contoso.Pricing;

internal sealed class TraceParsingFabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.InterceptMethods(
            typeof(PriceParser),
            nameof(PriceParser.Parse),
            nameof(this.TraceParse),
            InterceptorPlacement.GeneratedStaticClass() );
    }

    [Template]
    private dynamic? TraceParse()
    {
        Console.WriteLine( $"Parsing: {meta.MethodInterception.ArgumentParameters[0].Value}" );

        return meta.Proceed();
    }
}
```

Run-time source:

```csharp
namespace Contoso.Pricing;

public static class PriceParser
{
    public static decimal Parse( string text ) => decimal.Parse( text, CultureInfo.InvariantCulture );
}

public sealed class Catalog
{
    public List<decimal> LoadWithLambda( List<string> lines ) => lines.Select( line => PriceParser.Parse( line ) ).ToList();

    public List<decimal> LoadWithMethodGroup( List<string> lines ) => lines.Select( PriceParser.Parse ).ToList();
}
```

Transformed code:

```csharp
namespace Contoso.Pricing;

public sealed class Catalog
{
    public List<decimal> LoadWithLambda( List<string> lines ) => lines.Select( line => global::MetalamaInterceptors.Parse_Interceptor( line ) ).ToList();

    public List<decimal> LoadWithMethodGroup( List<string> lines ) => lines.Select( global::MetalamaInterceptors.Parse_Interceptor ).ToList();
}

// New syntax tree.
internal static class MetalamaInterceptors
{
    internal static decimal Parse_Interceptor( string text )
    {
        Console.WriteLine( $"Parsing: {text}" );

        return global::Contoso.Pricing.PriceParser.Parse( text );
    }
}
```

What it shows: a registration receives call sites and method-reference sites with no option (section [5.3.11](05a-api-registration.md#5311-kinds-of-method-use)). The call site inside the lambda and the method group share one interceptor, because their signatures are identical and method-group convertible (section [8.2](08-deduplication-and-naming.md#82-the-key)). The method group of a static target becomes a method group of a static interceptor (section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites)). The two methods of `Catalog` print the same lines, which the runtime test `MethodReference_LambdaAndMethodGroup_SameOutput` checks (section [12.8](12-test-plan.md#128-premium-runtime-execution-tests-runtime)).

#### Sample 12. Fabric: reads and writes of a property of an external type, with a compound site

Run-time source. `Order` is declared in a referenced assembly:

```csharp
// Referenced assembly.
public class Order
{
    public int Quantity { get; set; }
}

// Current project.
namespace Contoso.Shop;

public sealed class Cart
{
    private readonly List<Order> _orders = new();

    public void Add( int index, int count )
    {
        this._orders[index].Quantity += count;
    }

    public int Next( Order order ) => order.Quantity++;

    public void Reset( Order order ) => order.Quantity = 0;
}
```

Compile-time code:

```csharp
using Metalama.Extensions.Interceptors;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using System;

namespace Contoso.Shop;

internal sealed class TrackQuantityFabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender.InterceptAccessors( typeof(Order), nameof(Order.Quantity), MethodKind.PropertyGet, nameof(this.LogRead), InterceptorPlacement.GeneratedStaticClass() );
        amender.InterceptAccessors( typeof(Order), nameof(Order.Quantity), MethodKind.PropertySet, nameof(this.LogWrite), InterceptorPlacement.GeneratedStaticClass() );
    }

    [Template]
    private dynamic? LogRead()
    {
        var value = meta.Proceed();
        Console.WriteLine( $"Read {meta.MethodInterception.Destination.Name}: {value}" );

        return value;
    }

    [Template]
    private dynamic? LogWrite( dynamic? value )
    {
        Console.WriteLine( $"Write {meta.MethodInterception.Destination.Name}: {value}" );

        return meta.Proceed();
    }
}
```

Transformed code:

```csharp
public sealed class Cart
{
    private readonly List<Order> _orders = new();

    public void Add( int index, int count )
    {
        {
            var receiver = this._orders[index];
            global::MetalamaInterceptors.Quantity_set_Interceptor( receiver, global::MetalamaInterceptors.Quantity_get_Interceptor( receiver ) + count );
        }
    }

    public int Next( Order order )
        => global::MetalamaInterceptors.Quantity_get_Interceptor( order ) is var value
           && global::MetalamaInterceptors.Quantity_set_Interceptor( order, value + 1 ) is var _
            ? value
            : default!;

    public void Reset( Order order ) => global::MetalamaInterceptors.Quantity_set_Interceptor( order, 0 );
}

// New syntax tree.
internal static class MetalamaInterceptors
{
    internal static int Quantity_get_Interceptor( this Order receiver )
    {
        var value = receiver.Quantity;
        Console.WriteLine( $"Read Quantity: {value}" );

        return value;
    }

    internal static int Quantity_set_Interceptor( this Order receiver, int value )
    {
        Console.WriteLine( $"Write Quantity: {value}" );

        return receiver.Quantity = value;
    }
}
```

What it shows: `InterceptAccessors` with two registrations, one per accessor (section [5.3.13](05a-api-registration.md#5313-accessors)); a compound site that receives both interceptors; the evaluation of the receiver `this._orders[index]` once, in a block, because the site is a statement and the receiver can have side effects (section [6.4.13](06b-signatures-and-validation.md#6413-accessor-sites)); a postfix increment whose value is used, rewritten with pattern variables, which returns the value read before the increment; a write whose value is discarded, which uses the same setter interceptor as the compound sites (RC49); and `meta.MethodInterception.Destination`, which gives the property. The receiver `order` is a parameter that the site does not assign, so it needs no temporary. The interceptors are in extension form (R1x), so a site `order?.Quantity` would become `order?.Quantity_get_Interceptor()`.
