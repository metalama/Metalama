# Call-site semantics: signatures, placements, validation and caller information

> Part of the [call-site interceptors design](README.md). Previous: [06a-call-site-model.md](06a-call-site-model.md) | Next: [07-await-interception.md](07-await-interception.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

### 6.4 Signature derivation

#### 6.4.1 Receiver mapping

This section decides where the receiver of the intercepted call goes, whether the interceptor is static or an instance method, how the call site is rewritten, and what `meta.This` and `meta.Proceed()` mean (decision PO48, RC37). It replaces the earlier rule that an implicit or explicit `this` receiver is always passed as a parameter (review finding CS-06).

Terms:

- The intercepted call is `r.M(a)`, written in the calling type `C`. The receiver `r` has the static type `TR`. It is written explicitly, or it is an implicit or explicit `this`.
- The interceptor `I` is declared in the type `TI`: the placement, or the declaring type of an existing method.
- Receiver family: `TR` is `TI` or derives from `TI`.
- Caller family: `C` is `TI` or derives from `TI`.

Rules:

| Rule | Applies when | The receiver goes to | Rewritten call | `meta.This` | `meta.Proceed()` |
|---|---|---|---|---|---|
| R0 | The call has no receiver: a static target, a classic extension method (its `this` parameter is the first declared parameter, so the static form is used), a C# 14 extension member (static implementation form), or a static abstract member called through a type parameter (a type argument, not a receiver). | No receiver. The receiver of a reduced extension call is argument 0. | `X.I(a)`, or `this.I(a)` when `I` is an instance member of the caller family | The caller for an instance `I`; not available otherwise | `X.M(a)`, `E.Ext(x, a)`, `E.M(x, a)` or `T.M(a)` |
| R1 | `I` is static. | The first parameter. Struct receivers use the passing modes of section [6.2.7](06a-call-site-model.md#627-arguments-generic-context-and-passing-mode): `ref` for a writable variable, `in` or by value otherwise. | `X.I(r, a)`; implicit `this`: `X.I(this, a)` | Not available | `receiver.M(a)` |
| R1x | As R1, when `TI` is a top-level non-generic static class. | The first parameter, with the `this` modifier. | As R1 for a direct call; `r?.I(a)` for a conditional access | Not available | `receiver.M(a)` |
| R2 | `I` is an instance member, the receiver family holds, and conditions R2a to R2c hold. | The `this` of `I`. No receiver parameter. | `r.I(a)`; implicit `this`: `this.I(a)`; conditional access: `r?.I(a)` | The receiver | `this.M(a)` |
| R3 | `I` is an instance member, the caller family holds, R2 does not apply, and `this` is available at the call site. | The first parameter. | `this.I(r, a)` | The caller | `receiver.M(a)` |
| R4 | The call is `base.M(a)` and `M` is virtual (non-virtual dispatch). `I` is an instance member of `C` itself, or a local function. | The `this` of `I`. | `this.I(a)`; local function: `I(a)` | The caller, which is also the receiver | `base.M(a)` |
| Invalid | `I` is an instance member in neither family. | | | | LAMA1015 for a template, LAMA1013 (E5) for an existing method |

Conditions of R2:

- R2a, proceed binding. The call `this.M(a)`, bound speculatively inside `TI`, binds to the target. This condition addresses review finding CS-06. When `M` is declared only in a type derived from `TI`, or when a member of `TI` or of a type between `TI` and the declaring type of `M` hides `M` or wins overload resolution against it, R2 does not apply, and the engine uses R3 or R1. Types between `TI` and `TR` do not take part in a lookup inside `TI`. A member of such a type that hides `M` makes that member the target of the original call, which is the first case. The engine builds a block statement that declares one local of each parameter type of the target and calls `this.M<TArgs>(...)` with these locals and their reference kinds. It binds the block with `SemanticModel.TryGetSpeculativeSemanticModel( position, statement, out model )` (RC `CSharpExtensions.cs:1212`) at a position inside the body of an instance member of `TI`, and requires the bound method to be the target definition with the same type arguments. The result depends only on `TI` and on the constructed target, so it is memoized per pair. When `TI` has no instance member with a body in source, or is a type introduced by an aspect, the engine uses a conservative structural rule instead: `M` is declared in `TI` or in a base type of `TI`, no type from `TI` up to the declaring type of `M`, excluded, declares a member with the name of `M`, and every member with that name in the declaring type of `M` is accessible from `C`, so that the lookup inside `TI` sees the same candidates as the call site.
- R2b, accessibility through the receiver. `I` must be accessible from `C` through `TR`. A `private` member is accessible through any instance of its own type, so `private` is possible when `C` is `TI` or is nested in it (RC `Binder\Semantics\AccessCheck.cs:363-374`). A `protected` or `private protected` member is accessible from a derived type only through a receiver of that derived type or of a type derived from it, or CS1540 is reported (RC `Binder\Semantics\AccessCheck.cs:416-491`). So `private protected` is possible when `C` derives from `TI` and `TR` is `C` or derives from `C`. In every other case, `I` is `internal`. Section [8.7](08-deduplication-and-naming.md#87-names-and-accessibility) computes the level per group.
- R2c, struct receivers. `r.I(a)` on a variable passes `r` by reference, as the original call does, so no `ref` parameter is needed. A readonly receiver gets the same defensive copy as the original call (RC `Binder\Binder_Invocation.cs:1424-1436`). `I` is `readonly` exactly when `M` is readonly, so the copy happens for `I` exactly when it happened for `M`.

Precedence. R2 is tried before R3. When both apply, for example `other.M()` where `other` is a `C`, the interceptor is a member of the receiver: `meta.This` is `other`, and the caller's `this` is not available to the template. A `configure` function can force R3 or R1 (section [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder)). Under R2, a null receiver throws `NullReferenceException` at the rewritten call, before any template code runs, as the original call would. Under R1 and R3, template code before `meta.Proceed()` runs first (PO17).

Default selection. The engine selects the rule in this order. The first step that applies decides.

1. No receiver: R0, with a static `I`.
2. A `base` call to a virtual method: R4. `TI` must be `C`, or the placement must be a local function. A static interceptor, or an interceptor in another type, is refused with LAMA1015, because it would dispatch virtually and could recurse.
3. A local-function placement: the local-function rule below.
4. The receiver family holds, the receiver type is not a type parameter, and R2a to R2c hold: R2.
5. The receiver family holds, but R2 does not apply: R3 when the caller family holds and `this` is available, and R1 in the other cases. In both cases, the receiver parameter has the type of the target's containing type, which must be accessible in `TI` (check C10). When it is not, the engine reports LAMA1015.
6. Otherwise: R1, or R1x when `TI` is a top-level non-generic static class and the passing mode allows the `this` modifier (see below). A conditional access requires R1x or R2.

A `configure` function can then change the selection: R2 to R3 when the placement is also in the caller family, R2 to R1, R3 to R1, and R1 to R3 when the caller family holds and `this` is available. For R0, it can make `I` an instance method when the caller family holds and `this` is available. It cannot select R2 or R4, and it cannot change R0 or R4. R1 or R1x follows from the placement. A receiver whose type is a type parameter never uses R2 in version 1. Member lookup on a type parameter combines the effective base class and the interface constraints, and the R2a check would have to reproduce it.

Local functions. A local function is declared inside the origin, so a proceed call on `this` binds exactly as the source call does. When the receiver is the caller's implicit or explicit `this` and the calling type is a class, the local function behaves like R2: it has no receiver parameter, the call is `I(a)`, and `meta.Proceed()` is `this.M(a)`. A `base.M(a)` call to a virtual method becomes `I(a)` with `base.M(a)` in the body, because the local function captures `this`. In every other case, the local function behaves like R3: the receiver is its first parameter, and the call is `I(r, a)`. A local function in a struct member cannot capture `this` (CS1673), so a `this` receiver is passed as the first parameter, by reference when the host member is not readonly. The other restrictions of section [6.5.4](#654-local-function-specifics) are unchanged.

Existing methods. The validator of section [6.6](#66-signature-validation-existing-methods-and-adjusted-signatures-r9) derives the expected shape of an existing method `E` from the same rules, with `TI` the declaring type of `E`. A static `E` follows R0, R1 or R1x. An instance `E` follows R4 for a virtual `base` call, R2 when the receiver family holds, and R3 when the caller family holds. When both R2 and R3 are possible, R2 is used, unless `E` has exactly one more required parameter than the callee in static form and the receiver converts to that parameter, in which case R3 is used. Condition R2a does not apply to an existing method, which contains no proceed call. The binder of `E` exposes the derived rule as `IInterceptorMethodBinder.ReceiverMapping`, which is read-only: under R1, R1x and R3, the first parameter receives the receiver unless the `bind` function binds `InterceptorArgument.Receiver` to another parameter (section [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder)).

Parameter list of a synthesized interceptor:

1. The receiver parameter, under R1, R1x and R3, and for a local function that behaves like R3. Classic extension methods and extension members have no additional receiver parameter: their first parameter plays that role.
2. The parameters of the callee in static form: the target itself, the static form of a classic extension method, or `AssociatedExtensionImplementation` for an extension member.
3. The parameters that a `configure` function adds, with their sources (section [5.6.9](05b-api-providers-contexts-results.md#569-added-parameters-and-pulled-values)).

Type of the receiver parameter:

| Receiver | Type |
|---|---|
| Reference type, target not accessed through protected access | The target's containing type, with slots applied. |
| Reference type, protected instance target not reachable through internal access | The placement type (the calling type for a local function). |
| Concrete struct, or type parameter not known to be a reference type | The receiver static type, with slots applied. |

Binding the proceed call against the containing type prevents a `new` member in a derived receiver type from capturing the call, and it groups call sites whose receivers have different derived types (PO16). CS1540 forces the exception for protected access. When the placement type, or a type between it and the target's containing type, declares a member that hides the target, a placement that uses R1 or R3 is not admissible (reason `HiddenTarget`).

Static, instance, readonly, extension form and accessibility:

- R0 (by default), R1 and R1x give a static method. R2, R3 and R4 give an instance method. A local function never has the `static` modifier.
- Under R3 and R4, an instance interceptor in a struct is `readonly` when the host member is readonly. Under R2, it is `readonly` when the target is readonly (condition R2c).
- Extension form (R1x): the receiver parameter gets the `this` modifier whenever the placement is a top-level non-generic static class, an explicit receiver parameter exists, and the mode is `Value`, or `Ref` or `In` on a concrete non-generic struct type (CS8337, CS8338). The first parameter of a classic extension target gets the modifier under the same conditions. The same interceptor then serves conditional-access and direct call sites. A direct call keeps the static form `X.I(r, a)`, which binds without a name lookup and also works for an existing extension method whose class is not in scope at the call site.
- The accessibility of a synthesized method is computed per group (section [8.7](08-deduplication-and-naming.md#87-names-and-accessibility)), unless a `configure` function requests a level. A local function has no accessibility.

Section [6.4.3](#643-worked-examples-of-the-receiver-mapping) shows each rule on an example.

Method-reference sites. The rules R0 to R4 also select the interceptor of a method-reference site, but the rewritten site is a method group, not a call. A method group can carry the receiver only as `Delegate.Target`: under R0, under R2, under R1x, and for a local function that captures the caller's `this`. Under R1 and R3, and for a value-type receiver without R2, the receiver is a parameter, and the site needs a wrapper. For a method-reference site, the default selection therefore tries R2 and then R1x before R3 and R1. Section [6.4.12](#6412-method-reference-sites) gives the shapes, the wrapper and the signature constraints.

##### Receiver mapping as the binder reports it

This subsection shows each value of `InterceptorReceiverMapping` on one program, with the rewritten site, the interceptor, the parameters that `IInterceptorMethodBinder.Parameters` lists, and the meaning of `meta.This` (decision PO62). The rules are those above, applied in the order of the default selection: R0, R4, the local-function rule, R2, the fallback to R3, and R1.

```csharp
public partial class Order
{
    public virtual decimal Total( bool withTax ) { /* ... */ }

    public static Order Parse( string text ) { /* ... */ }
}

public class DiscountedOrder : Order
{
    public override decimal Total( bool withTax ) => base.Total( withTax ) * 0.9m;
}

internal sealed class OrderHooks { }

public abstract class ServiceBase { }

public sealed class OrderService : ServiceBase
{
    public void Process( string text )
    {
        var order = Order.Parse( text );
        var gross = order.Total( true );
        var net = order?.Total( false );
    }
}
```

| `ReceiverMapping` | Site and request | Rewritten site | Interceptor | `Parameters` of the binder | `meta.This` |
|---|---|---|---|---|---|
| `None` (R0), static | `Order.Parse( text )`, `CallingType()` | `Parse_Interceptor( text )` | `private static Order Parse_Interceptor( string text )` in `OrderService` | `text` (bound to `Argument( "text" )`) | Not available |
| `None` (R0), instance | `Order.Parse( text )`, `InType( typeof(ServiceBase) )` with `b.IsStatic = false` | `this.Parse_Interceptor( text )` | `private protected Order Parse_Interceptor( string text )` in `ServiceBase` | `text` | The caller, typed `ServiceBase` |
| `StaticReceiverParameter` (R1) | `order.Total( true )`, `InType( typeof(OrderHooks) )`, a class that is not static | `OrderHooks.Total_Interceptor( order, true )` | `internal static decimal Total_Interceptor( Order receiver, bool withTax )` in `OrderHooks` | `receiver` (`Receiver`), `withTax` (`Argument( "withTax" )`) | Not available |
| `StaticExtensionReceiver` (R1x) | `order.Total( true )` and `order?.Total( false )`, `GeneratedStaticClass()` | Direct call: `MetalamaInterceptors.Total_Interceptor( order, true )`; conditional access: `order?.Total_Interceptor( false )` | `internal static decimal Total_Interceptor( this Order receiver, bool withTax )` in `MetalamaInterceptors` | `receiver`, `withTax` | Not available |
| `InstanceOnReceiver` (R2) | `order.Total( true )` and `order?.Total( false )`, `InType( typeof(Order) )` | `order.Total_Interceptor( true )`; `order?.Total_Interceptor( false )` | `internal decimal Total_Interceptor( bool withTax )` in `Order`, whose body calls `this.Total( withTax )` | `withTax` | The receiver |
| `InstanceOnCaller` (R3) | `order.Total( true )`, `CallingType()` with `b.IsStatic = false` | `this.Total_Interceptor( order, true )` | `private decimal Total_Interceptor( Order receiver, bool withTax )` in `OrderService` | `receiver`, `withTax` | The caller |
| `InstanceBaseCall` (R4) | `base.Total( withTax )` in `DiscountedOrder.Total`, `CallingType()` | `this.Total_Interceptor( withTax ) * 0.9m` | `private decimal Total_Interceptor( bool withTax )` in `DiscountedOrder`, whose body calls `base.Total( withTax )` | `withTax` | The caller, which is also the receiver |

Remarks on the table:

- R1 applies to `OrderHooks` because it is not a static class. With a top-level non-generic static class, the same request gives R1x.
- Under R1x, a direct call keeps the static form, `MetalamaInterceptors.Total_Interceptor( order, true )`, which binds without a lookup of the extension method. Only the conditional access uses the extension form.
- Under R2, `Total_Interceptor` is `internal`, because `OrderService` is not in the hierarchy of `Order` (condition R2b). The proceed call `this.Total( withTax )` dispatches virtually, so a `DiscountedOrder` receiver runs its override.
- Under R3, `order?.Total( false )` cannot be rewritten, because a conditional access needs R1x or R2. The engine reports LAMA1015 for this site.
- Under R2 and R4, the receiver is the `this` of the interceptor, so `Parameters` has no receiver parameter, and `InterceptorArgument.Receiver` is not available.

Permitted overrides of `IInterceptorBuilder.ReceiverMapping` and `IsStatic`:

| Default | Request | Result |
|---|---|---|
| R2 | `InstanceOnCaller`, when the placement is also in the caller family | R3. The receiver parameter is added. |
| R2 | `StaticReceiverParameter`, or `IsStatic = true` | R1. The receiver parameter is added. |
| R3 | `StaticReceiverParameter`, or `IsStatic = true` | R1. |
| R1 | `InstanceOnCaller`, or `IsStatic = false`, when the caller family holds and `this` is available | R3. |
| R0 | `IsStatic = false`, when the placement is the calling type or one of its base types | R0 with an instance method. The mapping stays `None`. |
| R1 or R1x | `StaticReceiverParameter` or `StaticExtensionReceiver` | The placement decides between R1 and R1x. |
| Any | `InstanceOnReceiver` or `InstanceBaseCall`, a change of R0 or R4, or any other change | LAMA1014. |

The mapping of an existing method `E` is derived, and the binder reports it as read-only:

| Existing method `E` | `ReceiverMapping` |
|---|---|
| Static, at a site without receiver, for example `Order.Parse( text )` | `None` (R0) |
| Static, at a site with a receiver | `StaticReceiverParameter` (R1) |
| Extension method in a top-level static class | `StaticExtensionReceiver` (R1x); direct calls keep the static form |
| Instance method of `Order`, the receiver family | `InstanceOnReceiver` (R2) |
| Instance method of `OrderService` or `ServiceBase`, the caller family | `InstanceOnCaller` (R3) |
| Instance method of a type in both families | `InstanceOnCaller` (R3) when `E` has exactly one more required parameter than the callee in static form and that parameter accepts the receiver; otherwise `InstanceOnReceiver` (R2) |
| Instance method of the calling type, at a `base` call to a virtual method | `InstanceBaseCall` (R4) |

Binding example. An existing helper receives the receiver in its second parameter, and the member name of the caller in its first:

```csharp
public static class Telemetry
{
    public static decimal Track( string operation, Order target, bool withTax )
    {
        Console.WriteLine( $"{operation}: Order.Total" );

        return target.Total( withTax );
    }
}

// In the provider, for the site order.Total( true ).
return InterceptorResult.ExistingMethod(
    trackMethod,
    m =>
    {
        m.Parameters["target"].Bind( InterceptorArgument.Receiver );
        m.Parameters["operation"].Bind( InterceptorArgument.CallerInfo( CallerInfoKind.MemberName ) );
    } );

// Rewritten site in OrderService.Process.
var gross = Telemetry.Track( "Process", order, true );
```

`Track` is static and the site has a receiver, so the mapping is `StaticReceiverParameter`, and the canonical binding would give the receiver to the first parameter. The function binds it to `target` instead. `withTax` binds to the argument of the same name. The constant `"Process"` has no side effect, and `order` and `true` keep their order, so no temporary is needed. `Track` returns `decimal`, which the site needs, because the value of `order.Total( true )` is used. The site `order?.Total( false )` cannot use `Track`, because a conditional access needs an extension method or R2 (E8); a provider skips it when `context.IsConditionalAccess` is `true`.

Accessor sites follow the same tables, with the property or event access in place of the call (section [6.4.13](#6413-accessor-sites)). Await sites always report `None`, because an await has no receiver, and the awaited operand is bound through `InterceptorArgument.Awaitable`.

#### 6.4.2 Parameter names

- Target parameters keep the target's names. Named arguments at the call site stay valid, and attributes that name parameters stay valid.
- The receiver parameter is named `receiver`. When a target parameter or an interceptor type parameter uses that name, the suffixes `1`, `2` and so on are appended.
- For a local-function placement, each name that collides with a host parameter, a host type parameter, or a local or local function of the root block of the host is renamed with the host's lexical scope (section [10.4.4](10a-oss-bridge-hook-factory.md#1044-synthesized-methods-and-proceed-bindings)). The rename map is returned in `SynthesizedMethodHandle.RenamedParameters`, and the rewrite plan renames the `NameColon` of named arguments. The attribute copier applies the same map.
- Interceptor type parameters use the names of the definition's type parameters, with a numeric suffix on collision with the type parameters of the placement type or of the host (CS8387 would otherwise warn).

#### 6.4.3 Worked examples of the receiver mapping

The examples use these run-time types. The template of each example logs and calls `meta.Proceed()`. Only the generated interceptor and the rewritten call are shown, and copied attributes such as `[NotNull]` are omitted. Each example is an aspect test of section [12.7](12-test-plan.md#127-premium-aspect-tests-metalamaextensionsinterceptorsaspecttests-and-500) (`Invocations/ReceiverMapping/`).

```csharp
public class Order
{
    public virtual void Validate() { /* ... */ }

    protected void Log( string message ) { /* ... */ }

    public void Place()
    {
        this.Validate();
        Log( "placed" );
    }
}

public class SpecialOrder : Order
{
    public override void Validate()
    {
        base.Validate();
    }
}

internal sealed class OrderHooks { }

internal sealed class PointHooks { }

public struct Point
{
    public int X;

    public void Move( int dx ) => this.X += dx;
}

public abstract class ServiceBase { }

public sealed class OrderService : ServiceBase
{
    private readonly IMessageBus _bus;

    public void Place( Order o ) { /* ... */ }

    public void Submit( Order order )
    {
        order.Validate();
        this._bus.Publish( order );
        this.Place( order );
    }
}
```

1. R1, static helper. Placement `InType( typeof(OrderHooks) )`, call `order.Validate()` in `OrderService.Submit`. `Order` is not in the hierarchy of `OrderHooks`, so the interceptor is static, and the receiver is its first parameter.

   ```csharp
   OrderHooks.Validate_Interceptor( order );

   // In OrderHooks:
   internal static void Validate_Interceptor( Order receiver )
   {
       // Template code.
       receiver.Validate();
   }
   ```

2. R1x, extension form. Placement `GeneratedStaticClass()`, calls `order.Validate()` and `order?.Validate()`. The generated class is a top-level non-generic static class, so the receiver parameter has the `this` modifier, and one method serves both call sites. In the conditional access, the receiver stays in the chain, so `order` is evaluated once and the call is skipped when it is null.

   ```csharp
   MetalamaInterceptors.Validate_Interceptor( order );
   order?.Validate_Interceptor();

   internal static class MetalamaInterceptors
   {
       internal static void Validate_Interceptor( this Order receiver )
       {
           // Template code.
           receiver.Validate();
       }
   }
   ```

3. R2, instance interceptor in the receiver's type. Placement `InType( typeof(Order) )`, call `order.Validate()` in `OrderService.Submit`. `Order` is the static type of the receiver, and `this.Validate()` binds inside `Order` to the target (R2a). `Order` does not need to be `partial`, because the linker adds the member.

   ```csharp
   order.Validate_Interceptor();

   // In Order:
   internal void Validate_Interceptor()
   {
       // Template code. meta.This is the receiver.
       this.Validate();
   }
   ```

   The proceed call `this.Validate()` is a virtual call, so a `SpecialOrder` receiver still runs `SpecialOrder.Validate`. The method is `internal`, because `OrderService` is not in the hierarchy of `Order` (R2b).

4. R2 with an implicit `this`. The same placement, call `Log( "placed" )` in `Order.Place`.

   ```csharp
   this.Log_Interceptor( "placed" );

   // In Order:
   private void Log_Interceptor( string message )
   {
       // Template code.
       this.Log( message );
   }
   ```

5. R3, instance interceptor in the calling type with a receiver of another type. Placement `CallingType()` with `configure: b => b.IsStatic = false`, call `this._bus.Publish( order )` in `OrderService.Submit`. `IMessageBus` is not in the hierarchy of `OrderService`, so R2 does not apply.

   ```csharp
   this.Publish_Interceptor( this._bus, order );

   private void Publish_Interceptor( IMessageBus receiver, object message )
   {
       // Template code. meta.This is the caller.
       receiver.Publish( message );
   }
   ```

6. R4, `base` call to a virtual method. Placement `CallingType()`, call `base.Validate()` in `SpecialOrder.Validate`.

   ```csharp
   this.Validate_Interceptor();

   // In SpecialOrder:
   private void Validate_Interceptor()
   {
       // Template code.
       base.Validate();
   }
   ```

7. R2 refused, fallback to R3. Placement `InType( typeof(ServiceBase) )`, call `this.Place( order )` in `OrderService.Submit`. The receiver family holds, because `OrderService` derives from `ServiceBase`. `Place` is declared only in `OrderService`, so `this.Place( o )` does not bind inside `ServiceBase`, and condition R2a fails. The caller family holds and `this` is available, so the engine uses R3. The receiver parameter has the type of the target's containing type, `OrderService`, which `ServiceBase` can name because it is public. If `OrderService` could not be named in `ServiceBase`, the engine would report LAMA1015.

   ```csharp
   this.Place_Interceptor( this, order );

   // In ServiceBase:
   private protected void Place_Interceptor( OrderService receiver, Order o )
   {
       // Template code. meta.This is the caller.
       receiver.Place( o );
   }
   ```

8. Struct receiver under R1 and under R2. Call `point.Move( 1 )` on a local variable `point`. With the placement `InType( typeof(PointHooks) )`, where `PointHooks` is a non-static class, R1 passes the writable variable by reference:

   ```csharp
   PointHooks.Move_Interceptor( ref point, 1 );

   internal static void Move_Interceptor( ref Point receiver, int dx )
   {
       // Template code.
       receiver.Move( dx );
   }
   ```

   With the placement `InType( typeof(Point) )`, R2 applies, and C# passes `point` by reference as the `this` of the interceptor, as for the original call:

   ```csharp
   point.Move_Interceptor( 1 );

   // In Point:
   internal void Move_Interceptor( int dx )
   {
       // Template code.
       this.Move( dx );
   }
   ```

   In version 1, the `ref` receiver parameter of R1 is declared without `scoped`, which section [6.2.7](06a-call-site-model.md#627-arguments-generic-context-and-passing-mode) allows here because no type of the signature is ref-like.

#### 6.4.4 Defaults and params

The target has one `DefaultMode`, which is part of the signature:

- `Declared`: every optional parameter has an explicit default that C# can write in a parameter declaration of the same type: `null` or `default`, a constant of the parameter type (or of its underlying type for `Nullable<T>` and enumerations), or a string for a `string` parameter. Any other constant, such as `5` on an `object` parameter produced by `[DefaultParameterValue]`, cannot be declared (CS1763), and the target uses the `Materialized` mode. The interceptor parameters copy the defaults. Omitted arguments stay omitted, and Roslyn binds the same defaults at the rewritten call.
- `Materialized`: at least one optional parameter has a special default (`IsOptional` without `HasExplicitDefaultValue`, `[DateTimeConstant]`, `[IUnknownConstant]`, `[IDispatchConstant]`). The interceptor parameters have no default. Every omitted argument is appended as a named argument whose value is the value that Roslyn bound at the source call (RC `Binder\Binder_Invocation.cs:1693-1790`). If a value has no supported shape, the call site gets `UnsupportedDefaultValue`.

`params` is copied when the target parameter is `params`. The call site keeps its expanded or normal argument list. The proceed call passes the collection in normal form.

The following rules complete these two modes. Each rule has an aspect test in section [12.7](12-test-plan.md#127-premium-aspect-tests-metalamaextensionsinterceptorsaspecttests-and-500) (`Invocations/Parameters/`).

a. Template binding. A run-time template parameter binds by name to the interceptor parameter, as every run-time parameter does (section [5.7.4](05c-api-templates.md#574-template-signature-rules)). For a `params` parameter, the template parameter is declared with the collection type, for example `object[]`, or with `dynamic`, and without `params`. For an optional parameter, the template parameter is declared without a default. The template declares no default and no `params` on run-time parameters, because the generated signature comes from the call site.

   ```csharp
   // Target: void Write( string format, params object[] args ).
   [Template]
   private void LogWrite( string format, object[] args )
   {
       Console.WriteLine( $"{format}: {args.Length} arguments" );
       meta.Proceed();
   }
   ```

b. Empty expanded `params`. `M()` against `params int[] xs` stays `I()`, so that the compiler creates the same empty array, or the same empty span, at the rewritten call. The Materialized mode never treats an empty expanded `params` argument as an omitted argument: it appends no argument for the `params` parameter. `IArgumentOperation` reports the argument with the kind `ParamArray` or `ParamCollection` and no syntax (section [6.2.7](06a-call-site-model.md#627-arguments-generic-context-and-passing-mode)), and the rewrite plan ignores it.

c. Appended named arguments and `params`. A positional argument binds to an optional parameter before it binds to a `params` element (RC `Binder\Semantics\OverloadResolution\OverloadResolution_ArgsToParameters.cs:271-302`). An optional parameter that precedes a `params` parameter can therefore be omitted, while the `params` parameter is used in expanded form, only in one case: the `params` argument is named, as in `M( "a", args: 1 )`. Roslyn accepts this form with exactly one element (lines 306-339), and no positional argument can follow it (lines 283-288). A named argument can follow it, because the only restriction on arguments that follow an out-of-position named argument is that they are named (lines 201-237). Appending named arguments for omitted optional parameters is therefore always valid.

   ```csharp
   void M( string a, [CallerMemberName] string n = "", params object[] args );

   M( "a", "b", 1, 2 );  // n = "b" is explicit. Rewritten: I( "a", "b", 1, 2 ).
   M( "a", args: 1 );    // n is omitted. Rewritten: I( "a", args: 1, n: "Caller" ).
   ```

   The test `Invocations/Parameters/CallerInfoBeforeParams_NamedParamsArgument` compiles both call sites and runs them, and its expected output shows `n` and the elements of `args` equal to those of the control call without interception.

d. Defaults after lifting and specialization.
   - A default on a parameter whose type is a lifted interceptor type parameter is emitted as `default`. C# allows only `null` or `default` for such a parameter, and `default` is valid whatever the constraints.
   - An enumeration default is emitted as the member name when a member has that value, and otherwise as a cast of the constant to the enumeration type. This is the existing behavior of `ContextualSyntaxGenerator.EnumValueExpression`, which uses the first member with that value in declaration order, and a cast otherwise (ENG27 `SyntaxGeneration\ContextualSyntaxGenerator.cs:379-414`). The cast goes through `SafeCastExpression`, which adds parentheses around the operand when C# requires them (line 1026).
   - A `decimal` default is emitted with the `M` suffix, as `SyntaxFactoryEx.LiteralExpression` does for every `decimal` literal (ENG27 `SyntaxGeneration\SyntaxFactoryEx.cs:363`; RC `SymbolDisplay\ObjectDisplay.cs:504-508`).
   - A `DateTime` default, which C# can only express with `[DateTimeConstant]`, always selects the Materialized mode.

   ```csharp
   // Target, as declared in metadata: void Save( FileAccess access = (FileAccess) 3, FileShare share = (FileShare) 64, decimal rate = 1.5m ).
   static void Save_Interceptor( FileAccess access = FileAccess.ReadWrite, FileShare share = (FileShare) 64, decimal rate = 1.5M );
   ```

   `FileAccess.ReadWrite` has the value 3, so the member form is used. No member of `FileShare` has the value 64, so the cast form is used.

e. Group key. The `DefaultMode` is part of the signature and therefore of the key (section [8.2](08-deduplication-and-naming.md#82-the-key)). The default values themselves come from the target definition, which the key already contains through the proceed target (section [8.4](08-deduplication-and-naming.md#84-proceed-target-identity)), so they need no separate component.

f. Existing methods and defaults. An existing method `E` may declare parameters that the intercepted method does not have. An optional parameter that no source binds receives the default of `E`. A caller-information parameter binds canonically to `InterceptorArgument.CallerInfo` and receives the value materialized from the source call site (section [6.7](#67-caller-information-materialization)). Any parameter can be bound explicitly, for example to a pulled value (section [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder)). Rule E7 of section [6.6](#66-signature-validation-existing-methods-and-adjusted-signatures-r9) states this.

g. Existing methods and `params`. E11 covers the expanded form: the parameter of `E` must be `params` with an identical type. A call in normal form, `M( array )`, also accepts an `E` whose parameter is not `params`, through an implicit conversion of the argument (E10).

   ```csharp
   // Target: void Log( params string[] lines ). Existing method: static void LogAll( IEnumerable<string> lines ).
   Log( lines );       // Accepted: LogAll( lines ).
   Log( "a", "b" );    // Refused by E11: LogAll is not params.
   ```

h. `meta.Proceed()` and `params`. The proceed call passes the collection in normal form: `M( args )`. A template may read the collection parameter. For a `params` span, or for a ref struct collection type built through `CollectionBuilderAttribute`, the template must not store the collection beyond the call, for example in a field or in a lambda, because the ref-safety rules of the span apply to template code as to any other code. The compiler reports such an error in the generated code. A template that needs another collection builds it with ordinary code, for example `var copy = args.ToArray();`. The engine offers no API to rebuild the arguments of `meta.Proceed()`.

i. Method-reference sites. A method-reference site passes no argument, so rules b, c and f do not apply to it. The interceptor must use the `Declared` mode, and it copies every default value and `params` modifier of the target exactly, because the natural function type of `var d = M;` contains them: Roslyn synthesizes a delegate type with the default values, `params`, the reference kinds and the scopes of the method when one of them is present, and uses `Func` or `Action` only otherwise (RC `Binder\Binder_Expressions.cs:11804-11812, 12187-12250`). A target in the `Materialized` mode therefore gives the limitation `MethodReferenceRequiresMaterializedDefaults` at a method-reference site. A method-reference site that stays a method group belongs to a group without added parameters; a method-reference site whose group has added parameters uses the lambda wrapper of section [5.6.10](05b-api-providers-contexts-results.md#5610-the-callers-instance-and-method-reference-sites) (section [6.4.12](#6412-method-reference-sites)).

j. Groups with added parameters (section [5.6.9](05b-api-providers-contexts-results.md#569-added-parameters-and-pulled-values), RC65). An added parameter is required and trailing, so a group that has one uses the `Materialized` mode, and its interceptor declares the collection parameter of a `params` target without the `params` modifier. The rewrite packs the expanded arguments of the site into a collection expression, or into an array creation before C# 12, and passes an empty collection for an empty expanded form, so rule b does not apply to such a group. Rule c does not apply either, because every argument of the target is then passed. The added arguments are appended as named arguments.

#### 6.4.5 Attributes

| Attribute | Location | Policy |
|---|---|---|
| `CallerLineNumber`, `CallerFilePath`, `CallerMemberName`, `CallerArgumentExpression` | parameter | Not copied. Values are always passed explicitly (section [6.7](#67-caller-information-materialization)). |
| `AllowNull`, `DisallowNull`, `MaybeNull`, `NotNull`, `MaybeNullWhen`, `NotNullWhen`, `NotNullIfNotNull`, `DoesNotReturnIf` | parameter and return | Copied. Parameter names are remapped with the rename map. |
| `DoesNotReturn` | method | Copied. |
| `MemberNotNull`, `MemberNotNullWhen` | method | Not copied. They refer to members of the target's type. The lost flow state can cause new nullable warnings after the call (PO18). |
| `InterpolatedStringHandlerArgument` | parameter | Copied. `""` becomes the receiver parameter name when the receiver is a parameter (R1, R1x, R3), and stays `""` under R2, where the receiver is the `this` of the interceptor; other names follow the rename map, including the renames of a `configure` function. |
| `UnscopedRef` | parameter | Copied when the factory supports it. |
| `Obsolete`, `Experimental` | method | Copied. The call-site diagnostic keeps its identifier, and usage inside the interceptor is not reported because the interceptor is itself obsolete or experimental. |
| `RequiresUnreferencedCode`, `RequiresDynamicCode`, `RequiresAssemblyFiles` | method | Copied. Trimming and ahead-of-time analysis work on IL, so the requirement must flow to the callers of the interceptor. |
| `Conditional` | method | Not copied (check C11). |
| Compiler-emitted attributes (`IsReadOnly`, `ParamArray`, `ParamCollection`, `ScopedRef`, `Nullable`) | any | Not copied. They are expressed by modifiers. |
| Other attributes | any | Not copied. |

An attribute is emitted only when its type, resolved by metadata name in the current compilation (`Compilation.GetTypeByMetadataName`), exists and is accessible from the placement. The C# compiler recognizes the nullable flow attributes by namespace and name, so a polyfill of the current project is sufficient. The attribute type of the target can be an internal polyfill of the target's assembly, which the current project cannot access (CS0122). netstandard2.0 and .NET Framework do not declare `NotNullAttribute` (CS0246). When the type is not available, the attribute is omitted, and the flow state that it carries is lost (PO18). The same rule applies to the `[NotNull]` attribute of the receiver parameter (rules table row 2).

Analyzers run on source code by default in the Metalama compiler (Metalama.Compiler `src\Compilers\Core\Portable\CommandLine\CommonCompiler.cs:1191-1255`), so analyzer attributes such as `SupportedOSPlatform` need no copy.

Method-group convertible signatures. A delegate created from a rewritten method group exposes the interceptor through `Delegate.Method`. Frameworks that read the attributes of `Delegate.Method` and of its parameters then see the interceptor. For example, ASP.NET minimal APIs bind the parameters of a handler passed as `app.MapGet( "/orders", GetOrders )` by reading attributes such as `[FromServices]` and `[FromQuery]` on the parameters of the delegate method. This document did not verify the ASP.NET Core source; the runtime test `MethodReference_DelegateMethodAttributes` of section [12.8](12-test-plan.md#128-premium-runtime-execution-tests-runtime) checks the attributes that reflection sees, and an ASP.NET scenario is part of the corpus of section [12.9](12-test-plan.md#129-semantic-equivalence-corpus-test). An interceptor whose signature is method-group convertible (section [6.4.12](#6412-method-reference-sites)) therefore also copies the attributes that the table classifies as "Other attributes", on the method, on the return value and on each parameter, with these exceptions:

- the attributes that the table excludes: caller-information attributes, `MemberNotNull`, `MemberNotNullWhen`, `Conditional` and the compiler-emitted attributes;
- the attributes that change how the method itself is compiled or run, and that must stay on the target: `DllImport`, `MethodImpl`, `PreserveSig`, `ModuleInitializer`, `UnmanagedCallersOnly`, `SkipLocalsInit`, and the attributes of the `System.Runtime.CompilerServices` namespace that the compiler reserves;
- attributes whose type is not accessible from the placement, with the rule of the previous paragraph.

The policy is a function of the signature, not of the kinds of site in the group. A group that contains only call sites also copies these attributes when its signature is method-group convertible, so call sites and method-reference sites with the same signature share one method (section [8.2](08-deduplication-and-naming.md#82-the-key)). The copied attributes have no effect on a call. The parameter names are always the target's names (section [6.4.2](#642-parameter-names)), which frameworks that bind by name also read.

#### 6.4.6 Generic specialization and lifting

Default policy: specialize, and lift only when specialization is impossible (PO15).

- A specialized interceptor uses the constructed types of `TargetSymbol` directly. Type parameters visible at the placement count as nameable: the calling type's type parameters when the placement is the calling type or a type nested in it, and the host method's type parameters for a local function.
- A slot must be lifted when its type argument contains a type that the placement cannot name: a type parameter not visible at the placement, an anonymous type, a file-local type (unless the placement is a local function or a file-local type of the call-site file, check C12), or a type that `IsSymbolAccessibleWithin(type, placement)` rejects.

Justification: R9 asks for the intercepted signature, and a specialized signature is exactly the signature the call site uses. Constraint copying, the most error-prone part of lifting, is needed only in generic contexts. Lifting still solves every case that specialization cannot.

Lifting algorithm:

1. Build the slots (section [6.2.7](06a-call-site-model.md#627-arguments-generic-context-and-passing-mode)).
2. Mark each slot `Closed(type)` or `Lifted(ordinal)`. When no anonymous type occurs, only the slots that must be lifted are lifted. When an anonymous type occurs in any slot, every method-level slot is lifted, and the containing-type slots that must be lifted are lifted.
3. For each lifted slot, create an interceptor type parameter. Copy the constraints of the slot's defining type parameter: reference type (with nullable annotation), value type, unmanaged, not null, constructor, `AllowsRefLikeType`, and each constraint type with its annotation, substituted with the slot mapping. Type parameters referenced by constraints are lifted transitively.
4. Every signature type is the definition's type with the slot mapping substituted.
5. Type arguments at the rewritten call: when no anonymous type occurs, the call passes explicit type arguments (the original type argument of each lifted slot, which the call site can always name). When an anonymous type occurs, type arguments are omitted.

The omitted-argument case is sound: the method-level slots are all lifted, so the interceptor's parameter types are the target definition's parameter types with the same method type parameters, and the arguments are the same expressions. The inference problem is therefore the one the original call solved. Lifted containing-type slots occur in the receiver parameter and are inferred exactly from the receiver argument.

Constraint copying is exact. `T?` means `Nullable<T>` only under a struct constraint, so the copy must include the struct constraint. A missing `AllowsRefLikeType` would reject ref-struct type arguments.

#### 6.4.7 Return type and nullability

- The return type is the target's return type with the slot mapping and its annotations. Ref returns never reach this step.
- The nullable context of the interceptor is part of the signature. When the call site's nullable annotation context is disabled (`SemanticModel.GetNullableContext(position)`), the signature types are oblivious and the interceptor is emitted with `#nullable disable`. Otherwise it is emitted with `#nullable enable` and the exact annotations.

#### 6.4.8 Signature adjustments of the builder

The static, instance, readonly and extension-form rules, and the accessibility rules, are part of the receiver mapping (section [6.4.1](#641-receiver-mapping)). This section states how the adjustments and the bindings of an `IInterceptorBuilder` (sections [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder) and [5.6.9](05b-api-providers-contexts-results.md#569-added-parameters-and-pulled-values)) change the results of sections [6.4.1](#641-receiver-mapping) to [6.4.10](#6410-rewrite-plan). The bindings of an `IInterceptorMethodBinder` change only the rewrite plan.

| Adjustment | Signature | Rewrite plan | Proceed shape |
|---|---|---|---|
| `Name` | The requested name of the key (section [8.2](08-deduplication-and-naming.md#82-the-key)). | The callee name. | Unchanged. |
| `Accessibility` | The requested accessibility of the key. Section [8.7](08-deduplication-and-naming.md#87-names-and-accessibility) uses it instead of the computed level. | Unchanged. | Unchanged. |
| `IsStatic`, `ReceiverMapping` | The mapping of section [6.4.1](#641-receiver-mapping) is recomputed with the requested rule: the receiver parameter is added or removed, and the method kind changes. | The receiver mode of section [6.4.10](#6410-rewrite-plan) follows the new rule. | `InvokeOnThis` for R2, `InvokeOnParameter` for R1, R1x and R3 (section [6.4.9](#649-proceed-shape)). |
| Parameter type | The `InterceptorParameter.Type` of the parameter. | E10 of section [6.6](#66-signature-validation-existing-methods-and-adjusted-signatures-r9) decides whether the argument is cast to the original parameter type first. | The argument gets a cast to the original parameter type (`ProceedArgument.CastType`). |
| Return type | `InterceptorSignature.ReturnType`. | E14 decides the result cast. | Unchanged. The template produces the new type. |
| Parameter name | `InterceptorParameter.Name`. Copied attributes that name the parameter follow the rename (section [6.4.5](#645-attributes)). | Named arguments are renamed with the `ArgumentNameMap` of the substitution (section [10.5.6](10b-oss-linker-and-templates.md#1056-syntax-of-the-rewritten-call)). | Unchanged: the proceed call is positional. |
| Added parameter | A trailing `InterceptorParameter` with the role `Added` and no default. The signature of the group uses the `Materialized` mode, and a `params` parameter loses its modifier (section [6.4.4](#644-defaults-and-params), rule j). | An `AppendedArgument` with the value of the source at the site: a pulled expression, caller information (section [6.7](#67-caller-information-materialization)) or `this`. A `params` argument is packed. | The parameter is not passed. |
| Binding of a parameter | None: bindings are not part of the signature or of the key. | The `ArgumentPlan` of section [6.4.10](#6410-rewrite-plan): the order of the arguments, the temporaries and the discards. | When a parameter of the target is bound to another source than its argument, `meta.Proceed()` passes the parameter, which holds the value of that source. |
| `DefaultMode` | `InterceptorSignature.DefaultMode`. | The appended arguments of the Materialized mode (section [6.4.4](#644-defaults-and-params)). | Unchanged. |

After the function returns, `InterceptorSignatureValidator` validates the adjusted signature and the bindings at the call site (section [6.6](#66-signature-validation-existing-methods-and-adjusted-signatures-r9)). When the function changed no type, no name and no parameter, and bound no parameter explicitly, the signature is correct by construction, and the validator skips the speculative binding of E16.

#### 6.4.9 Proceed shape

The proceed shape depends only on the key, so it is the same for every member of a group. The engine translates it into the open-source `ProceedBinding` (section [10.4.4](10a-oss-bridge-hook-factory.md#1044-synthesized-methods-and-proceed-bindings)).

```csharp
/// <summary>Describes the expression that meta.Proceed() generates in a method interceptor.</summary>
internal sealed record InvocationProceedShape(
    ProceedCalleeKind CalleeKind,                    // StaticMember, ReceiverParameter, This, Base, TypeParameterStatic,
                                                     // ClassicExtensionStaticForm, ExtensionImplementationStaticForm.
    IMethodSymbol Callee,
    SignatureType? QualifyingType,
    ImmutableArray<SignatureType> MethodTypeArguments, // Always explicit when the callee is generic.
    int ReceiverParameterIndex,                      // -1 when the receiver is not an interceptor parameter.
    ImmutableArray<ProceedArgument> Arguments );

/// <summary>One argument of the proceed call, in callee parameter order.</summary>
/// <param name="CastType">The type to which the argument is cast: <c>object</c> for a <c>dynamic</c> parameter, the original
/// parameter type for a parameter that a configure function widened, and <c>null</c> otherwise.</param>
internal readonly record struct ProceedArgument( int InterceptorParameterIndex, ArgumentModifier Modifier, SignatureType? CastType );
```

| `ProceedCalleeKind` | Receiver-mapping rule (section [6.4.1](#641-receiver-mapping)) | `ProceedBinding` factory |
|---|---|---|
| `StaticMember`, `ClassicExtensionStaticForm`, `ExtensionImplementationStaticForm` | R0 | `ProceedBinding.InvokeStatic( method, receiverType, typeArguments, argumentParameterIndices )` |
| `TypeParameterStatic` | R0 | `ProceedBinding.InvokeStatic( method, receiverType: T1, ... )` |
| `ReceiverParameter` | R1, R1x, R3, and a local function that behaves like R3 | `ProceedBinding.InvokeOnParameter( method, receiverParameterIndex, ... )` |
| `This` | R2, and a local function whose receiver is the caller's `this` in a class | `ProceedBinding.InvokeOnThis( method, ... )` |
| `Base` (virtual target) | R4 | `ProceedBinding.InvokeOnBase( method, ... )` |

A proceed call written `this.M( args )` binds by member lookup in the placement, not in the calling type. It would not compile when the target is declared only in a type derived from the placement, and it would select another method when the placement declares an applicable member with the same name. For this reason, the `This` callee kind exists only under R2, whose condition R2a verifies by speculative binding that the lookup inside the placement gives the target, and in a local function, which is declared in the origin and therefore binds as the source call does. Every other instance call uses a receiver parameter typed as the target's containing type (section [6.4.1](#641-receiver-mapping)). The review of [Appendix C](appendix-c-review-log.md#appendix-c-review-log) had removed the `This` kind (finding CS-06). The follow-up product-owner review reintroduced it with this guard ([Appendix D](appendix-d-product-owner-reviews.md#appendix-d-product-owner-reviews-of-2026-09-24-and-2026-09-25), D-H).

- Arguments are positional, in callee parameter order. The modifier is `ref`, `out` or `in` according to the callee `RefKind`; `ref readonly` uses `in`.
- A parameter typed `dynamic` is passed as `(object)p`, so that the proceed call stays statically bound. A parameter whose type a `configure` function widened is passed as `(T)p`, with `T` the parameter type of the target. Both use the argument casts of `ProceedBinding` (section [10.4.4](10a-oss-bridge-hook-factory.md#1044-synthesized-methods-and-proceed-bindings)).
- Parameters added by a `configure` function are not passed.
- For a generic callee, the type arguments are explicit. Together with identically typed arguments and a receiver typed as the target's containing type, or the `this` of a placement verified by R2a, overload resolution selects exactly the target.

#### 6.4.10 Rewrite plan

```csharp
/// <summary>Describes how the linker replaces one source invocation. Translated into an InvocationRedirectionRequest.</summary>
internal sealed record InvocationRewritePlan(
    InvocationExpressionSyntax SourceNode,
    InterceptorHandle Interceptor,                   // Group handle for a synthesized method, or the existing IMethodSymbol.
    CallSiteReceiverMode ReceiverMode,               // Open-source enumeration, section 10.4.5.
    ImmutableArray<ITypeSymbol>? ExplicitTypeArguments, // Null when type arguments are omitted.
    ImmutableArray<AppendedArgument> AppendedArguments, // Caller information, materialized defaults and added arguments.
    ArgumentPlan? Arguments,                         // Null for the source order; set when a binding reorders, drops or adds values.
    ITypeSymbol? ResultCast );                       // Set only when the value of the call is used (section 6.6, E14).

/// <summary>
/// A named argument appended to the rewritten call. The value is produced by ArgumentValueMaterializer, or is the
/// expression of a pulled value, of caller information or of this.
/// </summary>
internal sealed record AppendedArgument( string ParameterName, ExpressionSyntax Value );

/// <summary>
/// The argument list of a rewritten call whose binding differs from the source order (section 5.6.8). It is site data
/// and is never part of the key.
/// </summary>
internal sealed record ArgumentPlan(
    ImmutableArray<PlannedArgument> Arguments,       // One entry per interceptor parameter that the call passes, in order.
    ImmutableArray<int> DiscardedSourceArguments,    // Source arguments that no parameter receives and that can have side effects.
    bool RequiresTemporaries );                      // True when the order of evaluation of the source values would change.

/// <summary>One argument of an ArgumentPlan: a source value (receiver or argument), a packed params argument, or an expression.</summary>
internal readonly record struct PlannedArgument( InterceptorArgumentKind Kind, int SourceOrdinal, ExpressionSyntax? Expression, string? Name );
```

| Receiver-mapping rule, internal receiver kind and passing mode | `CallSiteReceiverMode` |
|---|---|
| R0 (`None`, `TypeParameterStatic`), and R4 (`Base` with a virtual target) | `Drop` |
| R1, R1x or R3, with the receiver kind `Expression`, `ImplicitThis`, `ExplicitThis`, `PointerIndirection`, or `Base` with a non-virtual target (modeled as `ExplicitThis`, section [6.2.4](06a-call-site-model.md#624-target-and-dispatch)), and the mode `Value` or `In` | `FirstArgument` (the factory writes `this` for an implicit receiver and for a `base` receiver, and `*p` for a pointer receiver) |
| same with mode `Ref` | `FirstArgumentByRef` |
| same with mode `In`, for an existing method whose receiver parameter is `ref readonly` (section [6.6](#66-signature-validation-existing-methods-and-adjusted-signatures-r9), E8) | `FirstArgumentByIn` |
| R1x, with the receiver kind `ConditionalAccess` | `ExtensionReceiver` |
| R2, with any receiver kind except `Base` with a virtual target, including `ConditionalAccess` | `MemberOfReceiver` (the factory keeps the receiver and replaces the member name; an implicit receiver is written `this`) |

Under R1, R1x and R3, an implicit or explicit `this` receiver is passed as the first argument, so that the proceed call binds on a parameter typed as the target's containing type (section [6.4.9](#649-proceed-shape)). Under R2, the receiver stays the receiver of the call: `r.I(a)`, `this.I(a)` or `r?.I(a)`. A local function that behaves like R2 is called without receiver, `I(a)`, with the mode `Drop`.

The linker applies the plan bottom-up to the already rewritten node (section [10.5.6](10b-oss-linker-and-templates.md#1056-syntax-of-the-rewritten-call)), so the plan describes positions, not text. An argument that contains another intercepted call is rewritten first. The `ArgumentPlan` reaches the linker as the argument list of the redirection request (section [10.4.5](10a-oss-bridge-hook-factory.md#1045-call-site-redirections)), and the linker writes the temporaries and the discards with the forms of section [6.4.13](#6413-accessor-sites).

#### 6.4.11 Signature types

```csharp
/// <summary>
/// A type of the interceptor signature. It can contain interceptor-owned type parameters, which do not exist as symbols
/// before the open-source factory creates the method.
/// </summary>
internal abstract record SignatureType
{
    /// <summary>A type that the placement can name, compared with SymbolEqualityComparer.IncludeNullability.</summary>
    public sealed record Closed( ITypeSymbol Type ) : SignatureType;

    /// <summary>The interceptor type parameter at <paramref name="Ordinal"/>.</summary>
    public sealed record InterceptorTypeParameter( int Ordinal, NullableAnnotation Annotation ) : SignatureType;

    /// <summary>A constructed generic type whose arguments contain interceptor type parameters.</summary>
    public sealed record Constructed( INamedTypeSymbol Definition, ImmutableArray<SignatureType> TypeArguments, NullableAnnotation Annotation ) : SignatureType;

    /// <summary>An array type whose element type contains interceptor type parameters.</summary>
    public sealed record Array( SignatureType ElementType, int Rank, NullableAnnotation Annotation ) : SignatureType;
}

/// <summary>The derived signature of an interceptor, after the adjustments of the configure function.</summary>
internal sealed record InterceptorSignature(
    InterceptorMethodKind MethodKind,                // Static, StaticExtension, Instance, InstanceReadOnly, LocalFunction.
    InterceptorReceiverMapping ReceiverMapping,      // The rule R0 to R4 of section 6.4.1.
    ImmutableArray<InterceptorTypeParameter> TypeParameters,
    ImmutableArray<InterceptorParameter> Parameters, // Receiver first when present, added parameters last.
    SignatureType ReturnType,
    ImmutableArray<AttributeCopy> ReturnAttributes,
    ImmutableArray<AttributeCopy> MethodAttributes,
    DefaultMode DefaultMode,
    Accessibility? RequestedAccessibility,           // Set by the configure function; null for the computed level.
    NullableContextKind NullableContext );

/// <summary>One parameter of the interceptor.</summary>
internal sealed record InterceptorParameter(
    string Name,
    SignatureType Type,
    RefKind RefKind,
    ScopedKind ScopedKind,                           // The explicit scope of section 6.2.3, not IParameterSymbol.ScopedKind.
    bool IsParams,
    bool IsThis,
    TypedConstant? DefaultValue,
    InterceptorParameterRole Role,                   // Internal enumeration: Receiver, TargetParameter or Added.
    int TargetParameterOrdinal,                      // -1 for the receiver and for added parameters.
    SignatureType? OriginalType,                     // The derived type when the configure function widened it; otherwise null.
    ImmutableArray<AttributeCopy> Attributes );      // The source of an added parameter is site data, in the rewrite plan.

internal static class InterceptorSignatureBuilder
{
    /// <summary>
    /// Derives the signature, the rewrite plan and the proceed shape for a call site in an admissible placement, invokes
    /// the configure function on an InterceptorBuilder, applies its adjustments and bindings (section 6.4.8), and
    /// validates the result with InterceptorSignatureValidator (section 6.6).
    /// </summary>
    public static InterceptorPlanResult Build(
        InvocationCallSite callSite,
        AdmissiblePlacement placement,
        Action<IInterceptorBuilder>? configure,
        SemanticModel semanticModel,
        CancellationToken cancellationToken );

    /// <summary>
    /// Writes the signature into the builder of the open-source factory. Interceptor type parameters are created first,
    /// so that SignatureType.InterceptorTypeParameter can be resolved to the new IMethodBuilder type parameters.
    /// </summary>
    public static void Apply( InterceptorSignature signature, IMethodBuilder builder, CompilationModel compilation );
}
```

#### 6.4.12 Method-reference sites

This section specifies how a method-reference site (section [5.3.11](05a-api-registration.md#5311-kinds-of-method-use), [6.2.10](06a-call-site-model.md#6210-method-reference-sites)) is rewritten, and which signature its interceptor must have. The interceptor method, the template and `meta.Proceed()` are those of a call site. Only the rewritten site and the constraints on the signature differ.

Shapes. The engine selects the receiver mapping with the rules of section [6.4.1](#641-receiver-mapping), and tries R2 and then R1x before R3 and R1, because these rules keep the receiver in the method group.

| Method group at the site | Rule | Interceptor | Rewritten method group | `Delegate.Target` |
|---|---|---|---|---|
| `C.M` or `M`, static target | R0 | static `I` | `global::X.I`; a local function: `I` | `null`, unchanged |
| `C.M` or `M`, static target, instance `I` of the caller family requested by `configure` | R0 | instance `I` | `this.I` | the caller instead of `null` |
| `r.M`, `this.M` or `M`, receiver in the hierarchy of the placement | R2 | instance `I` | `r.I` or `this.I` | the receiver, unchanged |
| `base.M`, virtual target | R4 | instance `I` of the calling type, which calls `base.M` | `this.I` | the caller, unchanged |
| `r.M`, or a classic extension method group `x.Ext`, when the type of the receiver parameter is a reference type | R1x | static `I` with a `this` parameter | `r.I` | the receiver, unchanged |
| `M` or `this.M`, receiver is the caller's `this` in a class, local-function placement | local function | local function `I` | `I` | the caller, unchanged |
| any other receiver: R1, R3, a value-type receiver without R2, a classic extension method on a value type | wrapper | as for a call | the wrapper below | the closure of the wrapper |
| `&C.M` (function pointer) | R0 | static `I` in a type | `&global::X.I` | not applicable |

An extension method whose `this` parameter has a value type, or a type parameter that is not constrained to a reference type, cannot form a delegate (CS1113, RC `Binder\Semantics\Conversions\Conversions.cs:340-372`), so R1x requires a reference type. A local function is not used for a function-pointer site in version 1.

Type arguments. When the interceptor is generic, the rewritten method group has explicit type arguments, for example `global::X.I<int>`. The type arguments of the site are fixed by the source binding. With explicit type arguments, the method group has one candidate whose parameter and return types are fixed, so the type inference of an enclosing call, such as the inference of `TResult` in `list.Select( Transform )`, has the same result.

Uniqueness. The name of a synthesized interceptor is unique in its placement (section [8.7](08-deduplication-and-naming.md#87-names-and-accessibility)), so the rewritten method group has exactly one candidate. The natural function type of `var d = X.I;` requires a method group whose methods have one signature (RC `Binder\Binder_Expressions.cs:11804-11812`). Under R1x, check C5 guarantees that no other extension method with the final name is in scope at the site.

Signature constraints. The C# rules of a method-group conversion, verified in the Roslyn fork, are:

- Overload resolution considers only the normal form of each candidate: `params` is never expanded (RC `Binder\Semantics\OverloadResolution\OverloadResolution_ArgsToParameters.cs:445`).
- An optional parameter is not optional in a method-group conversion, so the candidate must have exactly as many parameters as the delegate (same file, lines 388-401).
- For a by-value parameter, an identity or implicit reference conversion must exist from the delegate parameter type to the method parameter type. For a `ref`, `out` or `in` parameter, the reference kind must match and the types must be identical. The return type must convert from the method to the delegate by an identity or implicit reference conversion, with the same reference kind. A function pointer also accepts pointer conversions, requires a static method, and refuses a reduced extension method (RC `Binder\Binder_Conversions.cs:3516-3659`).
- The natural function type of a method group contains the parameter types, the reference kinds, the default values, `params`, the scopes, `[UnscopedRef]`, and the return type with its reference kind (RC `Binder\Binder_Expressions.cs:12187-12250`).

The design rule is stricter than the conversion rule, because the natural function type and the inference of an enclosing call depend on the exact types. The signature of an interceptor at a method-reference site, after the receiver mapping, has the parameter types, the reference kinds, the default values, `params`, the scopes and the return type of the target, and no additional parameter. The default mode is `Declared`, and the binding is canonical. The builder can change only the name, the accessibility and the receiver mapping for the site to stay a method group (section [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder)). An existing method must satisfy the same rule (E18, section [6.6](#66-signature-validation-existing-methods-and-adjusted-signatures-r9)). The contravariance that the conversion allows is not used.

The signature is method-group convertible when it satisfies this rule. The property depends only on the signature, so it is computed for every request, including call sites, and it is part of the key (section [8.2](08-deduplication-and-naming.md#82-the-key)). A call site and a method-reference site with the same signature therefore share one method. A method-reference site whose signature is not method-group convertible after its `configure` function, whose group has added parameters, or whose binding is not canonical, uses the wrapper below, with the lambda form of section [5.6.10](05b-api-providers-contexts-results.md#5610-the-callers-instance-and-method-reference-sites) (RC69). Earlier versions reported such a site with LAMA1014.

Wrapper. When no method-group shape is admissible, the receiver must be passed as a parameter, and a method group cannot carry it. The engine then writes a wrapper that evaluates the receiver once, when the delegate is created, and creates a delegate of the converted type `D` that calls the interceptor. The same wrapper serves a binding that a method group cannot express: the inner lambda passes the parameters of `D` and the values of the other sources, which are evaluated at each invocation, as in the equivalent lambda form (section [5.6.10](05b-api-providers-contexts-results.md#5610-the-callers-instance-and-method-reference-sites)). A site without receiver needs no outer lambda:

```csharp
// Source. Format is an instance method of PriceFormatter, and the provider requested a static interceptor in the
// generated static class (rule R1).
var texts = prices.Select( formatter.Format );

// Rewritten.
var texts = prices.Select(
    ( (global::System.Func<global::Contoso.PriceFormatter, global::System.Func<decimal, string>>)
        ( static receiver =>
        {
            if ( receiver is null )
            {
                throw new global::System.NullReferenceException();
            }

            return ( decimal price ) => global::MetalamaInterceptors.Format_Interceptor( receiver, price );
        } ) )
    ( formatter ) );
```

- The outer lambda is `static` and receives the receiver as its parameter. Its immediate invocation evaluates `formatter` once, when the delegate is created, as the method-group conversion does. The `static` modifier needs C# 9, and it is omitted for earlier language versions. It is also omitted when the inner lambda captures a pulled value, `this` or a local of the site, because a static lambda cannot capture them.
- The inner lambda declares the parameter types and reference kinds of `D`, and captures the receiver parameter.
- The cast names `Func<TR, D>`, so `D` must be nameable at the site. An anonymous delegate type, which is the natural function type of a method group with default values, and a type that contains an anonymous type, give the limitation `MethodReferenceReceiverNotSupported`.
- A value-type receiver is copied into the closure, as a method-group conversion copies it into a box. When the receiver mode of section [6.2.7](06a-call-site-model.md#627-arguments-generic-context-and-passing-mode) is `Ref`, the inner lambda passes `ref receiver`, so the mutations persist across the invocations of the delegate, as they persist on the box.
- The outer lambda checks a reference-type receiver for null before it creates the inner delegate, so the exception is thrown when the delegate is created, as for the method group. Roslyn emits no null check for a method-group conversion: for a virtual target, `ldvirtftn` fails on the null receiver, and for a non-virtual target, the delegate constructor of the runtime receives the null target (RC `CodeGen\EmitConversion.cs:342-390`). The wrapper always throws `NullReferenceException`. The runtime test `MethodReference_Wrapper_NullReceiver` records the exception types of the original and of the wrapper for a virtual and a non-virtual target, and the documentation states any difference.
- The compiler caches the outer lambda, which captures nothing, in a static field (RC `Lowering\ClosureConversion\ClosureConversion.cs:1683-1696`). Each evaluation of the site allocates a closure and a delegate, where the method-group conversion allocated one delegate.
- `Delegate.Target` is the closure and `Delegate.Method` is the inner lambda. Two evaluations of the same site create delegates that are not equal. The wrapper is therefore never used for an event subscription, because `-=` would never remove the handler: such a site gets the limitation `DelegateEqualityRequired`. It is never used for a function pointer or a ref-like receiver either.

The wrapper is the program that keeps the semantics of the method group with a lambda: the receiver is evaluated once, at delegate creation. The lambda form `x => formatter.Format( x )` evaluates `formatter` at each invocation, so the wrapper follows the method-group form, not the lambda form. Whether the wrapper is the default, or whether such sites get the limitation and the warning LAMA1012, is decision PO51. The recommendation is the wrapper: without it, `list.Select( formatter.Format )` escapes an interception that `list.Select( x => formatter.Format( x ) )` receives, which is the inconsistency that RC44 removes.

Behavior changes at method-reference sites, documented for users:

| Topic | Original | Rewritten |
|---|---|---|
| `Delegate.Method` | The target. | The interceptor, or the inner lambda of a wrapper. The interceptor copies the attributes of section [6.4.5](#645-attributes) and the parameter names of the target. |
| Equality of two delegates created in the scope | Equal when the target and the receiver are equal. | Equal for a method group of the interceptor. Never equal for a wrapper. |
| Equality with a delegate created outside the scope | Equal. | Not equal. An event handler added in the scope is not removed by `-=` outside the scope, and the reverse. The hidden diagnostic LAMA1019 marks each intercepted event subscription. |
| Null receiver | Exception when the delegate is created. | Under R2 and with the wrapper, an exception is also thrown when the delegate is created. Its type can differ when the target is virtual and the interceptor is not, or the reverse (runtime test `MethodReference_NullReceiver_ExceptionAtCreation`). Under R1x, the delegate is created, and `meta.Proceed()` throws when the delegate is invoked, after any template code that precedes it. This is the method-reference form of PO17. |
| Allocation | One delegate. | One delegate for a method group; a closure and a delegate for a wrapper. |

#### 6.4.13 Accessor sites

This section specifies the signatures and the rewrites of accessor sites (section [5.3.13](05a-api-registration.md#5313-accessors), [6.2.11](06a-call-site-model.md#6211-accessor-sites)). The interceptor method, the template, `meta.Proceed()` and the group key follow the rules of method interceptors, with the accessor as the intercepted method.

Signatures. `TR` is the receiver parameter type of section [6.4.1](#641-receiver-mapping), `T` the property type, and `THandler` the event type.

| Accessor | Shape | Example |
|---|---|---|
| Getter | `T I( TR receiver )` | `static int Quantity_get_Interceptor( Order receiver )` |
| Setter | `T I( TR receiver, T value )`, which returns the assigned value | `static int Quantity_set_Interceptor( Order receiver, int value )` |
| Add, remove | `void I( TR receiver, THandler handler )` | `static void Changed_add_Interceptor( Order receiver, EventHandler handler )` |

A synthesized setter interceptor always returns `T`, so that one interceptor serves the sites whose value is used and the sites whose value is discarded (RC49). `meta.Proceed()` of a setter emits `receiver.P = value`. The value of this expression is the value of the original assignment expression: the right operand converted to `T`, not the value that the getter returns afterwards. A setter template therefore writes `return meta.Proceed();`. The parameter of a setter keeps the name `value` of the accessor. The parameter of an add or remove interceptor is named `handler`, because the name of the implicit parameter of the accessor is not visible at the site.

An existing method is admissible under rule E19 of section [6.6](#66-signature-validation-existing-methods-and-adjusted-signatures-r9): a `void` setter only at sites whose value is not used, and a setter whose return type is implicitly convertible to `T` at every site.

Receiver mapping. The rules R0 to R4 and R1x of section [6.4.1](#641-receiver-mapping) apply unchanged, with the property or event access in place of the call:

- R0: a static property or event has no receiver: `T I()`, `T I( T value )`, `void I( THandler handler )`. A C# 14 extension property uses the static implementation form of its accessors, with the receiver as the first parameter, as a C# 14 extension method does (rules table row 92).
- R1 and R1x: a static interceptor receives the receiver as its first parameter. In extension form (R1x), `r?.P` becomes `r?.I()` and `r?.P = v` becomes `r?.I( v )`, and the same method serves direct sites in static form.
- R2: the interceptor is an instance member of the receiver family, without receiver parameter: `r.I()`, `r.I( v )`, `this.I()` for an implicit `this`. Condition R2a binds `this.P` or `this.E += handler` speculatively inside the placement. `meta.Proceed()` is `this.P`, `this.P = value`, or `this.E += handler`.
- R3: an instance member of the caller family receives the receiver as its first parameter: `this.I( r )`.
- R4: `base.P`, `base.P = v` and `base.E += h` on a virtual member become `this.I()`, `this.I( v )` and `this.I( h )`, with `base.P`, `base.P = value` and `base.E += handler` in the body.
- Struct receivers use the passing modes of section [6.2.7](06a-call-site-model.md#627-arguments-generic-context-and-passing-mode). The setter of a writable struct variable receives it by `ref`. The getter receives it by `in` when the getter is readonly, which is the case of the getter of an auto-property, and by `ref` otherwise.

Rewrite of a site with one accessor use:

| Site | Rewrite (R1) |
|---|---|
| `r.P` | `Ig( r )` |
| `r.P = v` | `Is( r, v )`, whose value is the value of the assignment |
| `r.E += h`, `r.E -= h` | `Ia( r, h )`, `Ir( r, h )` |

Rewrite of a compound site (RC50). The receiver is evaluated exactly once, as in the original code. When the receiver needs a temporary (section [6.2.11](06a-call-site-model.md#6211-accessor-sites), step 8), the form depends on the context:

```csharp
// Statement context. The enclosing ExpressionStatementSyntax becomes a block.
r.P += 1;          // becomes:  { var t = r; Is( t, Ig( t ) + 1 ); }
r.P ??= v;         // becomes:  { var t = r; if ( Ig( t ) is null ) Is( t, v ); }
s.P += 1;          // struct variable, no temporary:  Is( ref s, Ig( s ) + 1 );
a[i++].P += 1;     // struct element that needs a temporary:  { ref var t = ref a[i++]; Is( ref t, Ig( t ) + 1 ); }

// Receiver without side effects, in any context: no temporary.
this.P += 1;       // becomes:  Is( this, Ig( this ) + 1 )

// Expression context: pattern variables.
x = r.P += 1;      // becomes:  x = r is var t ? Is( t, Ig( t ) + 1 ) : default!;
x = r.P++;         // becomes:  x = r is var t && Ig( t ) is var old && Is( t, old + 1 ) is var _ ? old : default!;
x = r.P ??= v;     // becomes:  x = r is var t ? ( Ig( t ) is { } c ? c : Is( t, v ) ) : default!;
```

Rules of these forms:

- Evaluation stays left to right, and each subexpression runs once: the receiver, then the getter, then the right operand, then the operator, then the setter. The pattern variable `t` holds the receiver, and `old` holds the value read by a postfix increment.
- A `var` pattern always matches, so the `default!` branch is unreachable. The `!` operator suppresses the nullable warning that `default` would cause for a non-nullable reference type or an unconstrained type parameter. A `var` pattern does not test for null, so the nullable state of the receiver after the site is the state that the original code gives it.
- The new value is computed with the operator of the original site, with its conversions and its checked context. When `ICompoundAssignmentOperation.OutConversion` is an explicit conversion, the rewrite writes it, for example `Is( t, (byte)( Ig( t ) + 1 ) )`. For `++` and `--` on a type whose operator is not `+ 1`, such as an enumeration or a type with a user-defined operator, the rewrite applies the `++` or `--` operator to the value, through a second pattern variable in an expression and through a local in a statement.
- For `??=` on a nullable value type `T?`, the null test is `Ig( t ) is null`, and the value of an expression-context site has the underlying type `T`, as in C#, so the rewrite writes `Is( t, v ).GetValueOrDefault()` in the branch that assigns.
- Expression variables of an expression statement have the scope of the enclosing block, so the names of the pattern variables and of the locals are reserved per site with the lexical scope of the host (section [10.4.6](10a-oss-bridge-hook-factory.md#1046-names-and-lexical-scopes)).
- In a position that accepts only a statement expression but is not an expression statement, such as a `for` incrementor or the body of a `void` lambda, the expression form is written after a discard assignment, `_ = r is var t ? ... : default!`. When `_` binds to a variable at the site, for example a lambda parameter named `_`, the site gets `ReceiverTemporaryNotPossible`.
- When only one accessor use of a compound site is intercepted, the other accessor stays a plain access on the same temporary: `t.P = Ig( t ) + 1` or `Is( t, t.P + 1 )`.

Section [6.2.11](06a-call-site-model.md#6211-accessor-sites) lists the positions that were verified to accept these forms. The linker performs the rewrite (section [10.5.6](10b-oss-linker-and-templates.md#1056-syntax-of-the-rewritten-call)).

### 6.5 Placement admissibility

#### 6.5.1 Placement request

The public `InterceptorPlacement` (section [5.6.3](05b-api-providers-contexts-results.md#563-interceptorplacement)) is resolved into one of these requests. The request names only the declaration. The receiver mapping of section [6.4.1](#641-receiver-mapping), possibly changed by the `configure` function, decides whether the method is static:

```csharp
/// <summary>A placement that an interceptor provider requested for a template result.</summary>
internal abstract record PlacementRequest
{
    public sealed record InType( INamedType Type ) : PlacementRequest;
    public sealed record BaseMostAccessibleType : PlacementRequest;
    public sealed record GeneratedStaticClass : PlacementRequest;
    public sealed record LocalFunction : PlacementRequest;
}

internal static class PlacementAdmissibilityChecker
{
    /// <summary>
    /// Returns the admissible placement with its receiver mapping, or the first reason why the request cannot be honored for
    /// this call site. The requested mapping is null for the default selection of section 6.4.1.
    /// </summary>
    public static PlacementAdmissibility Check(
        InvocationCallSite callSite,
        PlacementRequest request,
        InterceptorReceiverMapping? requestedMapping,
        CompilationModel sourceCompilation );
}
```

`CallingType()` resolves to `InType( callingType )`. `BaseMostAccessibleType()` resolves to `InType( T )`, where `T` is the type that the walk of section [6.5.6](#656-base-most-accessible-type) selects. The checker first runs the default selection of section [6.4.1](#641-receiver-mapping). When a `configure` function changes the mapping, the engine runs the checker again with the requested mapping, and reports the reason when the requested mapping is not admissible. An `INamedType` introduced by an aspect has no symbol in the source compilation. For accessibility checks, it is represented by the assembly for a top-level type, and by its source declaring type for a nested type. `SupportsPlacement` on the public context calls this checker.

#### 6.5.2 Checks

The checks run in order. The first failure produces LAMA1015 with the reason.

| Id | Reason | Rule |
|---|---|---|
| C1 | `PlacementNotInCurrentCompilation` | The type must be a source type of the current compilation or a type introduced by an aspect. Implicitly declared types (the implicit `Program` of top-level statements) are not admissible. |
| C2 | `PlacementTypeKindNotSupported` | Classes, structs, records and record structs only. |
| C3 | `CompileTimePlacement` | `IMemberOrNamedType.ExecutionScope` must be `RunTime` (FW27 `Code\IMemberOrNamedType.cs:84`). |
| C4 | `ShapeRequiresOtherPlacementKind` | The rules-table column: base calls to a virtual method need `C` or `L` (R4); conditional access needs `X` or `V`; instance mappings on a static class are refused. |
| C5 | `ExtensionPlacementRequired` | Conditional access with a static interceptor (R1x; an instance interceptor under R2 needs no extension form): a top-level, non-generic, static class (CS1106, CS1109), whose extension methods are in scope at the call site. No using directive is added (RC25). The final name must also be free at every call site of the group: `SemanticModel.LookupSymbols( position, container: receiverType, name: candidateName, includeReducedExtensionMethods: true )` must return no symbol. Without `container`, the lookup ignores extension methods (RC `Compilation\CSharpSemanticModel.cs:1593-1596`). The lookup also finds an instance member of the receiver type with the same name, which member lookup prefers, a same-named extension method in a closer namespace, which extension lookup finds first, and the extension methods of an internal class of a referenced assembly that `InternalsVisibleTo` makes visible, such as the generated static class of a friend assembly (CS0121). The premium engine runs this lookup through `SynthesizedMethodRequest.IsNameAvailable` (section [10.4.4](10a-oss-bridge-hook-factory.md#1044-synthesized-methods-and-proceed-bindings)), so that it applies to the final name that the linker allocates, not to the requested name. |
| C6 | `GenericPlacementNotEnclosing` | Every generic type in the chain of containing types of the placement type must enclose the call site, or be a base type of the calling type reached through inheritance. |
| C7 | `InstanceInNeitherFamily` | An instance mapping needs a family (section [6.4.1](#641-receiver-mapping)). `V` (R2): the receiver family, with conditions R2a to R2c. `H` (R3, and R0 with an instance method): the caller family, the placement being the calling type or a source base type. `C` (R4): the placement is exactly the calling type. |
| C8 | `ThisNotAvailable` | `H` and `C`: `IsThisAvailable`. `V` needs `this` only when the receiver is an implicit or explicit `this`, which the source call already requires. |
| C9 | `LocalFunctionNotPossible` (with a sub-reason) | `L`: body kind is `MethodBody` or `ExpressionBody`; no static enclosing function; language version at least C# 8; for a base call, the calling type is a class. |
| C10 | `TargetNotAccessible`, `TypeNotAccessible(type)`, `HiddenTarget` | `IsSymbolAccessibleWithin(callee, within, throughType)` must hold, with `throughType` the receiver parameter type for instance targets, or the placement type under R2. Each closed type of the final signature must be accessible within the placement. `HiddenTarget` applies to R1 and R3 (section [6.4.1](#641-receiver-mapping)); under R2, a hidden target makes condition R2a fail, and the default selection falls back to R3 or R1. Section [6.5.5](#655-access-to-private-and-protected-targets) explains when this check restricts the placement, and why the calling type and a local function always pass it. |
| C11 | `ConditionalSymbolNotDefined(symbol)` | For a present `[Conditional]` call, the tree where the method will be injected must define at least one conditional symbol of the target. The generated static class uses a new tree with the project parse options. |
| C12 | `FileLocalTypeRequiresFileLocalPlacement(type)` | When a file-local type occurs in the final signature or in a copied constraint, the placement must be a local function, or a file-local type (or a type nested in one) declared in the call-site tree. C# rejects a file-local type in the signature of a member of a type that is not file-local (CS9051, RC `Symbols\Source\SourceMemberMethodSymbol.cs:393-419`; `Binder\Binder_Constraints.cs:459-473`). All parts of a file-local type are in one file, so the factory needs no tree preference. |
| C13 | `RequiresNewerLanguageVersion(feature)` | For example `private protected` (C# 7.2) and local functions (C# 7). |

#### 6.5.3 Availability by context

| Context | S | X | V | H | C | L |
|---|---|---|---|---|---|---|
| Instance method, accessor, constructor body | yes | yes | yes | yes | yes | yes |
| Static method, static constructor, operator | yes | yes | yes | no | no | yes |
| Instance or static field or property initializer | yes | yes | yes | no | no | no |
| Constructor initializer, primary-constructor base arguments | yes | yes | yes | no | no | no |
| Top-level statements | yes (not the calling type) | yes | yes | no | no | no (version 1) |
| Lambda inside a class member (not static) | yes | yes | yes | yes | yes | yes |
| Static lambda or static local function | yes | yes | yes | no | no | no |
| Lambda or local function inside a struct member | yes | yes | yes | no (CS1673) | no | yes |
| Readonly struct member | yes | yes | yes, `readonly` when the target is | yes, `readonly` | yes, `readonly` | yes |
| Expression-bodied member | yes | yes | yes | yes | yes | yes (block conversion by the linker) |
| Member of a C# 14 extension block | yes | yes | yes | no | no | no (`this` is not available in an extension member, and the factory rejects the host, section [10.4.8](10a-oss-bridge-hook-factory.md#1048-validation-performed-by-the-factory)) |

`V` is available in every context, because it uses the receiver of the source call and not the caller's `this`. When the receiver is an implicit or explicit `this`, the source call already requires `this`.

#### 6.5.4 Local-function specifics

- The local function is appended to the root block of the host body, outside the linker's entry blocks (section [10.5.4](10b-oss-linker-and-templates.md#1054-local-function-injection)). It follows the body when the linker moves or inlines it.
- The host method's type parameters and the calling type's type parameters are visible. The type parameters of an enclosing generic local function are not visible, so they are lifted.
- The arguments are passed explicitly. The receiver is passed as the first parameter, except when it is the caller's implicit or explicit `this` in a class member, or the `base` of a virtual call. In these cases, the local function captures `this`, and `meta.Proceed()` is `this.M(a)` or `base.M(a)` (section [6.4.1](#641-receiver-mapping)). In a struct member, a `this` receiver is always passed as the first parameter, because a local function cannot capture `this` there (CS1673). Template code can also capture host state, subject to `NonCapturableHostParameters` and `IsThisCapturableByLocalFunction` (CS1628, CS1673, ref-like capture). The engine reports LAMA1015 before expansion when the host has such parameters and the template is known to capture parameters through `meta.MethodInterception.Origin`; otherwise the compiler reports the error on the generated code.
- A call site inside a static lambda or static local function cannot call a capturing local function (check C9).

#### 6.5.5 Access to private and protected targets

The interceptor method calls the intercepted method in `meta.Proceed()`. It must therefore have access to that method. Check C10 enforces this rule. This section explains when the rule restricts the choice of placement, and what a later version could do.

Facts:

- The call site always has access to the intercepted method. Otherwise, the original call would not compile.
- An interceptor in the calling type has the same access as the call site, because accessibility in C# is decided by the containing type. A local function in the origin also has the same access. These two placements can therefore always call the target.
- A placement elsewhere, such as an explicit `InType` or the generated static class, can lack access. This happens when the target is `private`, `protected` or `private protected`, or when it is a member of a file-local type and the placement is in another file. An `internal` member of another assembly that grants `InternalsVisibleTo` to the current assembly is not a problem: every placement is in the current assembly, and every type of the current assembly has access to it. The practical cases are therefore private, protected and file-local targets.
- For a protected instance target, C# also constrains the receiver: the receiver must be of the type that makes the access, or of a type derived from it (CS1540, RC `Errors\ErrorCode.cs:713`; resource text "the qualifier must be of type '{2}' (or derived from it)"). A placement that derives from the declaring type but not from the calling type fails this rule for a receiver typed as the calling type. `IsSymbolAccessibleWithin( symbol, within, throughType )` checks both conditions (RC `Core\Portable\Compilation\Compilation.cs:1647`), which is why check C10 passes the receiver type as `throughType`.
- The same restriction applies to C# interceptors, which must be accessible at the call site and must be able to call the interceptable method (RCDOCS `features\interceptors.md:249`).

Option 1, version 1. The interceptor must have access. When a template result or an existing method requests a placement without access, the engine reports LAMA1015 (template) or LAMA1013 (existing method) with the reason clause of C10. The documentation of the clause suggests `InterceptorPlacement.CallingType()` or `InterceptorPlacement.LocalFunction()`, which always have access. `InterceptionContext.SupportsPlacement` returns `false` with the same sentence, so a provider can choose another placement before it returns. No new diagnostic identifier is allocated.

Option 2, future, sketch only (section [16.4](16-future-directions.md#164-accessor-mode-for-inaccessible-targets)). An accessor mode gives an interceptor elsewhere a way to call an inaccessible target. The engine generates, at the call site or once per calling type, a static lambda that has access because it is declared in the calling type, and passes it to the interceptor as an extra argument:

```csharp
// Call site in OrderService, before:
this.Validate( order, strict );

// Call site after the rewrite, with an interceptor in the generated static class:
MetalamaInterceptors.Validate_Interceptor(
    this,
    ( order, strict ),
    static ( r, args ) => r.Validate( args.Item1, args.Item2 ) );

// Interceptor: meta.Proceed() invokes the accessor.
internal static bool Validate_Interceptor( OrderService r, (Order, bool) args, Func<OrderService, (Order, bool), bool> proceed )
{
    Log( "Validate" );

    return proceed( r, args );
}
```

- The accessor packs the arguments in a `ValueTuple`, so one delegate type `Func<TR, (A, B), R>` serves every arity up to the limit of `Func`.
- A static lambda captures nothing. The compiler caches a lambda that captures nothing in a static field, except in a static constructor (RC `Lowering\ClosureConversion\ClosureConversion.cs:1683-1696`). After the first call, the accessor therefore allocates nothing.
- The cost is one delegate invocation per call. A function pointer would avoid the delegate, but it requires unsafe code in the calling type and in the interceptor.
- `ref`, `out` and `in` parameters remain limitations, because `Func` and `ValueTuple` cannot carry references. Ref-struct arguments remain limitations, because a ref struct cannot be a type argument of `ValueTuple` (CS9244, RC `Errors\ErrorCode.cs:2324`). Ref returns remain limitations.
- One interceptor can still serve every call site of a group, because the accessor is a parameter and not part of the body.

The accessor mode is not implemented in version 1. Version 1 keeps it open with two properties of the design: the proceed binding is declarative (`ProceedBinding`, section [10.4.4](10a-oss-bridge-hook-factory.md#1044-synthesized-methods-and-proceed-bindings)), so a later binding kind can invoke a delegate parameter instead of the target; and the call-site rewrite already appends extra arguments (`CallSiteExtraArgument`, section [10.4.5](10a-oss-bridge-hook-factory.md#1045-call-site-redirections)), so a later request can append the tuple and the accessor.

#### 6.5.6 Base-most accessible type

`InterceptorPlacement.BaseMostAccessibleType()` lets the engine choose the type placement (decision PO61, RC61). The engine resolves it for each site in stage 1, before the key is computed. The result depends only on the calling type and on the site, so it is deterministic.

Walk:

1. The candidates are the calling type `C`, then its base class, then the base class of that class, and so on. The list ends before the first base class that is not a source type of the current compilation (check C1). A type of a referenced assembly cannot receive a member, and its base classes are not source types either.
2. For each candidate `T` after `C`, the engine runs the checks of section [6.5.2](#652-checks) for `InType( T )`, with the default selection of the receiver mapping of section [6.4.1](#641-receiver-mapping).
3. The walk stops at the first candidate that fails a check. The selected type is the last candidate that passed. The engine never skips a failing candidate to reach a type above it, so the selected type is always reached through a chain of admissible types.
4. When no base class passes, the selected type is `C`, and the engine continues as for `CallingType()`. When `C` is not admissible either, for example for a site in top-level statements, the engine reports LAMA1015, as for `CallingType()`.

The placement is then `InType( T )`. A `configure` function that changes the receiver mapping is checked against `T`, and a failure is reported with LAMA1015, as for `InType`. The walk is not repeated with the requested mapping.

The checks that usually decide the walk are the following. All of them are checks of `InType( T )`:

- Access to the destination (check C10). The intercepted member must be accessible from `T`. A `private` member is accessible only within its declaring type and the types nested in it (RC `Binder\Semantics\AccessCheck.cs:363-374`). A private destination therefore keeps the method in the calling type, which must be the declaring type or a type nested in it. A `protected` member declared in a type `B` is accessible from `T` only when `T` is `B` or derives from it, so the walk stops below `B`. For an instance protected target, C# also requires that the receiver has the type of `T` or a type derived from it (RC `Binder\Semantics\AccessCheck.cs:475`, CS1540). The receiver parameter then has the type `T` (section [6.4.1](#641-receiver-mapping)), and the site passes a receiver of `C` or of a type derived from `C`, because the original call already satisfied the same rule for `C`. `C` derives from `T`, so the proceed call passes the check. A `public` or `internal` destination is accessible from every type of the compilation, so the walk reaches the root of the source hierarchy unless another check fails.
- Types of the signature (check C10, reason `TypeNotAccessible`). Every closed type of the signature must be accessible within `T`. A type argument of a slot that `T` cannot name is lifted (section [6.4.6](#646-generic-specialization-and-lifting)). A type that is not in a slot cannot be lifted, for example the receiver parameter type. A site in `C` that calls a public method of a private nested type `C.Line` has the receiver parameter type `C.Line`, which no base class of `C` can name, so the walk stops at `C`.
- Generic base types (check C6, section [6.4.6](#646-generic-specialization-and-lifting)). A generic base class is reached through inheritance, so check C6 accepts it. The rewritten site names the construction that `C` inherits, for example `ServiceBase<Order>.I( ... )`, or calls `this.I( ... )`. A type of the signature that mentions a type parameter of `C` is not visible in `T`, and section [6.4.6](#646-generic-specialization-and-lifting) lifts it. A base class nested in a generic type that `C` does not inherit fails check C6, so the walk stops below it.
- Instance mappings (checks C7 and C8, condition R2a). Under R2, `this.M( a )` bound speculatively inside `T` must bind to the target. When it does not, the default selection falls back to R3 or R1 in `T`, as for any `InType` placement, and the walk continues. The caller family holds for every candidate, because `C` derives from each of them. R3 and an instance R0 need `this` at the site (check C8), which does not depend on `T`. A `base` call to a virtual method requires the placement to be `C` itself (R4, check C7), so the walk stops at `C`.
- Hidden target (check C10, reason `HiddenTarget`). Under R1 and R3, a member of `T`, or of a type between `T` and the containing type of the target, that hides the target makes `T` fail.
- The other checks apply as for `InType`: a compile-time type (C3), the conditional symbols of the tree of `T` (C11), file-local types (C12) and the language version (C13).

The factory never produces the codes `X` and `L` of section [6.3](06a-call-site-model.md#63-rules-table). A base class is never a static class, because a static class cannot be used as a base class (CS0709, RC `Errors\ErrorCode.cs:495`), so the extension form R1x is not available. A conditional-access site therefore keeps a base class only when R2 applies in it. The walk never selects a local function.

Consequences:

- Sharing. The placement identity of the key (section [8.2](08-deduplication-and-naming.md#82-the-key)) is the definition of `T`. Sites in sibling types that reach the same `T` share one method when the rest of the key is equal, as in the example of section [5.6.3](05b-api-providers-contexts-results.md#563-interceptorplacement). The registering owner is still part of the key (section [8.5](08-deduplication-and-naming.md#85-implementation-identity-and-the-user-contract)), so an aspect applied to each sibling type still produces one method per aspect instance. A fabric registration shares the method.
- Accessibility (section [8.7](08-deduplication-and-naming.md#87-names-and-accessibility)). The method is `private` when `T` is `C`, and `private protected` when `C` derives from `T`. Under R2, it is `private protected` only when the receiver has the type `C` or a type derived from it, and `internal` otherwise.
- `meta.This`. In an instance placement, `meta.This` has the type `T` (section [5.7.1](05c-api-templates.md#571-metatarget)). A template can use only the members that are visible in `T`. The engine cannot detect before expansion that a template needs a member of `C`, because a member access on `meta.This` is run-time code that the template compiler emits without binding it. Stage 1 does not detect it, and the expansion does not either. The C# compiler reports the error on the generated code, for example CS1061 or CS0122, at the placement. The documentation states the rule: a template that uses members of the calling type requests `CallingType()`. A template can also test `meta.Target.Type` in compile-time code. This is a weak spot of version 1 ([Appendix B](appendix-b-weak-spots.md#appendix-b-weak-spots-that-remain-after-the-review), item 38).
- Files. The method is added to the file that declares `T`. An interception in a derived type therefore changes the transformed code of the file of its base class. The preview of the file of a derived type does not contain the method when `T` is declared in another file. The site is then not rewritten in the preview, and LAMA1030 records the reason when the tree of `T` is outside the preview compilation (section [9.8](09-premium-engine.md#98-preview-live-templates-and-introspection)). A build that changes only a derived type can change the transformed code of the file of the base class. This is visible in the transformed files (`LamaDebug`), and it matters for any future cache of transformed code per syntax tree.

### 6.6 Signature validation: existing methods and adjusted signatures (R9)

#### 6.6.1 Rules

One validator, `InterceptorSignatureValidator`, checks both paths of R9, together with the binding of their parameters (section [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder)):

- an existing method `E` returned by a provider, after its `bind` function has run;
- a synthesized signature after its `configure` function has run (section [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder)). The rules then apply to the planned method, which this section also calls `E`.

The validation runs per call site, on the binding of that site: the canonical binding completed by the explicit bindings. A violation produces LAMA1013 with the reason given in parentheses. Rules E1 to E4 and E17 apply only to existing methods, because a synthesized method is ordinary, is not the target, is run-time code, has no `[Conditional]` attribute (section [6.4.5](#645-attributes)), and has no body in source. E6 is always satisfied by a synthesized signature, whose arity comes from the lifting of section [6.4.6](#646-generic-specialization-and-lifting). Every other rule applies to both paths, so R9 holds for both with one implementation.

- E1 (`NotAnOrdinaryMethod`, existing methods only). `E.MethodKind` is `Ordinary` (a classic extension method is ordinary). Local functions, lambdas, accessors, operators, constructors and extension-block members are refused.
- E2 (`InterceptorIsTarget`, existing methods only). `E` is not the target definition.
- E3 (`CompileTimeMethod`, existing methods only). `E` is run-time code.
- E4 (`ConditionalInterceptor`, existing methods only). `E` has no `[Conditional]` attribute.
- E5 (`InstanceInNeitherFamily`, `ThisNotAvailable`, `ReadOnlyRequired`, `BaseCallRequiresCallingType`). The expected shape of `E` comes from the receiver-mapping rules of section [6.4.1](#641-receiver-mapping), applied with `E.IsStatic` and with the declaring type of `E` as `TI`. A static `E` follows R0, R1 or R1x and can be in any accessible type. An instance `E` follows R4 for a virtual `base` call, and must then be declared in the calling type itself. Otherwise, it follows R2 when the receiver family holds, or R3 when the caller family holds; section [6.4.1](#641-receiver-mapping) gives the precedence and the disambiguation by parameter count. An instance `E` in neither family is refused. R3, R4 and an instance R0 require `IsThisAvailable`. `E.IsReadOnly` must be true when the host is a readonly struct member under R3 or R4, and when the target is readonly under R2.
- E6 (`ArityMismatch`). `E.Arity` is 0, or it equals the total arity of the slots, in slot order (RC29).
- E7 (`UnboundParameter`). Every required parameter of `E` has a source after the canonical binding and the explicit bindings (section [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder)). The message lists the unbound parameters and the names of the parameters of the site. An optional parameter without a source receives the default of `E`, and a caller-information parameter without an explicit binding receives the value materialized from the source call site (section [6.7](#67-caller-information-materialization)). A synthesized signature always binds every parameter: the target parameters to their arguments, and the added parameters to their mandatory sources. Earlier versions named this rule `ParameterCountMismatch` and bound by position.
- E8 (`ReceiverParameterMismatch`). For the parameter bound to `InterceptorArgument.Receiver`, by passing mode: `Ref` requires `RefKind.Ref` and an identical type; `In` accepts `in` with an identical type, or by value with an implicit conversion. It accepts `ref readonly` with an identical type only when the receiver is a variable, and the rewrite then uses `FirstArgumentByIn`. An argument passed without a modifier to a `ref readonly` parameter produces warning CS9192, or CS9193 for a value (RC `Errors\ErrorCode.cs:2260-2261`); `Value` requires by value with an implicit conversion (identity, reference or boxing). Conditional access additionally requires `E.IsExtensionMethod` and extension methods of `E`'s type in scope (R1x), or an instance `E` under R2. Under R2 and R4, `E` has no receiver parameter, and this rule does not apply.
- E9 (`RefKindMismatch`, `TypeNotIdentical`). For a callee parameter with a `RefKind` other than `None`, the `RefKind` is equal and the types are identical (`compilation.ClassifyConversion(Ti, Qi).IsIdentity`, RC `CSharpExtensions.cs:438`).
- E10 (`NoImplicitConversion`, `TargetTypedArgumentRequiresIdentity`). For a by-value parameter, `ClassifyConversion(Ti, Qi).IsImplicit` must hold. When `Qi` is not identical to `Ti`: a target-typed argument is refused; otherwise, if the argument's natural type is not identical to `Ti`, the rewrite inserts `(Ti)(argument)`.
- E11 (`ParamsRequired`). When the call site uses the expanded form of a `params` parameter, the matching parameter of `E` is `params` with an identical type. When the call site uses the normal form, `M( array )`, the parameter of `E` need not be `params`: E10 applies to the array argument (rules table row 68).
- E12 (`NamedArgumentMismatch`). Every named argument of the rewritten call, original, appended or added, names the parameter of `E` that its binding selects, after the renaming of section [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder) for a synthesized signature. The name of an added argument is unique against the parameters of the target (section [5.6.9](05b-api-providers-contexts-results.md#569-added-parameters-and-pulled-values)).
- E13 (`ScopedMismatch`). For each parameter where the callee has an explicit scope (section [6.2.3](06a-call-site-model.md#623-limitations)), `E` has the same scope. Speculative binding does not run ref-safety analysis, so this check is structural.
- E14 (`ReturnTypeMismatch`, `VoidMismatch`). A void target requires a void `E`. A non-void target requires `ClassifyConversion(E.ReturnType, R).IsImplicit`. When the conversion is not an identity and the value of the call is used (`IsResultUsed`), the rewrite wraps the call in `(R)(...)`. A cast is not a valid statement expression (CS0201). No cast is therefore added when the invocation is an expression statement, a `for` initializer or iterator, or the expression body of a member, lambda or local function that returns `void`. A cast cannot be placed inside a `?.` chain, so a call site inside a conditional access requires an identical return type, and any other return type is refused with `ReturnTypeMismatch`.
- E15 (`NotAccessible`). `IsSymbolAccessibleWithin(E, callingType, throughType)`, with `throughType` the calling type under R0, R1, R1x, R3 and R4, and the receiver type under R2 (condition R2b). For a synthesized signature, the check uses the accessibility that the `configure` function requested, or the computed one (section [8.7](08-deduplication-and-naming.md#87-names-and-accessibility)).
- E16 (`DoesNotBind(reason)`). The rewritten invocation, built exactly as the rewrite plan builds it with its argument plan, including the pulled expressions, the caller information and `this` for `CallerInstance`, is bound speculatively at the call-site position with `semanticModel.GetSpeculativeSymbolInfo(position, expression, SpeculativeBindingOption.BindAsExpression)` (RC `CSharpExtensions.cs:595`). The result must be `E` (constructed as planned) with `CandidateReason.None`. This catches overloads of `E`, constraint violations and inference failures. A synthesized method does not exist in the source compilation, so the engine binds a stand-in instead: a block statement that declares a local function with the adjusted parameter list, type parameters and constraints, followed by the rewritten call with the local function as callee, through `SemanticModel.TryGetSpeculativeSemanticModel( position, statement, out model )` (RC `CSharpExtensions.cs:1212`). The final name of a synthesized method is unique at the call site (section [8.7](08-deduplication-and-naming.md#87-names-and-accessibility)), so its binding reduces to the applicability that the stand-in reproduces: conversions, named arguments, `params` expansion, inference and constraints. The stand-in runs only when the `configure` function changed a type, a name or the parameter list, because the derived signature is correct by construction. Statement speculation needs a position inside a body. At a call site in a field, property or event initializer, a constructor initializer or primary-constructor base arguments, the adjusted signature is checked with E9 to E14 only, and the compiler reports any remaining error on the generated code ([Appendix B](appendix-b-weak-spots.md#appendix-b-weak-spots-that-remain-after-the-review)).
- E17 (`SelfInterception`, reported as LAMA1018, existing methods only). The call site is not inside the body of `E`, or of a method of the current compilation that overrides `E` directly or indirectly, including their lambdas and local functions. An instance `E` is called with virtual dispatch, so a call site in an override of `E` would call that override.
- E18 (`NotMethodGroupConvertible`, method-reference sites only). After the receiver mapping, `E` has exactly the parameters of the callee in static form, with identical types, reference kinds, default values, `params` and scopes, and an identical return type with the same reference kind (section [6.4.12](#6412-method-reference-sites)). E7 to E14 are replaced by this rule at a method-reference site. The rewritten method group, built as the rewrite plan builds it, is then bound speculatively. For a site whose converted type `D` can be named, the engine binds `(D) X.E` with `GetSpeculativeSymbolInfo` at the site and requires `E` with `CandidateReason.None`. For a site whose converted type is an anonymous delegate type, the engine binds the statement `var d = X.E;` through `TryGetSpeculativeSemanticModel` and requires the type of `d` to be equal to the converted type of the site. This catches overloads of `E` that make the method group ambiguous, which the natural function type rejects. For a function-pointer site, `E` must also be static and must not be called in reduced form.
- E19 (`AccessorShapeMismatch`, accessor sites only). After the receiver mapping, `E` has the parameters of the accessor shape of section [6.4.13](#6413-accessor-sites): none for a getter, the value for a setter, and the handler for an add or remove accessor, followed by optional trailing parameters as in E7. The value and the handler follow E10, and the getter follows E14. A setter whose return type is `void` is admissible only at a site whose value is not used (`IsResultUsed` false); a setter whose return type is implicitly convertible to the property type is admissible at every site, and the rewrite casts its result when the value is used and the return type differs (E14). An add or remove interceptor can return any type, because the value of `+=` and `-=` on an event is `void` and is never used. At a compound site, E19 is checked for each accessor use, and the computed value of the setter must convert implicitly to the value parameter. E7 to E14 are replaced by this rule at an accessor site, except where this rule names them.
- E20 (`PulledParameterNotAccessible`). A value pulled with `PullAction.UseExistingParameter` names a parameter of the origin, and that name, bound at the site, resolves to this parameter: no parameter of an enclosing lambda, anonymous method or local function shadows it, no static lambda or static local function lies between it and the site, and it is not a `ref`, `out` or `in` parameter that an enclosing lambda, anonymous method or local function would capture (section [5.6.9](05b-api-providers-contexts-results.md#569-added-parameters-and-pulled-values)). E16 alone would accept a shadowing parameter of the same type.
- E21 (`CallerInstanceNotAvailable`). A parameter bound to `InterceptorArgument.CallerInstance` requires `IsThisAvailable` at the site, and its type accepts the calling type through an implicit conversion. A `ref` parameter requires the calling struct type and a member that is not `readonly` (section [5.6.10](05b-api-providers-contexts-results.md#5610-the-callers-instance-and-method-reference-sites)).

Omitted arguments are always materialized for existing methods, whatever the target's default mode. `E` therefore receives the target's defaults for the parameters that are bound to arguments of the target, and its own defaults only for the optional parameters that no source binds (E7). Caller information is materialized as in section [6.7](#67-caller-information-materialization). A synthesized signature follows its `DefaultMode` (section [6.4.4](#644-defaults-and-params)).

```csharp
/// <summary>
/// Validates an interceptor signature against a call site, a method-reference site or an accessor site with the rules E1 to E21. The same rules serve existing methods
/// and synthesized signatures after their configure function, so that R9 has one implementation.
/// </summary>
internal static class InterceptorSignatureValidator
{
    /// <summary>
    /// Validates the existing <paramref name="method"/> as an interceptor of <paramref name="callSite"/> and returns the
    /// rewrite plan, or the first incompatibility.
    /// </summary>
    public static InterceptorSignatureValidation ValidateExisting(
        InvocationCallSite callSite,
        IMethodSymbol method,
        ParameterBindingPlan binding,                // The canonical binding completed by the bind function.
        SemanticModel semanticModel,
        CancellationToken cancellationToken );

    /// <summary>
    /// Validates a synthesized <paramref name="signature"/>, after the adjustments of its configure function, as an
    /// interceptor of <paramref name="callSite"/> placed in <paramref name="placement"/>. Rules E1 to E4 and E17 are skipped.
    /// </summary>
    public static InterceptorSignatureValidation ValidateSynthesized(
        InvocationCallSite callSite,
        InterceptorSignature signature,
        ParameterBindingPlan binding,                // The bindings and the sources of the added parameters at this site.
        AdmissiblePlacement placement,
        SemanticModel semanticModel,
        CancellationToken cancellationToken );
}

/// <summary>The source of each interceptor parameter at one site, as the ArgumentPlanBuilder resolved it.</summary>
internal sealed record ParameterBindingPlan( ImmutableArray<InterceptorArgument?> Sources, bool IsCanonical );

internal readonly record struct InterceptorSignatureValidation( InvocationRewritePlan? Plan, InterceptorSignatureIncompatibility? Incompatibility );
```

`ValidateExisting` and `ValidateSynthesized` have overloads that take a `MethodReferenceSite` (section [6.2.10](06a-call-site-model.md#6210-method-reference-sites)). These overloads apply E18 instead of E7 to E14, and they return a `MethodReferenceRewritePlan`, which holds the shape of section [6.4.12](#6412-method-reference-sites). Overloads that take an `AccessorSite` apply E19, once per accessor use, and return an `AccessorRewritePlan`, which holds the rewrite form of section [6.4.13](#6413-accessor-sites).

Existing methods need no synthesis and have no deduplication key. The one-interceptor-per-call-site rule still applies. For a synthesized signature, a failure is reported at the call site, and the call site does not join a group.

### 6.7 Caller-information materialization

#### 6.7.1 Rule

For every caller-information parameter whose argument is omitted at the source call, the engine appends a named argument whose value is the literal that Roslyn bound at the source call. The value comes from the `IArgumentOperation` of kind `DefaultValue` in the source operation, which Roslyn computes in `bindDefaultArgument` (RC `Binder\Binder_Invocation.cs:1693-1790`).

Caller information is also a binding source, `InterceptorArgument.CallerInfo( kind, argumentOf )` (section [5.6.8](05b-api-providers-contexts-results.md#568-parameter-binding-and-the-signature-builder)). A parameter bound to it, whether a parameter of an existing method (E7), of an existing await interceptor (section [7.10.1](07-await-interception.md#7101-existing-method-interceptors)), or a parameter that a `configure` function added (section [5.6.9](05b-api-providers-contexts-results.md#569-added-parameters-and-pulled-values)), has no `IArgumentOperation` in the source operation. A caller-information parameter of an existing method binds to this source canonically, from its attribute. The engine computes the value at the source call site with the rules of section [6.7.2](#672-values-per-context-existing-roslyn-rules-used-as-test-oracles). Line and path use the token that `GetCallerLocation` selects (RC `Binder\Binder_Invocation.cs:1438-1451`), or the `await` keyword for an await, together with `SyntaxTree.GetDisplayLineNumber` and `SyntaxTree.GetDisplayPath( span, compilation.Options.SourceReferenceResolver )`. The member name follows `GetMemberCallerName` applied to the origin. `CallerArgumentExpression` uses the source text of an argument of the site: the argument for the parameter of the intercepted method that `argumentOf` names, or, for a canonical binding from the attribute, the argument bound to the parameter of the existing method that the attribute names.

This gives the exact values of the source compilation: the correct member name when the linker later moves the body to an `M_Source` member, the original path when `LamaDebug` replaces file paths, the original line when the formatted output shifts lines, and the original argument text when a nested argument is intercepted. The interceptor parameters carry no caller attributes, and the proceed call passes the parameters explicitly, so the target receives the source values.

`ArgumentValueMaterializer` supports these shapes, with or without an implicit conversion around them:

- a constant, emitted as a literal, with a cast for `byte`, `sbyte`, `short`, `ushort` and enumeration types, and as `new global::System.DateTime(ticks)` for `DateTime`;
- `IDefaultValueOperation`, emitted as `default(T)`;
- a static `IFieldReferenceOperation`, emitted as a qualified field access (`global::System.Type.Missing`);
- an `IObjectCreationOperation` of `UnknownWrapper` or `DispatchWrapper` with a null argument.

The same materializer produces the defaults of the Materialized mode. The appended arguments reach the linker as `CallSiteExtraArgument` values (section [10.4.5](10a-oss-bridge-hook-factory.md#1045-call-site-redirections)).

A method-reference site has no argument list, and the code that invokes the delegate passes every argument, including the caller-information parameters. No value is materialized for it. The interceptor keeps the default values of these parameters, without the caller attributes, so the natural function type is unchanged (section [6.4.12](#6412-method-reference-sites)).

#### 6.7.2 Values per context (EXISTING Roslyn rules, used as test oracles)

| Parameter | Value | Evidence |
|---|---|---|
| `CallerLineNumber` | The line of the opening parenthesis of the argument list, after `#line` mapping. | RC `Binder\Binder_Invocation.cs:1438-1451`; `..\Core\Portable\Syntax\SyntaxTree.cs:320-324` |
| `CallerFilePath` | The mapped path, normalized with the compilation's `SourceReferenceResolver`, which applies the path map. | RC `..\Core\Portable\Syntax\SyntaxTree.cs:285-294`; `..\Core\Portable\SourceFileResolver.cs:96-100` |
| `CallerMemberName` in a method, operator or conversion | The method name. An operator gives its metadata name (for example `op_Addition`). | RC `Symbols\MemberSymbolExtensions.cs:940-950` |
| in a constructor, static constructor, destructor | `.ctor`, `.cctor`, `Finalize`. | same |
| in a property, indexer or event accessor | The property or event name; `Item` or the `[IndexerName]` name for indexers. | same |
| in an explicit interface implementation | The name without the interface prefix. | same |
| in a field or event-field initializer | The field or event name. | RC `Binder\Binder_Invocation.cs:1612-1621` |
| in a property initializer | The property name. | same |
| in a lambda or local function | The enclosing non-lambda member. | RC `Symbols\SymbolExtensions.cs:146-156` |
| in a constructor initializer, primary-constructor base arguments | `.ctor`. | same |
| in top-level statements | `<Main>$`. | same |
| `CallerArgumentExpression` | `argument.Syntax.ToString()` of the source argument, including the receiver of a reduced classic extension call and of an extension member. | RC `Binder\Binder_Invocation.cs:1733-1740, 1830-1856` |

An argument that the user passed explicitly is kept as written.

#### 6.7.3 Interplay with the existing linker substitution

EXISTING: the linker appends `[CallerMemberName]` arguments for calls from overridden source bodies (ENG26 `Linking\Substitution\CallerMemberSubstitution.cs:17-60`; `Linking\LinkerAnalysisStep.cs:1151-1265`). After materialization, a rewritten call has no omitted caller parameter. The injection step rewrites the call before the analysis step searches the intermediate compilation (RC39), so the analysis step sees the rewritten call and never registers a `CallerMemberSubstitution` for it. No skip rule is needed (section [10.5.8](10b-oss-linker-and-templates.md#1058-interaction-with-callermembersubstitution)).
