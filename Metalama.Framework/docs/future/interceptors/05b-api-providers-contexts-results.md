# User-facing API: providers, contexts, results and binding

> Part of the [call-site interceptors design](README.md). Previous: [05a-api-registration.md](05a-api-registration.md) | Next: [05c-api-templates.md](05c-api-templates.md). Evidence prefixes and terms: [00-conventions.md](00-conventions.md).

### 5.4 Interceptor provider interfaces

An interceptor provider is the user object that chooses the interceptor of each call site. The interceptor itself is the method that the rewritten call site calls (section [0.4](00-conventions.md#04-terminology-aligned-with-roslyn-interceptors)).

```csharp
namespace Metalama.Extensions.Interceptors;

/// <summary>
/// Provides the interceptor of each use of the selected methods or accessors in its scope: the method that the rewritten
/// site calls or that the rewritten method reference references, or no interceptor. The same interface serves
/// <c>InterceptMethods</c> and <c>InterceptAccessors</c>.
/// </summary>
/// <remarks>
/// <para>
/// Metalama calls <see cref="GetInterceptor"/> during the build. When the IDE option of decision PO26 is enabled, it also
/// calls it in the IDE. Calls can be concurrent, and the same call site can be evaluated several times. The provider must
/// be thread-safe and deterministic, and it must not modify its own state.
/// </para>
/// <para>
/// In the IDE, the provider is kept across compilations. It must therefore not store declarations, types, or objects that
/// reference them. Store an <see cref="IDurableRef{T}"/> instead. An analyzer verifies this rule for every provider.
/// </para>
/// <para>
/// The provider can report diagnostics through <see cref="InterceptionContext.Diagnostics"/>, and it can decline a call
/// site by returning <see cref="InterceptorResult.Skip"/>. An exception thrown by the provider is reported as an error at
/// the call site, and the call site is not intercepted.
/// </para>
/// </remarks>
/// <seealso href="@intercepting-call-sites"/>
[CompileTime]
[PublicAPI]
[Durable]
[ImmutableType]
public interface IMethodInterceptorProvider
{
    /// <summary>Returns the interceptor to use for an invocation, or <see cref="InterceptorResult.Skip"/>.</summary>
    InterceptorResult GetInterceptor( MethodInterceptionContext context );
}

/// <summary>
/// Provides the interceptor of each await expression in its scope, or no interceptor.
/// </summary>
/// <remarks>The rules of <see cref="IMethodInterceptorProvider"/> apply.</remarks>
[CompileTime]
[PublicAPI]
[Durable]
[ImmutableType]
public interface IAwaitInterceptorProvider
{
    /// <summary>Returns the interceptor to use for an await expression, or <see cref="InterceptorResult.Skip"/>.</summary>
    InterceptorResult GetInterceptor( AwaitInterceptionContext context );
}
```

| Choice | Reason and precedent |
|---|---|
| Interfaces, not abstract classes | R8 asks for an interface. One class can implement both kinds. Each interface has one method; everything that can grow lives in the sealed context and result classes. |
| `[Durable]` | Fabric registrations live in the pipeline configuration, and design-time registrations are kept across compilations. `IAspect` (FW27 `Aspects\IAspect.cs:46`), `IAspectState` and `Fabric` carry the same attribute. On an interface, the analyzer verifies every implementation (FW27 analyzers, LAMA0870 and LAMA0876). |
| `[ImmutableType]` | The existing analyzer enforces the thread-safety contract, as for `IAspect` and `Fabric`. |
| No `ICompileTimeSerializable` | Serialization is not required. Registrations are project-local and are never written to a manifest in version 1 (section [9.9.4](09-premium-engine.md#994-deferred-route-manifest-based-transitive-interceptors)). Manifest-based transitive interceptors stay deferred. If they are ever needed, they get a separate opt-in entry point that takes a serializable provider, for example `ISerializableMethodInterceptorProvider : IMethodInterceptorProvider, ICompileTimeSerializable`. This is an addition, not a breaking change. A declaring type and names can be serialized, which keeps that path open; a type predicate cannot, so such an entry point would accept only the type and names. An earlier version of this design required the interface on every provider, as `BaseReferenceValidator` does (P27 `Metalama.Extensions.Validation\BaseReferenceValidator.cs:15`). |
| One method name, `GetInterceptor` | The name states what the method returns, as in the Roslyn vocabulary (section [0.4](00-conventions.md#04-terminology-aligned-with-roslyn-interceptors)). A class that implements both interfaces has two overloads that differ by the context type. A method-group conversion to `Func<MethodInterceptionContext, InterceptorResult>` or to `Func<AwaitInterceptionContext, InterceptorResult>` selects the overload by its parameter type. An earlier version used two names, `InterceptMethod` and `InterceptAwait` (RC41). |
| Interfaces that stay open to handlers | A future delegate-based interceptor (section [16.2](16-future-directions.md#162-delegate-based-handler-interceptors)) is a new factory of `InterceptorResult`, not a new provider interface. The provider interfaces therefore do not change when it is added. |

### 5.5 Interception contexts

#### 5.5.1 InterceptionContext

```csharp
namespace Metalama.Extensions.Interceptors;

/// <summary>
/// The base class of the objects passed to <see cref="IMethodInterceptorProvider.GetInterceptor"/> and
/// <see cref="IAwaitInterceptorProvider.GetInterceptor"/>. It describes one call site.
/// </summary>
/// <remarks>
/// <para>
/// Declarations exposed by this class belong to the source compilation, before any aspect transformation. The aspect
/// repository of the compilation is the final one, so <c>Enhancements().HasAspect</c> reports the aspects of the whole
/// pipeline. Declarations introduced by aspects are not visible.
/// </para>
/// <para>
/// All information in this class is specific to one call site. It is not available to templates, because a generated
/// interceptor method can be shared by several call sites. To pass call-site information to a template, pass it as a
/// template argument. Template arguments are part of the grouping identity (see <see cref="InterceptorResult"/>).
/// </para>
/// <para>This object must not be stored after <c>GetInterceptor</c> returns.</para>
/// <para>
/// Source expressions exposed by the derived classes, such as the receiver, the arguments and the awaited operand, are
/// available for inspection only. They cannot be emitted in generated code, because the call site already evaluates them.
/// </para>
/// </remarks>
[CompileTime]
[PublicAPI]
public abstract class InterceptionContext
{
    internal InterceptionContext() { }

    /// <summary>Gets the declaration selected at registration whose scope contains the call site.</summary>
    public abstract IDeclaration ScopeDeclaration { get; }

    /// <summary>
    /// Gets the origin of the site: the innermost declaration of the code model that contains the site. It is a member, an
    /// accessor, the field or property of an initializer, the entry point for top-level statements, or the type for
    /// primary-constructor base arguments. Lambdas and local functions are attributed to their enclosing member.
    /// </summary>
    /// <remarks>
    /// The origin is an <see cref="IMember"/> except for primary-constructor base arguments, where it is the
    /// <see cref="INamedType"/>. The term has the meaning of <c>ReferenceEndRole.Origin</c> in reference validation.
    /// </remarks>
    public abstract IDeclaration Origin { get; }

    /// <summary>
    /// Gets the innermost type that contains the site: <see cref="Origin"/> itself when it is a type, and otherwise the
    /// declaring type of <see cref="Origin"/>.
    /// </summary>
    public abstract INamedType CallingType { get; }

    /// <summary>Gets the namespace of <see cref="CallingType"/>.</summary>
    public abstract INamespace CallingNamespace { get; }

    /// <summary>Gets the kind of code that immediately contains the call site.</summary>
    public abstract EnclosingCodeKind EnclosingCodeKind { get; }

    /// <summary>Gets a value indicating whether the innermost enclosing method, local function or lambda is <c>async</c>.</summary>
    public abstract bool IsInAsyncFunction { get; }

    /// <summary>
    /// Gets a value indicating whether the site is inside a lambda, an anonymous method or a local function of the origin.
    /// </summary>
    /// <remarks>
    /// A value pulled with <see cref="InterceptorArgument.Pull"/> refers to the parameters of <see cref="Origin"/>, never
    /// to the parameters of an enclosing lambda or local function. A provider that pulls a parameter can skip the sites
    /// for which this property is <c>true</c>, because a parameter of the enclosing function can shadow the pulled
    /// parameter, and the site is then refused (section 5.6.9 of the design).
    /// </remarks>
    public abstract bool IsInNestedFunction { get; }

    /// <summary>
    /// Gets a value indicating whether code at the call site can reference <c>this</c>. The value is <c>false</c> in static
    /// members, static lambdas, static local functions, field initializers, constructor initializers, and lambdas or local
    /// functions of struct members.
    /// </summary>
    public abstract bool CanAccessThis { get; }

    /// <summary>
    /// Gets the source of the site: the invocation expression, the method group, the member access of an accessor site, or
    /// the await expression.
    /// </summary>
    public abstract SourceReference Source { get; }

    /// <summary>
    /// Gets the default location of diagnostics: the name of the invoked method or of the accessed property or event, and the
    /// <c>await</c> keyword for await expressions.
    /// </summary>
    public abstract IDiagnosticLocation DiagnosticLocation { get; }

    /// <summary>
    /// Gets a sink that reports diagnostics at <see cref="DiagnosticLocation"/> and suppresses diagnostics in
    /// <see cref="Origin"/>. Diagnostics are attributed to the aspect or fabric that registered the interceptor.
    /// </summary>
    public abstract ScopedDiagnosticSink Diagnostics { get; }

    /// <summary>
    /// Gets the state of the aspect instance that registered the interceptor, as it is when <see cref="IAspect{T}.BuildAspect"/>
    /// exits, or <c>null</c> when a fabric registered the interceptor.
    /// </summary>
    public abstract IAspectState? AspectState { get; }

    /// <summary>
    /// Gets the reason why the call site cannot be intercepted, or <c>null</c> when it can be intercepted. When this property
    /// is not <c>null</c>, any result other than <see cref="InterceptorResult.Skip"/> produces a warning and the call site is
    /// left unchanged.
    /// </summary>
    public abstract NonInterceptableReason? NonInterceptableReason { get; }

    /// <summary>Gets a cancellation token that the provider should pass to long operations.</summary>
    public abstract CancellationToken CancellationToken { get; }

    /// <summary>
    /// Determines whether an interceptor generated in a given placement can intercept this call site. The check covers the
    /// availability of <c>this</c>, the accessibility of the intercepted method and of the types of its signature, the type
    /// parameters of the calling context, conditional access, and the receiver-mapping rules, including the rules for
    /// <c>base</c> calls. It checks the default signature, without the adjustments of an <see cref="IInterceptorBuilder"/>.
    /// </summary>
    /// <param name="reason">A sentence that explains why the placement is not supported, or <c>null</c>.</param>
    public abstract bool SupportsPlacement( InterceptorPlacement placement, out string? reason );

    /// <summary>Determines whether an interceptor generated in a given placement can intercept this call site.</summary>
    public bool SupportsPlacement( InterceptorPlacement placement ) => this.SupportsPlacement( placement, out _ );
}

/// <summary>Kinds of code that immediately contain a call site.</summary>
[CompileTime]
public enum EnclosingCodeKind
{
    MemberBody,
    Lambda,
    LocalFunction,
    Initializer,
    ConstructorInitializer,
    PrimaryConstructorBaseArguments,
    TopLevelStatements
}

/// <summary>Reasons why a call site that matches a registration cannot be intercepted in this version.</summary>
[CompileTime]
public enum NonInterceptableReason
{
    /// <summary>The intercepted method returns by reference.</summary>
    RefReturn,

    /// <summary>The signature needs <c>scoped</c> or <c>[UnscopedRef]</c>, which generated methods cannot declare in this version.</summary>
    ScopedParameter,

    /// <summary>The signature contains a pointer or a function pointer.</summary>
    PointerType,

    /// <summary>The intercepted method uses <c>__arglist</c>.</summary>
    VariableArguments,

    /// <summary>An omitted optional argument has a default value that cannot be written in C#.</summary>
    UnsupportedDefaultValue,

    /// <summary>The signature contains a type that no placement can name, such as an anonymous type outside a type argument.</summary>
    UnnameableType,

    /// <summary>The receiver is an array element of a type parameter type that could be passed only by reference.</summary>
    CovariantArrayElementReceiver,

    /// <summary>The awaited type or the result of an await expression is a ref-like type.</summary>
    RefLikeAwait,

    /// <summary>A generated method would need a language feature that the project language version does not have.</summary>
    RequiresNewerLanguageVersion,

    /// <summary>
    /// The call is in a conditional access, and its receiver is a type parameter that is not known to be a reference type
    /// and that would have to be passed by reference.
    /// </summary>
    ConditionalAccessMutableReceiver,

    /// <summary>
    /// The receiver is a type parameter that is not known to be a value type, and an argument of the call can assign the
    /// receiver variable.
    /// </summary>
    ReceiverReassignedByArguments,

    /// <summary>The call is in the initializer of a field that an aspect promotes to a property.</summary>
    PromotedFieldInitializer,

    /// <summary>The method-reference site needs the default values of the target, which an interceptor cannot declare.</summary>
    MethodReferenceRequiresMaterializedDefaults,

    /// <summary>The receiver of the method-reference site can stay neither in a method group nor in a wrapper.</summary>
    MethodReferenceReceiverNotSupported,

    /// <summary>
    /// The method-reference site is the handler of <c>+=</c> or <c>-=</c> on an event, and the result would need a
    /// wrapper, which creates a delegate that no other delegate equals, so <c>-=</c> could not remove the handler.
    /// </summary>
    DelegateEqualityRequired,

    /// <summary>The property is set in an object initializer or in the initializer of a <c>with</c> expression, which cannot call a method.</summary>
    ObjectOrWithInitializer,

    /// <summary>The property is a target of a deconstruction assignment.</summary>
    DeconstructionTarget,

    /// <summary>The setter is an <c>init</c> accessor, which only a constructor or an <c>init</c> accessor can call.</summary>
    InitOnlySetter,

    /// <summary>The site uses a C# 14 user-defined instance compound assignment operator or instance increment operator.</summary>
    InstanceCompoundOperator,

    /// <summary>
    /// The compound site needs a temporary for its receiver that C# cannot declare at the site, for example a
    /// <c>ref</c> temporary for a struct receiver in an expression.
    /// </summary>
    ReceiverTemporaryNotPossible
}
```

Call sites with a limitation are presented to the interceptor (RC5). Method groups converted to a delegate or to a function pointer are presented as method-reference sites (section [5.3.11](05a-api-registration.md#5311-kinds-of-method-use)). Uses of property and event accessors are presented as accessor sites (section [5.3.13](05a-api-registration.md#5313-accessors), [6.2.11](06a-call-site-model.md#6211-accessor-sites)). Call sites that are not calls in any observable sense are never presented: calls and method groups in expression trees (including query expressions over `IQueryable`), `nameof`, method groups that are not converted, delegate invocations, local-function calls, function-pointer invocations, dynamic invocations, dynamic awaits, calls that the compiler omits (`[Conditional]` without the symbol, partial methods without implementation), calls that do not bind, calls in compile-time code, and `await foreach` and `await using`. Section [6.2.2](06a-call-site-model.md#622-silent-refusals) gives the detection rules.

#### 5.5.2 MethodInterceptionContext and InvocationArgument

```csharp
/// <summary>
/// Describes one use of a method that a method registration selects, a call or a method group converted to a delegate
/// or to a function pointer, or one use of an accessor that an accessor registration selects.
/// </summary>
/// <remarks>
/// One context type serves both member verbs. The members that describe an accessor site, <see cref="AssignmentOperator"/>,
/// <see cref="IsPostfix"/> and <see cref="IsChecked"/>, have the values <see cref="OperatorKind.None"/>, <c>false</c> and
/// <c>false</c> for a use of an ordinary method.
/// </remarks>
[CompileTime]
[PublicAPI]
public abstract class MethodInterceptionContext : InterceptionContext
{
    internal MethodInterceptionContext() { }

    /// <summary>
    /// Gets the kind of use at the site. Every registration receives every kind. A provider that does not support a kind
    /// returns <see cref="InterceptorResult.Skip"/>.
    /// </summary>
    /// <remarks>
    /// Later versions can add values to <see cref="MethodUseKind"/>. A provider that depends on the kind tests for the
    /// kinds that it supports and skips the others.
    /// </remarks>
    public abstract MethodUseKind Kind { get; }

    /// <summary>
    /// Gets the delegate type or the function pointer type to which the method group is converted, or <c>null</c> for a
    /// call. For <c>var d = M;</c>, it is the natural function type of the method group.
    /// </summary>
    public abstract IType? ConvertedType { get; }

    /// <summary>
    /// Gets a value indicating whether the method group is the right operand of <c>+=</c> or <c>-=</c> on an event. The
    /// rewrite changes the identity of the handler, so a handler added in the scope is not removed by code outside the
    /// scope, and the reverse.
    /// </summary>
    public abstract bool IsEventSubscription { get; }

    /// <summary>
    /// Gets the method to which the C# compiler binds the call, or that the method-group conversion selects, constructed
    /// with the type arguments of the site. At an accessor site, it is the accessor: the getter, the setter, or the add or
    /// remove accessor.
    /// A classic extension method called in reduced form is represented in its static form, and its receiver is the first
    /// element of <see cref="Arguments"/>.
    /// </summary>
    public abstract IMethod InterceptedMethod { get; }

    /// <summary>
    /// Gets the destination of the site: the definition of the member that the site uses. For a use of a method, it is the
    /// definition of <see cref="InterceptedMethod"/>. At an accessor site, it is the definition of the property or the
    /// event whose accessor the site calls. It can be an override or an implementation of the member that the registration
    /// selected, according to <see cref="MethodInterceptionOptions.Matching"/>.
    /// </summary>
    /// <remarks>
    /// The term has the meaning of <c>ReferenceEndRole.Destination</c> in reference validation, where a property access also
    /// references the property. The accessor itself is <see cref="InterceptedMethod"/>, constructed with the type
    /// arguments of the site.
    /// </remarks>
    public abstract IMember Destination { get; }

    /// <summary>
    /// Gets the method or accessor definition that the registration matched by its declaring type and its name. It differs
    /// from the definition of <see cref="InterceptedMethod"/>, and from <see cref="Destination"/>, when the site is bound to an
    /// override or to an interface implementation, according to <see cref="MethodInterceptionOptions.Matching"/>.
    /// </summary>
    public abstract IMethod MatchedMethod { get; }

    /// <summary>
    /// Gets the assignment operator of an accessor site: <see cref="OperatorKind.None"/> for a plain read, a plain write,
    /// an event subscription and a method use; the compound kind, such as <see cref="OperatorKind.AdditionAssignment"/>
    /// for <c>r.P += v</c>; <see cref="OperatorKind.Increment"/> or <see cref="OperatorKind.Decrement"/> for <c>++</c>
    /// and <c>--</c>; and <see cref="OperatorKind.NullCoalescingAssignment"/> for <c>??=</c>. The getter use and the setter
    /// use of one compound site report the same operator.
    /// </summary>
    /// <remarks>
    /// The value is the plain kind, never a <c>Checked</c> member such as <see cref="OperatorKind.CheckedAddition"/>, which
    /// denotes the declaration of a user-defined checked operator. Read <see cref="IsChecked"/> for the checked context.
    /// For <c>+=</c> and <c>-=</c> on an event, the value is <see cref="OperatorKind.None"/>, because the accessor kind
    /// already says whether a handler is added or removed.
    /// </remarks>
    public abstract OperatorKind AssignmentOperator { get; }

    /// <summary>
    /// Gets a value indicating whether an increment or a decrement is written in postfix form, <c>r.P++</c>. It is
    /// <c>false</c> for every other site.
    /// </summary>
    public abstract bool IsPostfix { get; }

    /// <summary>
    /// Gets a value indicating whether the operator of the site performs overflow checking, which happens in a
    /// <c>checked</c> context for integral types. It is <c>false</c> when the site has no operator.
    /// </summary>
    public abstract bool IsChecked { get; }

    /// <summary>Gets how the call dispatches to the intercepted method.</summary>
    public abstract InvocationDispatchKind DispatchKind { get; }

    /// <summary>Gets the kind of receiver written at the call site.</summary>
    public abstract InvocationReceiverKind ReceiverKind { get; }

    /// <summary>Gets the static type of the receiver, or <c>null</c> when <see cref="ReceiverKind"/> is <see cref="InvocationReceiverKind.None"/>.</summary>
    public abstract IType? ReceiverType { get; }

    /// <summary>
    /// Gets the receiver expression written at the call site, for inspection only, or <c>null</c> when the intercepted
    /// method is static or the receiver is implicit. For a classic extension method called in reduced form, the receiver is
    /// the first element of <see cref="Arguments"/>, and this property is <c>null</c>. For a conditional access
    /// <c>a?.M()</c>, it is the expression <c>a</c>. For a method-reference site <c>obj.M</c>, it is <c>obj</c>, which C#
    /// evaluates when the delegate is created; for a classic extension method in reduced form, it is the receiver of the
    /// method group.
    /// </summary>
    /// <remarks>
    /// The value is an <see cref="ISourceExpression"/>. Its <see cref="IHasType.Type"/> and
    /// <see cref="ISourceExpression.AsTypedConstant"/> can be read. The Roslyn syntax is available through the SDK method
    /// <c>SourceExpressionExtensions.GetSourceSyntax</c>. The expression cannot be emitted in generated code (LAMA0297).
    /// </remarks>
    public abstract IExpression? Receiver { get; }

    /// <summary>
    /// Gets a value indicating whether the call is written with the null-conditional operator <c>?.</c>. It is always
    /// <c>false</c> for a method-reference site.
    /// </summary>
    public abstract bool IsConditionalAccess { get; }

    /// <summary>
    /// Gets a value indicating whether the value returned by the call is used. It is always <c>false</c> for a
    /// method-reference site, because the value is used by the code that invokes the delegate. At an accessor site, it
    /// tells whether the value of the source expression is used: the read, the assignment, the compound assignment or the
    /// increment. Both uses of one compound site report the same value.
    /// </summary>
    public abstract bool IsResultUsed { get; }

    /// <summary>
    /// Gets one element for each parameter of <see cref="InterceptedMethod"/>, in parameter order, including parameters
    /// that receive default values. The list is empty for a method-reference site, because the arguments are passed when
    /// the delegate or the function pointer is invoked, and for a getter. For a setter, the element is the value; for an
    /// add or remove accessor, it is the handler.
    /// </summary>
    public abstract IReadOnlyList<InvocationArgument> Arguments { get; }
}

/// <summary>Kinds of use of a method at a site.</summary>
/// <remarks>Later versions can add values (section 16.3).</remarks>
[CompileTime]
public enum MethodUseKind
{
    /// <summary>An invocation, for example <c>x.M(a)</c> or <c>E.M(x, a)</c>, or a use of an accessor, for example <c>x.P = a</c>.</summary>
    Call,

    /// <summary>
    /// A method group converted to a delegate, for example <c>list.Select( Transform )</c>, <c>new Action( M )</c>,
    /// <c>var d = M;</c> or <c>button.Click += OnClick;</c>.
    /// </summary>
    DelegateCreation,

    /// <summary>A method group converted to a function pointer, for example <c>&amp;Square</c>.</summary>
    FunctionPointer
}

/// <summary>Describes the argument passed to one parameter at a call site.</summary>
[CompileTime]
[PublicAPI]
public readonly struct InvocationArgument
{
    internal InvocationArgument( IParameter parameter, InvocationArgumentKind kind, IType? type, TypedConstant? constantValue, SourceReference? source, IExpression? expression );

    /// <summary>Gets the parameter of the intercepted method.</summary>
    public IParameter Parameter { get; }

    /// <summary>Gets whether the argument is written explicitly, is a default value, or is an expanded params collection.</summary>
    public InvocationArgumentKind Kind { get; }

    /// <summary>
    /// Gets the static type of the argument expression, or <c>null</c> when the expression has no type (for example a
    /// <c>null</c> literal, a lambda, a <c>default</c> literal or a collection expression).
    /// </summary>
    public IType? Type { get; }

    /// <summary>Gets the value of the argument when it is a compile-time constant.</summary>
    public TypedConstant? ConstantValue { get; }

    /// <summary>Gets the source of the argument, or <c>null</c> when the argument is not written explicitly.</summary>
    public SourceReference? Source { get; }

    /// <summary>
    /// Gets the argument expression written at the call site, for inspection only. The value is <c>null</c> when the
    /// argument is omitted (<see cref="InvocationArgumentKind.DefaultValue"/>) and when the compiler materializes it from
    /// several expressions (<see cref="InvocationArgumentKind.ParamsElements"/>). For a <c>ref</c>, <c>out</c> or <c>in</c>
    /// argument, it is the expression after the modifier.
    /// </summary>
    /// <remarks>The rules of <see cref="MethodInterceptionContext.Receiver"/> apply.</remarks>
    public IExpression? Expression { get; }
}

/// <summary>Kinds of argument passed to one parameter at a call site.</summary>
[CompileTime]
public enum InvocationArgumentKind
{
    Explicit,
    DefaultValue,
    ParamsElements,

    /// <summary>
    /// The value of a setter at a compound assignment, an increment, a decrement or a <c>??=</c>, which the rewrite
    /// computes from the value that the getter returns. <see cref="InvocationArgument.Expression"/> is the right operand,
    /// or <c>null</c> for an increment or a decrement.
    /// </summary>
    Computed
}

/// <summary>Kinds of dispatch of an intercepted call.</summary>
[CompileTime]
public enum InvocationDispatchKind
{
    /// <summary>A static method, including classic extension methods and static extension members.</summary>
    Static,

    /// <summary>An instance method called on a receiver. Virtual and interface dispatch is preserved by <c>meta.Proceed()</c>.</summary>
    Instance,

    /// <summary>A non-virtual call written <c>base.M()</c> to a virtual method. Only an instance interceptor in the calling type or a local function can intercept it.</summary>
    Base,

    /// <summary>A static abstract or static virtual interface member called through a type parameter, written <c>T.M()</c>.</summary>
    StaticThroughTypeParameter
}

/// <summary>Kinds of receiver written at a call site.</summary>
[CompileTime]
public enum InvocationReceiverKind
{
    /// <summary>No receiver: the method is static.</summary>
    None,

    /// <summary>An implicit or explicit <c>this</c>.</summary>
    This,

    /// <summary>The <c>base</c> keyword.</summary>
    Base,

    /// <summary>Any other expression, including the receiver of an extension method and a conditional-access receiver.</summary>
    Expression,

    /// <summary>A type parameter, for static abstract or static virtual members.</summary>
    TypeParameter
}
```

For a call, the data comes from `IInvocationOperation.TargetMethod`, `Instance`, `IsVirtual`, `ConstrainedToType` and `Arguments` with `ArgumentKind` (section [6.2](06a-call-site-model.md#62-the-call-site-model)). For a method-reference site, it comes from `IMethodReferenceOperation.Method`, `Instance`, `IsVirtual` and `ConstrainedToType`, and from the type of the parent `IDelegateCreationOperation` or `IAddressOfOperation` (section [6.2.10](06a-call-site-model.md#6210-method-reference-sites)). For an accessor site, it comes from the `IPropertyReferenceOperation` or `IEventReferenceOperation` and from its parent operation, which gives the kind of use, the operator, `IsPostfix` and `IsChecked` (section [6.2.11](06a-call-site-model.md#6211-accessor-sites)). `TypedConstant` is the existing public type used by `IParameter.DefaultValue` (FW27 `Code\IParameter.cs:32`).

Three members describe the called side of a site. `MatchedMethod` is the definition that the target selection of the registration selected. `Destination` is the definition of the member that the site uses, which can be an override or an implementation of it; at an accessor site, it is the property or the event. `InterceptedMethod` is the method or the accessor that the site calls, constructed with the type arguments of the site. `Origin`, on the base class, describes the calling side (section [0.3](00-conventions.md#03-terms)).

The expressions of the contexts (`MethodInterceptionContext.Receiver`, `InvocationArgument.Expression` and `AwaitInterceptionContext.Operand`) wrap source syntax in the way the code model wraps field initializers: the code model creates a `SourceUserExpression` over the source node, typed in the source compilation (ENG27 `CodeModel\Source\SourceField.cs:149`; `Templating\Expressions\SourceUserExpression.cs:18-24`). The premium engine cannot create that internal class, so it calls the open-source factory of inspection-only source expressions (section [10.6.8](10b-oss-linker-and-templates.md#1068-inspection-only-source-expressions)). The rules are:

- The expressions are for inspection. A provider can read the type, the constant value through `ISourceExpression.AsTypedConstant` (FW27 `Code\ISourceExpression.cs:31`), the text, and, through the SDK, the Roslyn syntax (section [10.9](10c-oss-reference-graph-design-time.md#109-small-public-helpers-b2g)).
- Generated code cannot contain them. Emitting one would evaluate the operand a second time, would place it outside the scope where its locals exist, and would make the template depend on one call site, which splits groups. A template result whose arguments or tags contain an expression of a context is rejected with LAMA1014 at evaluation. Any other attempt to emit such an expression during a template expansion fails with the open-source error LAMA0297.
- The code model deliberately exposes no expression tree. A provider that needs the structure of an expression uses the Roslyn syntax through the SDK.

The expressions are created lazily, on the first read of the property. A provider that does not read them pays nothing.

#### 5.5.3 AwaitInterceptionContext

```csharp
/// <summary>Describes where the calling function resumes after the original await.</summary>
[CompileTime]
public enum AwaitResumption
{
    /// <summary>The function resumes on the synchronization context or task scheduler that was current at the await.</summary>
    CapturedContext,

    /// <summary>The function resumes on the thread that completes the awaited operation, or on the thread pool.</summary>
    AnyContext,

    /// <summary>The resumption context is chosen by an awaiter whose behavior the engine does not know.</summary>
    Unknown
}

/// <summary>Mirrors the values of <c>System.Threading.Tasks.ConfigureAwaitOptions</c>, which netstandard2.0 does not have.</summary>
[CompileTime]
[Flags]
public enum AwaitConfigurationFlags
{
    None = 0,
    ContinueOnCapturedContext = 1,
    SuppressThrowing = 2,
    ForceYielding = 4
}

/// <summary>Describes a direct call to <c>ConfigureAwait</c> that produces the awaited value.</summary>
[CompileTime]
[PublicAPI]
public sealed class AwaitConfiguration
{
    internal AwaitConfiguration( IType unconfiguredType, IMethod configureAwaitMethod, AwaitConfigurationFlags? constantFlags );

    /// <summary>Gets the type on which <c>ConfigureAwait</c> is called, for example <c>Task&lt;int&gt;</c>.</summary>
    public IType UnconfiguredType { get; }

    /// <summary>Gets the <c>ConfigureAwait</c> method that is called.</summary>
    public IMethod ConfigureAwaitMethod { get; }

    /// <summary>Gets the constant argument, or <c>null</c> when the argument is not a compile-time constant.</summary>
    public AwaitConfigurationFlags? ConstantFlags { get; }
}

/// <summary>Describes one await expression in the scope of an await registration.</summary>
/// <remarks>
/// An await registration selects no awaitable type, so every await expression of the scope is presented. The provider
/// reads this context and returns <see cref="InterceptorResult.Skip"/> for the await expressions that it does not
/// intercept. The recommended first statement of a provider returns <see cref="InterceptorResult.Skip"/> when
/// <see cref="Resumption"/> is <see cref="AwaitResumption.Unknown"/>.
/// </remarks>
[CompileTime]
[PublicAPI]
public abstract class AwaitInterceptionContext : InterceptionContext
{
    internal AwaitInterceptionContext() { }

    /// <summary>
    /// Gets the type of the awaited expression, including nullable annotations, for example <c>Task&lt;int&gt;</c> or
    /// <c>ConfiguredTaskAwaitable</c>. It is the destination of the await site.
    /// </summary>
    public abstract IType AwaitableType { get; }

    /// <summary>
    /// Gets the awaited expression, for inspection only. For <c>await client.GetAsync(url).ConfigureAwait(false)</c>, it
    /// is the whole operand, including the call to <c>ConfigureAwait</c>.
    /// </summary>
    /// <remarks>The rules of <see cref="MethodInterceptionContext.Receiver"/> apply.</remarks>
    public abstract IExpression Operand { get; }

    /// <summary>Gets the type of the await expression, or the <c>void</c> type when the await expression has no value.</summary>
    public abstract IType ResultType { get; }

    /// <summary>Gets the kind of the awaited expression.</summary>
    public abstract AwaitableKind AwaitableKind { get; }

    /// <summary>
    /// Gets where the calling function resumes after the original await. The value is <see cref="AwaitResumption.Unknown"/>
    /// for a custom awaitable, for a <c>ConfigureAwait</c> call whose argument is not a constant, and for a stored
    /// configured awaitable.
    /// </summary>
    public abstract AwaitResumption Resumption { get; }

    /// <summary>Gets the <c>ConfigureAwait</c> call that produces the awaited value, or <c>null</c>.</summary>
    public abstract AwaitConfiguration? Configuration { get; }

    /// <summary>
    /// Gets the method invoked by the awaited expression, after removing a call to <c>ConfigureAwait</c>, or <c>null</c>
    /// when the awaited expression is not an invocation. For <c>await client.GetAsync(url).ConfigureAwait(false)</c>,
    /// this is <c>HttpClient.GetAsync</c>.
    /// </summary>
    public abstract IMethod? AwaitedMethod { get; }

    /// <summary>
    /// Gets the return type of <c>ConfigureAwait(bool)</c> on <see cref="AwaitableType"/>, or <c>null</c> when this method
    /// does not exist. For <c>Task&lt;int&gt;</c>, it is <c>ConfiguredTaskAwaitable&lt;int&gt;</c>.
    /// </summary>
    public abstract IType? ConfiguredAwaitableType { get; }

    /// <summary>Gets a value indicating whether the awaiter is obtained through an extension <c>GetAwaiter</c> method.</summary>
    public abstract bool UsesExtensionGetAwaiter { get; }

    /// <summary>Gets a value indicating whether the value of the await expression is used.</summary>
    public abstract bool IsResultUsed { get; }
}
```

The context contains every fact that an earlier type filter used: `AwaitableType`, `AwaitableKind`, `Configuration` with the unconfigured type, which replaces the look-through rule, and `AwaitedMethod` (section [5.3.4](05a-api-registration.md#534-await-registrations), RC58).

The destination of an await site is its awaitable type. The context exposes it as `AwaitableType` and has no separate `Destination` property. Such a property would have the same value, and the base class cannot declare one `Destination` property whose type is `IMember` for member sites and `IType` for await sites (RC55).

The awaitable facts come from Roslyn's await-expression information and from the operand type, not from Metalama's `AsyncHelper`, which ignores inherited and extension `GetAwaiter` methods (ENG26 `CodeModel\Helpers\AsyncHelper.cs:115-171`). Under runtime async, `GetAwaiterMethod` can be null, so classification uses types (section [7.3](07-await-interception.md#73-awaitable-kinds-and-resumption-classes)).

#### 5.5.4 Relationship with ReferenceValidationContext

The interception contexts do not share a base class with `ReferenceValidationContext`. `InterceptionContext` is its own base class, with its own `AspectState` property. The two features share vocabulary instead: the origin and the destination of a site have the meaning of the ends of a reference (section [0.3](00-conventions.md#03-terms)).

The second product-owner review had adopted a shared base class, `ReferenceContext`, in a new package `Metalama.Extensions.References` (RC35, PO45). Its purpose was to evaluate reference predicates on interception contexts, which required changing the parameter of `ReferencePredicate.IsMatchCore` from `ReferenceValidationContext` to the base class (P27 `Metalama.Extensions.Architecture\Predicates\ReferencePredicate.cs:51`), a breaking change for user predicates (CS0115). The third batch selects targets by declaring type and member names (RC46), so the engine no longer evaluates reference predicates, and the shared base has no remaining purpose. It is withdrawn with the extraction (RC47). No Validation or Architecture type changes.

The contexts stay separate for these reasons:

- The evaluation unit differs. A validation context groups references by granularity, and `ReferenceEnd` throws when code reads a finer level than the granularity of the validator (P27 `Metalama.Extensions.Validation\ReferenceEnd.cs:81-89`). An interception context is always one site.
- The called side differs. A validation destination is a declaration at the granularity of the validator. An interception site has a destination member or awaitable type, and it also exposes the constructed method or accessor that it calls (`InterceptedMethod`).
- A shared base would couple the interceptor package to a type of another premium package, which the design avoids (section [9.2](09-premium-engine.md#92-sharing-with-metalamaextensionsvalidation-r5)).

For the same reasons, the interception contexts expose the origin and the destination as plain `IDeclaration`, `IMember` and `IType` values, and not as `ReferenceEnd` values. `ReferenceEnd` is a type of the Validation package, and its granularity contract would restrict providers, which always see one site.

| `ReferenceValidationContext` | `InterceptionContext` |
|---|---|
| `Origin.Declaration` at the finest granularity | `Origin` |
| `Origin.Type`, `Origin.Namespace`, `Origin.Member` | `CallingType`, `CallingNamespace`, and `Origin` when it is a member |
| `Destination.Declaration` | `Destination` and `MatchedMethod` (member sites), `AwaitableType` (await sites) |
| `Details[i].Source` | `Source` |
| `Details[i].Diagnostics` | `Diagnostics` |
| `AspectState` | `AspectState` |
| `ReferenceKinds.Invocation` | `InterceptMethods` |
| `ReferenceKinds.Default` and `ReferenceKinds.Assignment` on a property or an event | `InterceptAccessors` |

### 5.6 Result model

#### 5.6.1 InterceptorResult

```csharp
namespace Metalama.Extensions.Interceptors;

/// <summary>
/// The decision returned by an interceptor for one call site: skip the call site, redirect it to an existing method, or
/// redirect it to a method generated from a template.
/// </summary>
/// <remarks>
/// <para>
/// Call sites that receive equivalent template results share one generated method. Two template results are equivalent
/// when they have the same placement, the same template, the same template provider, equal arguments and equal tags,
/// when they come from the same aspect instance or fabric, and when the call sites have the same receiver mapping, the
/// same interceptor signature after the adjustments of the configure function (including the name and the
/// accessibility), and the same intercepted target. Arguments and tags are compared member by member: declarations
/// and types are compared as declarations, other values with <see cref="object.Equals(object)"/>.
/// </para>
/// </remarks>
[CompileTime]
[PublicAPI]
public sealed class InterceptorResult
{
    private InterceptorResult() { }

    /// <summary>
    /// Gets a result that leaves the call site unchanged for this provider. Other providers registered for the same call
    /// site are still evaluated. To explain a skip, report a diagnostic through <see cref="InterceptionContext.Diagnostics"/>.
    /// </summary>
    public static InterceptorResult Skip { get; }

    /// <summary>Redirects the call site to an existing method.</summary>
    /// <remarks>
    /// <para>The method must be accessible from the call site. A static method can be declared in any type. An instance
    /// method is called on the receiver when it is declared in the static type of the receiver or in one of its base
    /// types, and on <c>this</c> when it is declared in the calling type or in one of its base types. The first rule wins
    /// when both apply. The complete rules are given in the documentation of receiver mapping.</para>
    /// <para>Each parameter of the method receives its value from a source, <see cref="InterceptorArgument"/>. A
    /// parameter that <paramref name="bind"/> does not bind receives its canonical binding: the receiver-mapping rules
    /// select the parameter that receives the receiver, every other parameter receives the argument of the call site
    /// whose parameter has the same name, a caller-information parameter receives the caller information, and an
    /// optional parameter receives its own default value. A required parameter that remains unbound is an error. The
    /// value of each source must convert implicitly to the parameter type. A <c>ref</c>, <c>out</c> or <c>in</c>
    /// parameter must receive an argument with the same reference kind and an identical type. The return type must be
    /// implicitly convertible to the return type of the intercepted method. It must be identical when the call site is
    /// inside a conditional access (<c>?.</c>).</para>
    /// <para>For a method group converted to a delegate or to a function pointer, the site stays a method group of this
    /// method only when the parameter types, the reference kinds, the default values, <c>params</c> and the return type
    /// are identical to those of the intercepted method, after the receiver mapping, when the method has no additional
    /// parameter, and when the binding is canonical. Otherwise the site uses a lambda wrapper.</para>
    /// <para>For an await expression, the parameter bound to <see cref="InterceptorArgument.Awaitable"/> must accept the
    /// awaitable type through an implicit conversion. By default, it is the single required parameter. The return type of the method can be another awaitable type
    /// than the awaited expression, but the result of awaiting its return value must convert implicitly to the result type of the await expression. When the await expression
    /// has no value, awaiting the return value must produce no value either. A cast is inserted when the value of the await expression is used and the
    /// two result types differ.</para>
    /// <para>For an accessor site, the parameters are the receiver, when the receiver mapping passes it as a parameter,
    /// followed by the value for a setter or the handler for an add or remove accessor. A getter returns a type implicitly
    /// convertible to the property type. A setter returns a type implicitly convertible to the property type, or
    /// <c>void</c>, which is admissible only at sites whose value is not used.</para>
    /// </remarks>
    /// <param name="method">The existing method.</param>
    /// <param name="bind">A function that binds the parameters of <paramref name="method"/> for this site, or <c>null</c>
    /// for the canonical binding. See <see cref="IInterceptorMethodBinder"/>.</param>
    public static InterceptorResult ExistingMethod( IMethod method, Action<IInterceptorMethodBinder>? bind = null );

    /// <summary>Redirects the call site to a method generated from a template given as a <see cref="MethodTemplateSelector"/>.</summary>
    /// <remarks>
    /// A selector can name alternative templates for async and iterator methods. Such a selector is valid only for a method
    /// or accessor site. At an await site, a selector that names an alternative template or sets one of its flags is an
    /// invalid result (LAMA1014). Pass the template name as a <see cref="string"/> instead.
    /// </remarks>
    /// <param name="template">A <see cref="MethodTemplateSelector"/>.</param>
    /// <param name="placement">The declaration in which the method is generated. This parameter is required.</param>
    /// <param name="configure">A function that adjusts the signature of the generated method, or <c>null</c>. The engine
    /// fills the builder with the signature derived from the call site and then invokes the function once for this call
    /// site. By default, the name is the name of the intercepted method followed by <c>_Interceptor</c>, the name of the
    /// property or event followed by the accessor keyword and <c>_Interceptor</c> for an accessor (for example
    /// <c>Status_set_Interceptor</c>), or <c>Await_Interceptor</c> for await expressions, and a numeric suffix is added
    /// when the name is already used in the placement. See <see cref="IInterceptorBuilder"/>.</param>
    /// <param name="args">Values of the compile-time parameters of the template.</param>
    /// <param name="tags">An object exposed to the template through <c>meta.Tags</c>. It is merged with
    /// <c>IAspectBuilder.Tags</c> of the registering aspect, and the values given here win.</param>
    /// <param name="templateProvider">The object or type that declares the template. When it is <c>default</c>, the
    /// template provider is the interceptor provider object when it implements <see cref="ITemplateProvider"/>, and
    /// otherwise the default template provider of the registration.</param>
    public static InterceptorResult Template(
        in MethodTemplateSelector template,
        InterceptorPlacement placement,
        Action<IInterceptorBuilder>? configure = null,
        object? args = null,
        object? tags = null,
        TemplateProvider templateProvider = default );

    /// <summary>Redirects the site to a method generated from the template of the given name.</summary>
    /// <remarks>
    /// This overload serves every kind of site, and it is the form to use at an await site. C# selects it for a string
    /// argument, because an identity conversion is better than the implicit conversion from <see cref="string"/> to
    /// <see cref="MethodTemplateSelector"/>. At a method or accessor site, it is equivalent to a selector that names only
    /// a default template. At an await site, the template method decides whether the interceptor awaits the original
    /// awaitable: an <c>async</c> template awaits the value of <c>meta.Proceed()</c>, and a non-async template receives the
    /// awaited value from <c>meta.Proceed()</c>.
    /// </remarks>
    /// <param name="template">The name of the template.</param>
    public static InterceptorResult Template(
        string template,
        InterceptorPlacement placement,
        Action<IInterceptorBuilder>? configure = null,
        object? args = null,
        object? tags = null,
        TemplateProvider templateProvider = default );

    /// <summary>Redirects the call site to a method generated from a template given as a <see cref="TemplateInvocation"/>.</summary>
    /// <remarks>At an await site, the rules of the <see cref="string"/> overload apply to the template name.</remarks>
    public static InterceptorResult Template(
        TemplateInvocation template,
        InterceptorPlacement placement,
        Action<IInterceptorBuilder>? configure = null,
        object? tags = null );

    /// <summary>
    /// Returns a copy of the current result with options that control how an await interceptor is generated and how the
    /// await expression is rewritten. This method is valid only for await interceptors.
    /// </summary>
    /// <param name="awaitableType">The return type of an interceptor in <see cref="AwaitInterceptionMode.Awaitable"/> mode.
    /// When it is <c>null</c>, the interceptor returns the awaited type itself.</param>
    public InterceptorResult WithAwaitRewriteOptions( AwaitRewriteOptions options, IType? awaitableType = null );

    /// <summary>Gets the kind of the result.</summary>
    public InterceptorResultKind Kind { get; }

    /// <summary>Gets the existing method, or <c>null</c> when the result is not an existing method.</summary>
    public IMethod? Method { get; }

    /// <summary>
    /// Gets the template, or <c>null</c> when the result is not a template. For a result created from a template name or
    /// from a <see cref="TemplateInvocation"/>, the selector names only a default template.
    /// </summary>
    public MethodTemplateSelector? TemplateSelector { get; }

    /// <summary>Gets the template provider given explicitly, or <c>default</c>.</summary>
    public TemplateProvider TemplateProvider { get; }

    /// <summary>Gets the template arguments, or <c>null</c>.</summary>
    public object? Arguments { get; }

    /// <summary>Gets the tags exposed to the template, or <c>null</c>.</summary>
    public object? Tags { get; }

    /// <summary>Gets the placement of the generated method, or <c>null</c> when the result is not a template.</summary>
    public InterceptorPlacement? Placement { get; }

    /// <summary>Gets the function that adjusts the signature of the generated method, or <c>null</c>.</summary>
    public Action<IInterceptorBuilder>? Configure { get; }

    /// <summary>Gets the function that binds the parameters of the existing method, or <c>null</c>.</summary>
    public Action<IInterceptorMethodBinder>? Bind { get; }

    /// <summary>Gets the await rewrite options, or <c>null</c>.</summary>
    public AwaitRewriteOptions? AwaitRewriteOptions { get; }

    /// <summary>Gets the return type of an interceptor in <see cref="AwaitInterceptionMode.Awaitable"/> mode, or <c>null</c>.</summary>
    public IType? AwaitableType { get; }
}

/// <summary>Kinds of <see cref="InterceptorResult"/>.</summary>
[CompileTime]
public enum InterceptorResultKind
{
    Skip,
    ExistingMethod,
    Template
}
```

The first template factory takes a `MethodTemplateSelector` for the same reason as `Override` (FW27 `Aspects\AdviserExtensions.cs:62-67`): a method interceptor for a method that returns `Task<T>` can have an async variant. The second factory takes a template name. It is needed because `MethodTemplateSelector` has an implicit conversion from `string` (FW27 `Advising\MethodTemplateSelector.cs:174`): without a `string` overload, a template name would become a selector silently. With it, overload resolution prefers the identity conversion of the argument (RC `Binder\Semantics\OverloadResolution\OverloadResolution.cs:3003-3004`, `ExpressionMatchExactly`), so a string never becomes a selector, and existing calls such as `InterceptorResult.Template( nameof(this.LogCall), placement )` keep their meaning at method sites. An await site accepts a template name only (decision PO60, RC60). The engine still receives a `MethodTemplateSelector` when a provider passes one explicitly, and it reports LAMA1014 at an await site when the selector names an alternative template or sets `UseAsyncTemplateForAnyAwaitable` or `UseEnumerableTemplateForAnyEnumerable`. The third overload accepts the existing `TemplateInvocation` record, which has no tags, so `tags` is a separate parameter. The `configure` function replaces the `name` parameter of earlier versions, because it also sets the name (section [5.6.8](#568-parameter-binding-and-the-signature-builder)). `InterceptorResult` is not `[Durable]`, because a result is consumed within one evaluation, so the `configure` and `bind` parameters of the result carry no `[Durable]` either. The `bind` function of `ExistingMethod` receives an `IInterceptorMethodBinder`, and the `configure` function of `Template` receives an `IInterceptorBuilder`, which derives from it (section [5.6.8](#568-parameter-binding-and-the-signature-builder)).

R14 is covered by two members: `Skip` declines a call site, and `InterceptionContext.Diagnostics` reports any diagnostic, including one that explains a skip. An earlier version also had `SkipWithJustification( string )`, which the second product-owner batch removed (RC42). A provider that wants the IDE to show why it skipped a call site reports its own diagnostic, with its own identifier and severity.

Diagnostics reported by a provider do not depend on the linker. The premium engine produces them when it evaluates the provider, which happens in two places:

- At compile time, in the source stage (section [9.5.7](09-premium-engine.md#957-evaluation-of-user-interceptor-providers)). Every build reports them.
- At design time, in the analyzer (Phase B, section [9.7.4](09-premium-engine.md#974-phase-b-analyzer)), only if decision PO26 is accepted. If PO26 is rejected, provider diagnostics are reported only by the build, and the documentation says so.

Template expansion, grouping, the checks of the factory and the checks of the linker never run in the analyzer. Their diagnostics are reported only by the build and, for the call sites that the preview rewrites, by the preview (section [9.8](09-premium-engine.md#98-preview-live-templates-and-introspection)).

`InterceptorResult` is a sealed class with a private constructor and static factories. A later kind of result, such as the handler of section [16.2](16-future-directions.md#162-delegate-based-handler-interceptors), is a new factory and a new member of `InterceptorResultKind`. It is an addition, not a breaking change, as long as providers do not switch exhaustively over the kinds. The documentation of `InterceptorResultKind` states that new members can be added.

#### 5.6.2 Await rewrite options

```csharp
/// <summary>Options that control how an await interceptor is generated and how the await expression is rewritten.</summary>
/// <remarks>
/// The options contain no code-model type, so a registration shorthand can store them. The return type of an interceptor
/// in <see cref="AwaitInterceptionMode.Awaitable"/> mode is passed to <see cref="InterceptorResult.WithAwaitRewriteOptions"/>.
/// The selection of await expressions is described by <see cref="AwaitInterceptionOptions"/>.
/// </remarks>
[CompileTime]
[PublicAPI]
[Durable]
[ImmutableType]
public sealed record AwaitRewriteOptions
{
    /// <summary>Gets the mode. The default is <see cref="AwaitInterceptionMode.Await"/>.</summary>
    public AwaitInterceptionMode Mode { get; init; } = AwaitInterceptionMode.Await;

    /// <summary>
    /// Gets the task type of an interceptor generated from a non-async template in <see cref="AwaitInterceptionMode.Await"/>
    /// mode. An <c>async</c> template declares its task type. For such a template, a value other than
    /// <see cref="AwaitInterceptorTaskKind.Default"/> must name the declared task type, or the template is reported as
    /// incompatible (LAMA1016).
    /// </summary>
    public AwaitInterceptorTaskKind TaskKind { get; init; } = AwaitInterceptorTaskKind.Default;

    /// <summary>
    /// Gets the resumption policy to apply when the resumption of the original await is <see cref="AwaitResumption.Unknown"/>
    /// and the interceptor returns a task of another type.
    /// </summary>
    public AwaitResumption? Resumption { get; init; }
}

/// <summary>Modes of an await interceptor.</summary>
[CompileTime]
public enum AwaitInterceptionMode
{
    /// <summary>
    /// The interceptor is an <c>async</c> method. In a non-async template, <c>meta.Proceed()</c> awaits the original
    /// awaitable. In an <c>async</c> template, <c>meta.Proceed()</c> returns the original awaitable, and the template
    /// awaits it.
    /// </summary>
    Await,

    /// <summary>
    /// The interceptor is not <c>async</c>. It returns an awaitable that the call site awaits. The template must not be
    /// <c>async</c>, and <c>meta.Proceed()</c> returns the original awaitable without awaiting it.
    /// </summary>
    Awaitable
}

/// <summary>Task types of an interceptor in <see cref="AwaitInterceptionMode.Await"/> mode.</summary>
[CompileTime]
public enum AwaitInterceptorTaskKind
{
    /// <summary><c>ValueTask</c> or <c>ValueTask&lt;T&gt;</c> when available with a method builder, otherwise <c>Task</c> or <c>Task&lt;T&gt;</c>.</summary>
    Default,

    /// <summary><c>ValueTask</c> or <c>ValueTask&lt;T&gt;</c>. The type must be available (LAMA1023).</summary>
    ValueTask,

    /// <summary><c>Task</c> or <c>Task&lt;T&gt;</c>.</summary>
    Task
}
```

The mode is not deduced from the template. Mode `Await` accepts an `async` template and a non-async template, and the template method decides whether the interceptor awaits the original awaitable itself (section [7.9.1](07-await-interception.md#791-accepted-template-shapes), RC60). Mode `Awaitable` is the explicit choice for an interceptor that returns another awaitable without awaiting, for example a custom awaitable (R11), and it requires a non-async template. `Resumption = AwaitResumption.Unknown` is rejected with LAMA1020, in the same way as a missing policy. `WithAwaitRewriteOptions` on the result of a method or accessor site is reported with LAMA1014. Section [7](07-await-interception.md#7-await-interception) defines the semantics. The record was named `AwaitInterceptorOptions`, and the method `WithAwaitOptions`, in earlier versions of this design (RC53).

#### 5.6.3 InterceptorPlacement

```csharp
namespace Metalama.Extensions.Interceptors;

/// <summary>Specifies where a generated interceptor method is declared: the placement of the method.</summary>
/// <remarks>
/// <para>
/// The generated method is added during linking. It is not visible to aspects, to the code model, or to the code that the
/// IDE generates, and the type of a type placement does not need to be <c>partial</c>.
/// </para>
/// <para>
/// The placement does not decide whether the generated method is static. The receiver-mapping rules decide it for each
/// call site. By default, the method is an instance method when the placement is the static type of the receiver or one
/// of its base types, or when the call is a <c>base</c> call to a virtual method. Otherwise, it is static. An
/// <see cref="IInterceptorBuilder"/> can choose a static method instead of an instance method, or an instance
/// method when the placement is the calling type or one of its base types.
/// </para>
/// <para>
/// The generated method has the smallest accessibility that allows every call site of its group to call it:
/// <c>private</c> in the calling type, <c>private protected</c> in a base type, and <c>internal</c> in any other type.
/// A local function has no accessibility modifier.
/// </para>
/// </remarks>
[CompileTime]
[PublicAPI]
[Durable]
[ImmutableType]
public sealed class InterceptorPlacement
{
    private InterceptorPlacement() { }

    /// <summary>
    /// Gets a placement that adds the method to the innermost type that contains the call site. Call sites in top-level
    /// statements do not support this placement, because the type that contains them is implicit.
    /// </summary>
    public static InterceptorPlacement CallingType();

    /// <summary>
    /// Gets a placement that adds the method to a given type of the current project. The type must be accessible from the
    /// call sites, and it must not be generic unless it is the calling type or one of its base types. The method can be an
    /// instance method only when the type is the static type of the receiver or one of its base types, or the calling type
    /// or one of its base types.
    /// </summary>
    public static InterceptorPlacement InType( INamedType type );

    /// <summary>Gets a placement that adds the method to a given type of the current project.</summary>
    public static InterceptorPlacement InType( Type type );

    /// <summary>
    /// Gets a placement that adds the method to the base-most type of the calling type that can receive it. The engine
    /// walks from the calling type through its base types that are declared in the current project, and it selects the
    /// last type for which <see cref="InType(INamedType)"/> would be admissible for the call site. When no base type is
    /// admissible, the method is added to the calling type, as with <see cref="CallingType"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call sites in different derived types that reach the same base type share one interceptor method when their
    /// other grouping data are equal. The accessibility follows the rules of type placements: <c>private protected</c>
    /// in a base type, and <c>private</c> when the selected type is the calling type. An instance method that is called
    /// on a receiver whose type does not derive from the calling type is <c>internal</c>.
    /// </para>
    /// <para>
    /// The walk stops at the first base type that cannot receive the method, for example because the intercepted
    /// member is not accessible from it or because the signature uses a type that it cannot name. A private
    /// intercepted member keeps the method in its declaring type, which must be the calling type. In an instance
    /// interceptor, <c>meta.This</c> has the type of the selected type, so a template can use only the members that are
    /// visible in that type. This placement never produces an extension-form method or a local function. Call sites in
    /// top-level statements do not support it.
    /// </para>
    /// </remarks>
    public static InterceptorPlacement BaseMostAccessibleType();

    /// <summary>
    /// Gets a placement that adds the method to an internal static class that Metalama generates once per project, in the
    /// global namespace. The methods are static and can be called in extension form. Call sites written with the
    /// null-conditional operator are supported by this placement, by a static class of the project whose extension
    /// methods are in scope, and by an instance method declared in the static type of the receiver or in one of its base
    /// types.
    /// </summary>
    public static InterceptorPlacement GeneratedStaticClass();

    /// <summary>
    /// Gets a placement that adds a local function to the root block of the body of the origin, which is the member that
    /// contains the call site. The generated code can use the parameters of the origin through
    /// <see cref="MethodInterceptionInfo.Origin"/>, which templates read as <c>meta.MethodInterception.Origin</c>, and
    /// <c>this</c> when the origin is an instance member of a class. A local function in a struct member cannot use
    /// <c>this</c>. Call sites in initializers, in constructor initializers, in primary-constructor base arguments, in
    /// top-level statements and in static lambdas or static local functions do not support this placement.
    /// </summary>
    public static InterceptorPlacement LocalFunction();

    /// <summary>Gets the kind of the placement.</summary>
    public InterceptorPlacementKind Kind { get; }

    /// <summary>Gets the type specified with <see cref="InType(INamedType)"/>, or <c>null</c>.</summary>
    public IDurableRef<INamedType>? SpecifiedType { get; }
}

/// <summary>Kinds of <see cref="InterceptorPlacement"/>.</summary>
[CompileTime]
public enum InterceptorPlacementKind
{
    CallingType,
    SpecifiedType,
    BaseMostAccessibleType,
    GeneratedStaticClass,
    LocalFunction
}
```

| Requirement | API |
|---|---|
| An explicit type placement (R8) | `InterceptorPlacement.InType( type )` |
| The calling type, static (R13) | `InterceptorPlacement.CallingType()` (rule R1, the default when the receiver is not of the calling type's hierarchy) |
| The calling type or a base type of it, instance, with the caller's `this` (R13) | `InterceptorPlacement.CallingType()` or `InType( baseType )`, with `configure: b => b.IsStatic = false` (rule R3) |
| The base-most type of the calling hierarchy that can receive the method, shared by sibling types | `InterceptorPlacement.BaseMostAccessibleType()` (section [6.5.6](06b-signatures-and-validation.md#656-base-most-accessible-type)) |
| The type of the receiver, instance, with the receiver as `this` | `InterceptorPlacement.InType( receiverType )` or `CallingType()` when the receiver has the calling type (rule R2, the default) |
| A local function in the origin (R12) | `InterceptorPlacement.LocalFunction()` |
| An existing instance method in the calling hierarchy or in the receiver's hierarchy (R13) | `InterceptorResult.ExistingMethod( method )` |
| A placement that a library can name in any consumer (section [9.9](09-premium-engine.md#99-cross-project-interception)) | `InterceptorPlacement.CallingType()` or `InterceptorPlacement.GeneratedStaticClass()` |
| Conditional access (RC25) | `InterceptorPlacement.GeneratedStaticClass()`, `InType` with a top-level non-generic static class whose extension methods are in scope at the call site (rule R1x), or an instance interceptor in the receiver's hierarchy (rule R2) |

The static or instance choice is not a property of the placement, because one placement can serve call sites with different receivers. Section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping) gives the rules. A `base` call to a virtual method needs a non-virtual call, which only an instance method of the calling type or a local function can make (rule R4). A static interceptor would dispatch virtually and could recurse indefinitely. For this reason, a registration on `Stream.Close` with `CallingType()` intercepts `base.Close()` calls in derived streams without extra code. A `base.Dispose()` call does not need this rule, because `Stream.Dispose()` is not virtual. Such a call is modeled as a call on `this` (section [6.3](06a-call-site-model.md#63-rules-table), row 5). An earlier version expressed the choice with an enumeration `InterceptorBinding` on the placement. The builder of section [5.6.8](#568-parameter-binding-and-the-signature-builder) replaces it (RC38).

Example of `BaseMostAccessibleType()` (decision PO61, RC61). `OrderService` and `InvoiceService` derive from `ServiceBase`, and all three are declared in the current project. A fabric registers one template for the calls to `ILogger.Log`, with `BaseMostAccessibleType()`:

```csharp
amender.InterceptMethods(
    typeof(ILogger),
    nameof(ILogger.Log),
    nameof(this.LogWithService),
    InterceptorPlacement.BaseMostAccessibleType() );
```

```csharp
// Declared in a referenced assembly.
public interface ILogger { void Log( string message ); }

// Source of the current project.
public abstract class ServiceBase { }

public sealed class OrderService( ILogger logger ) : ServiceBase
{
    public void Place() => logger.Log( "Order placed." );
}

public sealed class InvoiceService( ILogger logger ) : ServiceBase
{
    public void Send() => logger.Log( "Invoice sent." );
}

// Transformed code.
public abstract class ServiceBase
{
    private protected static void Log_Interceptor( ILogger receiver, string message )
    {
        // Template body.
        receiver.Log( message );
    }
}

public sealed class OrderService( ILogger logger ) : ServiceBase
{
    public void Place() => ServiceBase.Log_Interceptor( logger, "Order placed." );
}

public sealed class InvoiceService( ILogger logger ) : ServiceBase
{
    public void Send() => ServiceBase.Log_Interceptor( logger, "Invoice sent." );
}
```

For each site, the walk starts at the calling type, `OrderService` or `InvoiceService`, and reaches `ServiceBase`: `ILogger.Log` is public, `ILogger` and `string` can be named in `ServiceBase`, and `object` is not declared in the current project, so the walk ends there. Both sites resolve to `InType( ServiceBase )`, so their keys have the same placement, and one method serves both. With `CallingType()`, each service would receive its own copy. The method is static under rule R1, because the receiver `logger` is not in the hierarchy of `ServiceBase`. A `configure` function can make it an instance method under rule R3, because `ServiceBase` is in the caller family of both sites.

The generated static class is created once per project with the factory primitive `DeclareStaticClass` (section [10.4.3](10a-oss-bridge-hook-factory.md#1043-synthesized-static-classes)). Its default name is `MetalamaInterceptors`, with a numeric suffix when the name is taken (decision PO19).

The premium engine resolves each `InterceptorPlacement` into a `SynthesizedMethodPlacement` of the open-source factory (section [10.4.4](10a-oss-bridge-hook-factory.md#1044-synthesized-methods-and-proceed-bindings)), so the two layers use the same word. `CallingType()`, `InType` and `BaseMostAccessibleType()` become `SynthesizedMethodPlacement.InType( INamedType )`, with the type that the walk of section [6.5.6](06b-signatures-and-validation.md#656-base-most-accessible-type) selected for the last one. `GeneratedStaticClass()` becomes `SynthesizedMethodPlacement.InType( SynthesizedTypeHandle )`. `LocalFunction()` becomes `SynthesizedMethodPlacement.AsLocalFunction( host )`, where the host is the origin of the site. Earlier versions of this design named the type `InterceptorContainer` (RC54).

#### 5.6.4 Rules for existing methods

These rules implement R9 and R13 for `InterceptorResult.ExistingMethod` and for the `IMethod` registration shorthand. Section [6.6](06b-signatures-and-validation.md#66-signature-validation-existing-methods-and-adjusted-signatures-r9) gives the complete validation algorithm.

- The method can be declared in the current project or in a referenced assembly.
- A static method uses rule R1, or R1x when it is an extension method. An instance method uses rule R2 when it is declared in the static type of the receiver or in one of its base types, rule R3 when it is declared in the calling type or in one of its base types, and rule R4 for a `base` call to a virtual method (section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping)). The expected parameter list follows from the rule (section [6.6](06b-signatures-and-validation.md#66-signature-validation-existing-methods-and-adjusted-signatures-r9), E5 and E7).
- The parameters of the method are bound to sources (section [5.6.8](#568-parameter-binding-and-the-signature-builder)). By default, the receiver goes to the parameter that the receiver mapping selects, and every other parameter binds to the argument of the site with the same name. A caller-information parameter receives the values computed at the source call site (section [6.7](06b-signatures-and-validation.md#67-caller-information-materialization)), and an unbound optional parameter receives its own default. An unbound required parameter is reported with LAMA1013 (`UnboundParameter`). The `bind` function of `ExistingMethod` binds the other parameters explicitly, for example to the receiver in another position, to the caller's `this`, or to a pulled value, and `BindRemainingByPosition()` opts in to positional binding.
- A generic existing method used for an invocation has either no type parameter, or as many type parameters as the total arity of the type-argument slots of the call site, in the slot order of section [6.2.7](06a-call-site-model.md#627-arguments-generic-context-and-passing-mode) (RC29). It is called with the type arguments of the call site.
- A generic existing method used for an await expression is called without explicit type arguments, and C# type inference must succeed from the awaited expression.
- Parameter names that differ from those of the intercepted method need an explicit binding, `Bind( InterceptorArgument.Argument( ... ) )` or `BindRemainingByPosition()`, because the canonical binding matches names only (RC64). The rewrite passes the arguments in the order of the parameters of the method, with the temporaries of section [5.6.8](#568-parameter-binding-and-the-signature-builder) when the order of evaluation would otherwise change.
- When the return type differs and is implicitly convertible, the engine inserts a cast when the value of the call site is used, so that the rewritten expression keeps the original type. When the value is not used, no cast is inserted, because a cast is not a valid statement expression (CS0201). An existing method whose return type differs cannot intercept a call site inside a conditional access, because a cast cannot be placed inside a `?.` chain (section [6.6](06b-signatures-and-validation.md#66-signature-validation-existing-methods-and-adjusted-signatures-r9), E14).
- For an await expression, the call site is rewritten according to the rules of section [7.5](07-await-interception.md#75-the-adaptive-rewrite-challenge-to-b8-adopted), which depend on the type returned by the method. The method can return another awaitable type than the awaited expression. The result of awaiting its return value must convert implicitly to the result type of the await, and a cast restores the original result type when the value is used (section [7.6.1](07-await-interception.md#761-result-compatibility)).
- For an accessor site, the method follows the accessor shapes of section [6.4.13](06b-signatures-and-validation.md#6413-accessor-sites) and rule E19 of section [6.6](06b-signatures-and-validation.md#66-signature-validation-existing-methods-and-adjusted-signatures-r9). A `void` setter interceptor is admissible only at sites whose value is not used. A setter interceptor that returns a type implicitly convertible to the property type is admissible at every site.
- A call site located inside the body of the existing method that intercepts it is reported with LAMA1018 (RC28), because the rewrite would make the method call itself.
- Violations are reported with LAMA1013 at the call site, and the call site is left unchanged.

#### 5.6.5 Rules for template results

- For a method interceptor, the generated method has the exact signature derived from the call site (R9): a receiver parameter when the receiver mapping passes the receiver as a parameter (section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping)), then the parameters of the intercepted method with the same reference kinds, then the return type of the intercepted method (section [6.4](06b-signatures-and-validation.md#64-signature-derivation)).
- The `configure` function can then adjust this signature and bind its parameters within the limits of section [5.6.8](#568-parameter-binding-and-the-signature-builder), and add parameters (section [5.6.9](#569-added-parameters-and-pulled-values)). The adjusted signature and the binding are validated with the rules that apply to existing methods (section [6.6](06b-signatures-and-validation.md#66-signature-validation-existing-methods-and-adjusted-signatures-r9)).
- For a method or accessor interceptor, the template variant of `MethodTemplateSelector` is selected from the return type of the generated method, with the selector unchanged (FW27 `Advising\MethodTemplateSelector.cs:107`). The generated method is `async` only when the selected template is interpreted as async. This happens when the selector sets `UseAsyncTemplateForAnyAwaitable` and the return type has a method builder (ENG27 `Advising\AdviceFactory.cs:262`). Whether the intercepted method is `async` is irrelevant, because the interceptor is a new method.
- For an await interceptor, the template is given by its name, and no variant is selected. The `async` modifier of the template method decides whether the template awaits the original awaitable itself (section [7.9.1](07-await-interception.md#791-accepted-template-shapes), RC60). The generated method and the call-site rewrite follow section [7](07-await-interception.md#7-await-interception).
- For an accessor interceptor, the generated method has the accessor shape of section [6.4.13](06b-signatures-and-validation.md#6413-accessor-sites): a getter returns the property type, a setter takes the value and returns it, and an add or remove interceptor takes the handler and returns `void`.

#### 5.6.6 Names

The default requested name is `{MethodName}_Interceptor` for an invocation, `{MemberName}_{Accessor}_Interceptor` for an accessor, where `{Accessor}` is `get`, `set`, `add` or `remove` (for example `Status_get_Interceptor` and `Changed_add_Interceptor`), and `Await_Interceptor` for an await expression (RC16). The accessor name starts with the member name, like the method name, and it does not start with `get_` or `set_`, which the accessors of the properties of the placement type could use. The `configure` function can request another name through `IInterceptorBuilder.Name` (section [5.6.8](#568-parameter-binding-and-the-signature-builder)). The pattern follows the underscore convention of names that the linker generates, for example `{Member}_{Aspect}` for overrides (ENG27 `Linking\LinkerInjectionNameProvider.cs:42-45`). A numeric suffix is added when the name is taken, and each group gets its own name (section [8.7](08-deduplication-and-naming.md#87-names-and-accessibility)). Unique names avoid overload resolution at the rewritten call site, which could bind a `null` argument to a different overload.

#### 5.6.7 Grouping identity as seen by users

Call sites share a generated method when all of these are equal: the placement, the receiver-mapping rule, the requested name, the requested accessibility, the interceptor signature after the adjustments of the `configure` function, the intercepted target and its dispatch, the template (member and selected variant), the template provider, the arguments, the tags, and the registering owner (aspect instance or fabric). Section [8](08-deduplication-and-naming.md#8-deduplication-and-naming) defines the key precisely.

Consequences that the documentation states:

- Per-call-site data passed as template arguments creates one method per distinct value. Sample 4 shows this.
- Per-call-site data passed through an added parameter does not split a group, because the binding of a parameter and the value that it receives are site data. Only the signature that the builder shapes, including the added parameters, is part of the identity (section [5.6.9](#569-added-parameters-and-pulled-values)).
- The kind of use is not part of the identity. A call site and a method-reference site with the same signature share one method (sample 11). The signature of a group that contains a method-reference site is the exact signature of the target (section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites)).
- The shape of an accessor site is not part of the identity either. One getter interceptor serves plain reads and the reads of compound sites, and one setter interceptor serves writes whose value is used or discarded and the writes of compound sites (sample 12). The accessor kind is part of the identity (section [8.2](08-deduplication-and-naming.md#82-the-key)).
- An aspect applied to many types, each registering the same template, produces one method per aspect instance, even when the placement is shared. To share one method across types, register from a single fabric (sample 6b) or from an aspect on a namespace or the compilation.

#### 5.6.8 Parameter binding and the signature builder

A result can bind the parameters of its interceptor, and a template result can also adjust the signature of the generated method (decisions PO49, PO62 and PO63, RC62 to RC64). Existing methods and synthesized interceptors do not share one interface, because the signature of an existing method is fixed. Both need parameter binding: each parameter of the interceptor receives its value at a site from a source, `InterceptorArgument`.

- `InterceptorResult.ExistingMethod( method, bind )` passes an `IInterceptorMethodBinder` to `bind`. The binder describes the existing method, and it binds its parameters.
- `InterceptorResult.Template( ..., placement, configure, ... )` and the template shorthands pass an `IInterceptorBuilder` to `configure`. The builder derives from the binder. The engine first fills it with the signature derived from the call site: the receiver mapping (section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping)), the parameters, the return type, the defaults and the accessibility. The function then adjusts the signature within the limits of this section, binds parameters, and adds parameters (section [5.6.9](#569-added-parameters-and-pulled-values)).

```csharp
namespace Metalama.Extensions.Interceptors;

/// <summary>
/// Describes where the value of one parameter of an interceptor comes from at a site. The static members create the
/// sources. A source is passed to <see cref="IInterceptorParameterBinder.Bind"/> or to
/// <see cref="IInterceptorBuilder.AddParameter"/>.
/// </summary>
/// <remarks>
/// <para>
/// The receiver and the arguments of the site are evaluated once, from left to right, as in the original code. Pulled
/// values and the caller's instance are evaluated after them.
/// </para>
/// <para>
/// Later versions can add sources, for example the packed arguments and the proceed delegate of section 16.8 of the
/// design. A provider that inspects <see cref="Kind"/> handles the kinds that it knows.
/// </para>
/// </remarks>
[CompileTime]
[PublicAPI]
public sealed class InterceptorArgument
{
    private InterceptorArgument() { }

    /// <summary>
    /// Gets a source that gives the receiver of the site. The source is not available when the receiver is the
    /// <c>this</c> of the interceptor (rules R2 and R4) or when the site has no receiver (rule R0).
    /// </summary>
    public static InterceptorArgument Receiver { get; }

    /// <summary>
    /// Gets a source that gives the argument of the site for the parameter of the intercepted method that has the given
    /// name. When the site omits the argument, the source gives the value that the C# compiler binds at the site.
    /// </summary>
    public static InterceptorArgument Argument( string interceptedParameterName );

    /// <summary>Gets a source that gives the argument of the site for a parameter of the intercepted method.</summary>
    public static InterceptorArgument Argument( IParameter interceptedParameter );

    /// <summary>Gets a source that gives the value of a setter site, or the handler of an add or remove site.</summary>
    public static InterceptorArgument Value { get; }

    /// <summary>Gets a source that gives the awaited operand of an await site.</summary>
    public static InterceptorArgument Awaitable { get; }

    /// <summary>
    /// Gets a source that gives the <c>this</c> of the origin, which is the caller's instance. The source is not
    /// available in static members, instance field initializers, constructor initializers, static lambdas, top-level
    /// statements, and lambdas or local functions of struct members.
    /// </summary>
    public static InterceptorArgument CallerInstance { get; }

    /// <summary>Gets a source that gives caller information computed at the site.</summary>
    /// <param name="kind">The kind of caller information.</param>
    /// <param name="argumentOf">For <see cref="CallerInfoKind.ArgumentExpression"/>, the name of the parameter of the
    /// intercepted method whose argument text is given. It must be <c>null</c> for the other kinds.</param>
    public static InterceptorArgument CallerInfo( CallerInfoKind kind, string? argumentOf = null );

    /// <summary>
    /// Gets a source that gives a value pulled at the site: an expression, a parameter of the origin, or a constant.
    /// <see cref="PullAction.None"/> means that the site cannot supply the value.
    /// <see cref="PullAction.IntroduceParameterAndPull"/> is not supported.
    /// </summary>
    public static InterceptorArgument Pull( PullAction action );

    /// <summary>Gets the kind of the source.</summary>
    public InterceptorArgumentKind Kind { get; }

    /// <summary>Gets the name of the parameter of the intercepted method for <see cref="InterceptorArgumentKind.Argument"/>, or <c>null</c>.</summary>
    public string? InterceptedParameterName { get; }

    /// <summary>Gets the kind of caller information for <see cref="InterceptorArgumentKind.CallerInfo"/>, or <c>null</c>.</summary>
    public CallerInfoKind? CallerInfoKind { get; }

    /// <summary>Gets the pull action for <see cref="InterceptorArgumentKind.Pull"/>, or <c>null</c>.</summary>
    public PullAction? PullAction { get; }
}

/// <summary>Kinds of <see cref="InterceptorArgument"/>. Later versions can add members.</summary>
[CompileTime]
public enum InterceptorArgumentKind
{
    Receiver,
    Argument,
    Value,
    Awaitable,
    CallerInstance,
    CallerInfo,
    Pull
}

/// <summary>Kinds of caller information.</summary>
[CompileTime]
public enum CallerInfoKind
{
    LineNumber,
    FilePath,
    MemberName,
    ArgumentExpression
}

/// <summary>One parameter of an interceptor, as seen by the function that binds the parameters.</summary>
/// <remarks>The object is valid only while the function runs.</remarks>
[CompileTime]
[PublicAPI]
[InternalImplement]
public interface IInterceptorParameterBinder : IParameter
{
    /// <summary>
    /// Gets the source of the value: the explicit binding, or the canonical binding when none was set. The value is
    /// <c>null</c> when the parameter has neither. An optional parameter then receives its own default value, and a
    /// required parameter is an error (LAMA1013).
    /// </summary>
    InterceptorArgument? Argument { get; }

    /// <summary>Gets a value indicating whether <see cref="Bind"/> set <see cref="Argument"/>.</summary>
    bool IsExplicitlyBound { get; }

    /// <summary>
    /// Binds the parameter to a source. A later call replaces the binding. The engine validates the binding at each site
    /// after the function returns.
    /// </summary>
    void Bind( InterceptorArgument argument );
}

/// <summary>The parameters of an interceptor, as seen by the function that binds the parameters.</summary>
[CompileTime]
[PublicAPI]
[InternalImplement]
public interface IInterceptorParameterBinderList : IReadOnlyList<IInterceptorParameterBinder>
{
    /// <summary>
    /// Gets the parameter at the given position. The list includes the parameter that receives the receiver under the
    /// rules R1 and R3. Under the rules R2 and R4, the receiver is the <c>this</c> of the interceptor, so no parameter
    /// receives it.
    /// </summary>
    new IInterceptorParameterBinder this[ int index ] { get; }

    /// <summary>Gets the parameter with the given name.</summary>
    IInterceptorParameterBinder this[ string name ] { get; }
}

/// <summary>
/// An interceptor method, as seen by the function that binds its parameters. An instance is passed to the <c>bind</c>
/// function of <see cref="InterceptorResult.ExistingMethod"/>, and <see cref="IInterceptorBuilder"/> derives from it.
/// </summary>
/// <remarks>
/// <para>
/// For an existing method, the object wraps the method for one site and delegates to it. <c>ToRef()</c> and comparisons
/// through the declaration comparer give the wrapped method. The object is not reference-equal to the method, as an
/// <c>IMethodBuilder</c> is not reference-equal to the method that it builds.
/// </para>
/// <para>
/// The object is valid only while the function runs. It is frozen afterwards, and every member then throws
/// <see cref="InvalidOperationException"/>, so a stored object cannot be used.
/// </para>
/// </remarks>
[CompileTime]
[PublicAPI]
[InternalImplement]
public interface IInterceptorMethodBinder : IMethod
{
    /// <summary>Gets the intercepted method, as <see cref="MethodInterceptionContext.InterceptedMethod"/> gives it.</summary>
    IMethod InterceptedMethod { get; }

    /// <summary>Gets the parameters of the interceptor, with their bindings.</summary>
    new IInterceptorParameterBinderList Parameters { get; }

    /// <summary>
    /// Gets how the receiver of the site reaches the interceptor. The value is <see cref="InterceptorReceiverMapping.None"/>
    /// at an await site, whose operand is bound through <see cref="InterceptorArgument.Awaitable"/>.
    /// </summary>
    InterceptorReceiverMapping ReceiverMapping { get; }

    /// <summary>
    /// Binds the parameters that are still unbound to the arguments of the site that are still unbound, in order. Each
    /// pair must have identical types, so that an implicit conversion cannot hide two swapped arguments.
    /// </summary>
    void BindRemainingByPosition();
}

/// <summary>One parameter of an interceptor generated from a template, as seen by the <c>configure</c> function.</summary>
[CompileTime]
[PublicAPI]
[InternalImplement]
public interface IInterceptorParameterBuilder : IInterceptorParameterBinder
{
    /// <summary>
    /// Gets or sets the name. Named arguments at the sites are renamed accordingly. The setter throws
    /// <see cref="ArgumentException"/> when the value is not a valid C# identifier.
    /// </summary>
    new string Name { get; set; }

    /// <summary>
    /// Gets or sets the type. For a by-value parameter that receives an argument of the intercepted method, the new type
    /// must be reachable from the original type through an identity, implicit reference, boxing or implicit nullable
    /// conversion. The type of the receiver parameter and of parameters passed by reference cannot change.
    /// </summary>
    new IType Type { get; set; }
}

/// <summary>The parameters of an interceptor generated from a template, as seen by the <c>configure</c> function.</summary>
/// <remarks>
/// The type does not derive from <see cref="IInterceptorParameterBinderList"/>, because the indexers would be
/// ambiguous. <c>IParameterBuilderList</c> does not derive from <c>IParameterList</c> for the same reason.
/// </remarks>
[CompileTime]
[PublicAPI]
[InternalImplement]
public interface IInterceptorParameterBuilderList : IReadOnlyList<IInterceptorParameterBuilder>
{
    /// <summary>Gets the parameter at the given position. See <see cref="IInterceptorParameterBinderList"/>.</summary>
    new IInterceptorParameterBuilder this[ int index ] { get; }

    /// <summary>Gets the parameter with the given name.</summary>
    IInterceptorParameterBuilder this[ string name ] { get; }
}

/// <summary>
/// Adjusts the signature of an interceptor method generated from a template, and binds its parameters. An instance is
/// passed to the <c>configure</c> function of <see cref="InterceptorResult.Template(in MethodTemplateSelector, InterceptorPlacement, Action{IInterceptorBuilder}?, object?, object?, TemplateProvider)"/>
/// and of the template shorthands.
/// </summary>
/// <remarks>
/// <para>
/// When the function is invoked, the builder contains the signature that the engine derived from the call site. The
/// function runs once for each call site, before any template is expanded, at compile time and in the IDE. It must be
/// deterministic. The members that the builder hides, such as <see cref="Name"/>, <see cref="IMemberOrNamedType.IsStatic"/> and
/// <see cref="Parameters"/>, give the current values while the function runs.
/// </para>
/// <para>
/// The adjusted signature is part of the grouping identity: call sites whose adjusted signatures differ get different
/// methods. The bindings are not part of it. The adjusted signature and the bindings are validated with the rules of
/// existing methods. A violation is reported at the call site, and the call site is left unchanged.
/// </para>
/// </remarks>
[CompileTime]
[PublicAPI]
[InternalImplement]
public interface IInterceptorBuilder : IInterceptorMethodBinder
{
    /// <summary>
    /// Gets or sets the requested name. A numeric suffix is added when the name is already used. The setter throws
    /// <see cref="ArgumentException"/> when the value is not a valid C# identifier.
    /// </summary>
    new string Name { get; set; }

    /// <summary>
    /// Gets or sets the accessibility. The value must allow the call site to call the method. It is ignored for a local
    /// function.
    /// </summary>
    new Accessibility Accessibility { get; set; }

    /// <summary>
    /// Gets or sets whether the method is static. Setting <c>true</c> selects
    /// <see cref="InterceptorReceiverMapping.StaticReceiverParameter"/>, or
    /// <see cref="InterceptorReceiverMapping.StaticExtensionReceiver"/> when the placement gives the extension form, when
    /// the call has a receiver. Setting <c>false</c> selects <see cref="InterceptorReceiverMapping.InstanceOnCaller"/>
    /// when the call has a receiver, and keeps <see cref="InterceptorReceiverMapping.None"/> with an instance method of
    /// the calling type or of one of its base types when it has none.
    /// </summary>
    new bool IsStatic { get; set; }

    /// <summary>
    /// Gets or sets the receiver mapping. The engine selects the default. The permitted changes are listed in section
    /// 5.6.8 of the design; any other change is refused with LAMA1014. Setting the value updates <see cref="IsStatic"/>
    /// and adds or removes the receiver parameter in <see cref="Parameters"/> while the function runs.
    /// </summary>
    new InterceptorReceiverMapping ReceiverMapping { get; set; }

    /// <summary>
    /// Gets or sets the return type. The new type must convert implicitly to the return type of the intercepted method.
    /// Inside a conditional access, the type cannot change.
    /// </summary>
    new IType ReturnType { get; set; }

    /// <summary>Gets or sets how omitted optional arguments reach the interceptor (section 6.4.4 of the design).</summary>
    InterceptorDefaultMode DefaultMode { get; set; }

    /// <summary>Gets the parameters: the receiver parameter first, when there is one, then the parameters of the intercepted method, then the added parameters.</summary>
    new IInterceptorParameterBuilderList Parameters { get; }

    /// <summary>
    /// Adds a trailing parameter whose value <paramref name="argument"/> supplies at each site. The parameter has no
    /// default value. The rewritten call passes the value as a named argument (section 5.6.9 of the design).
    /// </summary>
    IInterceptorParameterBuilder AddParameter( string name, IType type, InterceptorArgument argument );
}

/// <summary>How the receiver of the intercepted call reaches the interceptor method (section 6.4.1 of the design).</summary>
[CompileTime]
public enum InterceptorReceiverMapping
{
    /// <summary>Rule R0. The call has no receiver: a static method, an extension method in static form, a C# 14 extension member, or a static abstract member called through a type parameter. Await sites also report this value.</summary>
    None,

    /// <summary>Rule R1. The method is static, and the receiver is bound to one of its parameters, the first one by default.</summary>
    StaticReceiverParameter,

    /// <summary>Rule R1x. As R1, with the receiver as the <c>this</c> parameter of an extension method, which also serves conditional access.</summary>
    StaticExtensionReceiver,

    /// <summary>Rule R2. The method is an instance member of the static type of the receiver or of one of its base types, and it is called on the receiver.</summary>
    InstanceOnReceiver,

    /// <summary>Rule R3. The method is an instance member of the calling type or of one of its base types, called on <c>this</c>, and the receiver is bound to one of its parameters, the first one by default.</summary>
    InstanceOnCaller,

    /// <summary>Rule R4. The call is <c>base.M()</c> to a virtual method, and the method is an instance member of the calling type that calls <c>base.M()</c>.</summary>
    InstanceBaseCall
}

/// <summary>How omitted optional arguments reach a generated interceptor method.</summary>
[CompileTime]
public enum InterceptorDefaultMode
{
    /// <summary>The interceptor declares the defaults of the intercepted method, and omitted arguments stay omitted.</summary>
    Declared,

    /// <summary>The interceptor declares no defaults, and every omitted argument is passed explicitly with the value bound at the source call site.</summary>
    Materialized
}
```

Canonical binding. Every parameter that the function does not bind receives its canonical binding (RC64):

1. The receiver rules R0 to R4 of section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping) decide which parameter receives the receiver, or whether the receiver is the `this` of the interceptor. Under R1, R1x and R3, it is the first parameter unless the function binds `InterceptorArgument.Receiver` to another parameter. At an await site, the single required parameter receives `InterceptorArgument.Awaitable`, and at an accessor site, the value or handler parameter receives `InterceptorArgument.Value`.
2. Every other parameter binds to the argument of the site whose parameter in the intercepted method has the same name. Names are compared with ordinal comparison. Positions are never used.
3. A parameter with a caller-information attribute (`CallerLineNumber`, `CallerFilePath`, `CallerMemberName`, `CallerArgumentExpression`) that no argument of the site binds by name binds to `InterceptorArgument.CallerInfo`, with the parameter named by `CallerArgumentExpression` as `argumentOf`.
4. An unbound optional parameter of an existing method receives its own default value.
5. An unbound required parameter is an error: LAMA1013 with the reason `UnboundParameter`, whose message lists the unbound parameters and the names of the parameters of the site.

A synthesized signature has the names of the target (section [6.4.2](06b-signatures-and-validation.md#642-parameter-names)), so its canonical binding is the positional binding of earlier versions. A parameter that the builder renames keeps its binding.

Positional binding is an explicit opt-in. `BindRemainingByPosition()` pairs the parameters that are still unbound, in order, with the arguments of the site that are still unbound, in the order of the parameters of the intercepted method. Each pair must have identical types; otherwise the site gets LAMA1013 (E9). A parameter left without a pair follows rules 4 and 5. Two reasons exclude positional binding by default: two parameters of the same type can be swapped silently, and different overloads of a target can name their parameters differently.

Type rules (R9). The value of a source must convert implicitly to the type of the parameter, and E10 of section [6.6](06b-signatures-and-validation.md#66-signature-validation-existing-methods-and-adjusted-signatures-r9) decides the cast. `InterceptorArgument.Argument` bound to a `ref`, `out` or `in` parameter requires the same reference kind and an identical type (E9). `InterceptorArgument.Receiver` follows the passing modes of section [6.2.7](06a-call-site-model.md#627-arguments-generic-context-and-passing-mode) for structs (E8). A source of the site (the receiver, an argument, the value, the awaitable) can be bound to at most one parameter; a second binding is refused with LAMA1014. A source that the kind of site does not have, for example `Value` at a method site or `Awaitable` at a method or accessor site, is refused with LAMA1014.

Evaluation. The rewritten call evaluates the receiver and the arguments of the site once, from left to right, as the original call did. When a binding passes them in another order, or drops one, the engine first evaluates them into temporaries: a block in a statement context, and `is var` patterns in an expression context, with the machinery of compound accessor sites (section [6.4.13](06b-signatures-and-validation.md#6413-accessor-sites)). A dropped argument that can have side effects is evaluated into a discard, `_ = F()`. A receiver or an argument without side effects needs no temporary, with the rule of section [6.2.11](06a-call-site-model.md#6211-accessor-sites), step 8. Pulled values and `CallerInstance` are evaluated after the arguments of the site. The argument plan reaches the linker through the redirection request (section [10.4.5](10a-oss-bridge-hook-factory.md#1045-call-site-redirections)).

```csharp
// Source: the target is bool Log( string category, string message ). The existing method is
// static bool Write( string message, string category ), bound by name.
Log( GetCategory(), GetMessage() );
var ok = Log( GetCategory(), GetMessage() );

// Statement context. Write receives its arguments in its own order, so temporaries keep the original order.
{
    var category = GetCategory();
    var message = GetMessage();
    Hooks.Write( message, category );
}

// Expression context.
var ok = GetCategory() is var category && GetMessage() is var message ? Hooks.Write( message, category ) : default!;
```

A `var` pattern always matches and holds a value of any type, including a null reference, so the `default!` branch is unreachable, as in section [6.4.13](06b-signatures-and-validation.md#6413-accessor-sites). The names of the temporaries are reserved with the lexical scope of the host (section [10.4.6](10a-oss-bridge-hook-factory.md#1046-names-and-lexical-scopes)).

Deduplication. Bindings are site data, never part of the group key. Only the signature that `IInterceptorBuilder` shapes is part of it (section [8.2](08-deduplication-and-naming.md#82-the-key)). Existing methods are not grouped.

Validation. After the function returns, `InterceptorSignatureValidator` checks the signature and the binding at the site (section [6.6](06b-signatures-and-validation.md#66-signature-validation-existing-methods-and-adjusted-signatures-r9)). The speculative binding of the rewritten call (E16) checks every site, including the pulled expressions and the availability of `CallerInstance`. The same path serves existing methods and synthesized signatures, so R9 has one implementation.

Rules of version 1 for the builder:

| Member | Rule | Violation |
|---|---|---|
| `Name` | Replaces the `name` parameter of earlier versions of the template factories. The final name follows section [8.7](08-deduplication-and-naming.md#87-names-and-accessibility). | Invalid identifier: `ArgumentException` from the setter. |
| `Accessibility` | At least the level that the call site needs (section [8.7](08-deduplication-and-naming.md#87-names-and-accessibility)). The requested level applies to the whole group, because it is part of the key. | LAMA1013 (E15). |
| `IsStatic`, `ReceiverMapping` | The permitted changes are R2 to R3 when the placement is also in the caller family, R2 to R1, R3 to R1, and R1 to R3 when the caller family holds and `this` is available. For a call without receiver (R0), `IsStatic` can be `false` when the placement is the calling type or one of its base types; the mapping stays `None`. R0 and R4 are fixed. R1 or R1x follows from the placement (section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping)). Setting either member adds or removes the receiver parameter in `Parameters` while the function runs. The change from R1 to R3 replaces `InterceptorBinding.Instance` of earlier versions. | Any other change, including a request of R2 or R4: LAMA1014. Placement outside the caller family, or `this` not available: LAMA1015. |
| `Parameters[i].Type` | By-value parameters that receive an argument only. Identity, implicit reference, boxing or implicit nullable conversion from the type derived from the call site. `meta.Proceed()` casts the parameter back to the original type (section [6.4.9](06b-signatures-and-validation.md#649-proceed-shape)), which exactly reverses these conversions. Numeric and user-defined conversions are refused, because their inverse cast is not always exact: `int` to `float` loses precision, and a user-defined conversion has no guaranteed inverse. | LAMA1014 for a forbidden conversion; LAMA1013 (E10, E16) when a call site does not bind. |
| `ReturnType` | Implicit conversion to the original return type (E14). The template must produce the new type. | LAMA1013 (E14). |
| `Parameters[i].Name` | Unique among the parameters. Named arguments at the call sites are renamed with the `ArgumentNameMap` of the substitution (section [10.5.6](10b-oss-linker-and-templates.md#1056-syntax-of-the-rewritten-call)), and copied attributes that name parameters follow the same map (section [6.4.5](06b-signatures-and-validation.md#645-attributes)). The binding of the parameter does not change. | LAMA1013 (E12). |
| `Parameters[i].Bind` | Any source of the site, and `CallerInstance`, `CallerInfo` and `Pull`. Binding a parameter of the target to another source than its argument drops that argument, and `meta.Proceed()` passes the parameter, which then holds the value of the other source. | LAMA1014 for a source that the site does not have or that is bound twice; LAMA1013 for a type or availability error. |
| `AddParameter` | A trailing parameter with a mandatory source and no default value (section [5.6.9](#569-added-parameters-and-pulled-values)). | A name that is not unique: LAMA1014. |
| `DefaultMode` | `Materialized` is always permitted. `Declared` is permitted only when every default can be declared (section [6.4.4](06b-signatures-and-validation.md#644-defaults-and-params)) and when the group has no added parameter (section [5.6.9](#569-added-parameters-and-pulled-values)). | LAMA1014. |

The permitted receiver-mapping changes are those that the rules of section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping) can implement. A change from R1 or R3 to R2 would require condition R2a, which the engine checks only when it selects the default, and a change of R4 would make the interceptor dispatch virtually and recurse.

Changes that version 1 does not offer, and the reason:

- Changing a default value. The target would silently receive another value than the one that the source code gives it.
- Adding or removing `params` through the builder. In a group with added parameters, the engine itself declares the collection parameter without `params` and packs the arguments (section [5.6.9](#569-added-parameters-and-pulled-values)).
- Removing or reordering parameters. `meta.Proceed()` passes the parameters to the target in their order, so the proceed call would no longer match the target.
- Changing the type of the receiver parameter or of a parameter passed by reference. C# requires identity for `ref`, `out` and `in`, and the proceed call on the receiver would need a cast that the proceed binding does not express.
- A default value for an added parameter. A required parameter needs no default, and a declared default would reintroduce the ordering constraints of optional parameters.

Other properties:

- The functions run in stage 1 (section [8.1](08-deduplication-and-naming.md#81-two-stages)), once per call site. They expand no template.
- They also run at design time, in Phase B (section [9.7.4](09-premium-engine.md#974-phase-b-analyzer)), so their errors appear while the user types.
- They must be deterministic, because the adjusted signature is part of the group key (section [8.2](08-deduplication-and-naming.md#82-the-key)), and because the IDE and the build must agree.
- They are not stored with the result. Only a shorthand registration stores them, which is why the `configure` and `bind` parameters of the shorthands carry `[Durable]` (section [5.3.6](05a-api-registration.md#536-fabric-and-query-surface)).
- `meta.Target` shows the adjusted signature. Run-time template parameters bind by name to the adjusted parameter names (section [5.7.4](05c-api-templates.md#574-template-signature-rules)).
- For an await interceptor, only `Name`, `Accessibility`, `IsStatic`, `AddParameter` and the bindings of the added parameters can change. The parameter that receives the awaitable cannot change. Any other change is reported with LAMA1014.
- For an accessor interceptor, the builder and the receiver mapping apply as for a method (section [6.4.13](06b-signatures-and-validation.md#6413-accessor-sites)), with these restrictions, each reported with LAMA1014. The type of the value parameter of a setter and of the handler parameter of an add or remove accessor cannot change. The return type of a setter, which is the property type, and the `void` return type of an add or remove interceptor cannot change. The return type of a getter can be narrowed, as for a method. The reason for refusing the widening of the value parameter: at a compound, increment or `??=` site, the argument is a value that the rewrite computes and whose type is the property type, and the setter returns that value as the value of the assignment, so a wider parameter would make the returned value and the parameter differ in type. The property is part of the key through the proceed target (section [8.2](08-deduplication-and-naming.md#82-the-key)), so a wider parameter would not let one method serve several properties either. `Name`, `Accessibility`, `IsStatic`, `ReceiverMapping`, the name of the value parameter and `AddParameter` are permitted. At a compound site, both calls pass their own added arguments.
- At a method-reference site, any binding other than the canonical binding of an unchanged signature makes the site use a lambda wrapper (section [5.6.10](#5610-the-callers-instance-and-method-reference-sites)).

Implementation. The builder is a premium internal class, `InterceptorBuilder`, that records the adjustments into the `InterceptorSignature` of section [6.4.11](06b-signatures-and-validation.md#6411-signature-types) and the bindings into the rewrite plan of section [6.4.10](06b-signatures-and-validation.md#6410-rewrite-plan). The binder of an existing method is a premium internal class, `InterceptorMethodBinder`, that wraps the `IMethod` of the method. The builder is an `IMethod` in stage 1, when the `MethodBuilder` of the open-source factory does not exist yet: the factory creates it only in stage 2 (section [10.4.4](10a-oss-bridge-hook-factory.md#1044-synthesized-methods-and-proceed-bindings)). The premium engine therefore implements a lightweight, read-only `IMethod` over the derived signature model, `InterceptorSignatureMethod`, whose parameters, return type and type parameters are resolved in the source compilation. This is implementation work of M2. `InterceptorSignatureBuilder.Apply` later writes the signature into the `IMethodBuilder` callback of `ExtensionTransformationFactory.DeclareMethod` (section [10.4.4](10a-oss-bridge-hook-factory.md#1044-synthesized-methods-and-proceed-bindings)). No new open-source primitive is needed for the builder. The only open-source consequence of the builder is that the proposed `ProceedBinding.WithObjectCasts` becomes `WithArgumentCasts`, which casts an argument to any type, so that `meta.Proceed()` can pass a widened parameter to the target (section [10.4.4](10a-oss-bridge-hook-factory.md#1044-synthesized-methods-and-proceed-bindings)). The argument plan of a binding is an open-source consequence of the binding (section [10.4.5](10a-oss-bridge-hook-factory.md#1045-call-site-redirections)).

Example: an interceptor that receives the line number of each call site without creating one method per line.

```csharp
return InterceptorResult.Template(
    nameof(this.LogCall),
    InterceptorPlacement.CallingType(),
    configure: b =>
    {
        b.Name = "Logged" + b.InterceptedMethod.Name;
        b.AddParameter( "callerLine", TypeFactory.GetType( SpecialType.Int32 ), InterceptorArgument.CallerInfo( CallerInfoKind.LineNumber ) );
    } );

[Template]
private dynamic? LogCall( int callerLine )
{
    Console.WriteLine( $"{meta.MethodInterception.Method.Name} called at line {callerLine}" );

    return meta.Proceed();
}
```

For two calls `Console.WriteLine( "a" )` at lines 12 and 20 of a type, the transformed code is:

```csharp
LoggedWriteLine( "a", callerLine: 12 );
LoggedWriteLine( "a", callerLine: 20 );

private static void LoggedWriteLine( string? value, int callerLine )
{
    Console.WriteLine( $"WriteLine called at line {callerLine}" );
    Console.WriteLine( value );
}
```

The binding of an existing method is shown in section [6.4.1](06b-signatures-and-validation.md#641-receiver-mapping), where `Telemetry.Track` receives the receiver in its second parameter.

#### 5.6.9 Added parameters and pulled values

`IInterceptorBuilder.AddParameter( name, type, argument )` adds a trailing parameter to a synthesized interceptor (decision PO64, RC65). Its value is supplied at each site by its source, typically a pulled value: `InterceptorArgument.Pull( PullAction.UseExpression( expression ) )` or `InterceptorArgument.Pull( PullAction.UseExistingParameter( parameter ) )`. Caller information and `CallerInstance` are other common sources. The same mechanism applies to accessor interceptors and to await interceptors. An existing method has a fixed signature, so its binder has no `AddParameter`: the trailing parameters of an existing method are bound with `Bind`.

Rules:

- The value is site data. Two sites that pass different values share one method, because the source is not part of the key. Only the added parameter, its name and its type are part of the signature.
- `PullAction.None` at a site means that the site cannot supply the value. The site is left unchanged with LAMA1012 and the clause "the value of the parameter '{0}' is not available at this site". A provider that expects this case returns `Skip` instead.
- An expression created with `ExpressionFactory.Parse` is accepted as a pulled value. It is checked only by the speculative binding of E16. The remark of `PullAction.UseExpression` that restricts it to static expressions (FW27 `Advising\PullAction.cs:197-208`) concerns the pull of constructor parameters. At an interception site, the expression is emitted in the origin, so it can use `this` when `InterceptionContext.CanAccessThis` is `true`, and E16 checks it.
- Added parameters have no default value in version 1.
- No registration-level overload takes an `IPullStrategy` in version 1. Section [16.9](16-future-directions.md#169-pull-strategies-at-the-registration-and-at-the-site) sketches it.
- Template binding. A run-time template parameter binds to an added parameter by name only (section [10.6.2](10b-oss-linker-and-templates.md#1062-binder-with-hidden-leading-parameters)).

Groups with added parameters. C# requires optional parameters to follow required ones, and a `params` parameter to be the last one (CS1737, CS0231). An added parameter is required and trailing, so a group that has one changes the parameters of the target:

- `params` is packed. The interceptor declares the collection parameter without `params`. The expanded arguments of the site become a collection expression, `[1, 2, 3]`, with C# 12 or later, and `new T[] { 1, 2, 3 }` for an array before C# 12. An empty expanded form, `M()`, becomes `[]`, or `global::System.Array.Empty<T>()` before C# 12. The allocation must be identical to the one of the expanded form that the compiler emits, including the inline arrays of a `params ReadOnlySpan<T>`. The test `Invocation_PackedParams_SameAllocation` of section [12.8](12-test-plan.md#128-premium-runtime-execution-tests-runtime) verifies this.
- Defaults are materialized: the group uses the `Materialized` mode, so the required added parameters can follow the parameters of the target. The limitation `UnsupportedDefaultValue` still applies.
- Added arguments are appended as named arguments, with names made unique against the parameters of the target.

Example. The target is `void M( A a, int b = 5, params int[] xs )`, the call site `r.M( ... )` is rewritten under R1, and the provider adds `CancellationToken cancellationToken`, pulled from the parameter `ct` of the origin. A positional argument binds to an optional parameter before it binds to a `params` element (section [6.4.4](06b-signatures-and-validation.md#644-defaults-and-params), rule c), so `r.M( a, 1, 2 )` passes `b = 1` and `xs = [2]`:

```csharp
r.M( a, 1, 2 );   // becomes:  I( r, a, 1, [2], cancellationToken: ct );
r.M( a );         // becomes:  I( r, a, 5, [], cancellationToken: ct );

private static void I( C receiver, A a, int b, int[] xs, CancellationToken cancellationToken )
```

Example: a dependency-injection service read from a field of the calling type. The template passes it to a logging helper.

```csharp
var services = context.CallingType.Fields.OfName( "_services" ).SingleOrDefault();

if ( services == null || !context.CanAccessThis )
{
    return InterceptorResult.Skip;
}

return InterceptorResult.Template(
    nameof(this.LogWithServices),
    InterceptorPlacement.GeneratedStaticClass(),
    configure: b => b.AddParameter( "services", services.Type, InterceptorArgument.Pull( PullAction.UseExpression( services ) ) ) );

// A call site in a method of the calling type becomes:
MetalamaInterceptors.Save_Interceptor( repository, order, services: this._services );
```

A field is an `IExpression`, which is emitted as a member access on `this` (FW27 `Code\Invokers\IFieldOrPropertyInvoker.cs:16-21, 36`).

Example: the `CancellationToken` parameter of the origin, passed to an await interceptor.

```csharp
var token = context.Origin is IHasParameters origin
    ? origin.Parameters.FirstOrDefault( p => p.Type.Is( typeof(CancellationToken) ) )
    : null;

if ( token == null || context.IsInNestedFunction )
{
    return InterceptorResult.Skip;
}

return InterceptorResult.Template(
    nameof(this.WithCancellation),
    InterceptorPlacement.CallingType(),
    configure: b => b.AddParameter( "cancellationToken", token.Type, InterceptorArgument.Pull( PullAction.UseExistingParameter( token ) ) ) );

// An await template that stops waiting when the token is canceled. The provider returns it only for awaits of Task<T>.
[Template]
private async AnyAwaitable<dynamic?> WithCancellation( CancellationToken cancellationToken )
{
    return await meta.Proceed().WaitAsync( cancellationToken );
}

// Source, in an async method with the parameter ct:
var order = await this.LoadAsync( id );

// Transformed code.
var order = await Await_Interceptor( this.LoadAsync( id ), cancellationToken: ct );

private static async ValueTask<Order> Await_Interceptor( Task<Order> awaitable, CancellationToken cancellationToken )
{
    return await awaitable.WaitAsync( cancellationToken );
}
```

The name of the added parameter, `cancellationToken`, is the name in the interceptor, and the pulled value is the parameter `ct` of the origin. `Task<TResult>.WaitAsync( CancellationToken )` exists from .NET 6. The template is written with `AnyAwaitable<dynamic?>`, so the engine chooses `ValueTask<Order>` (section [7.9.1](07-await-interception.md#791-accepted-template-shapes)). The interceptor returns another task type than `Task<Order>`, and the original await captured the context, so the rewritten await has no suffix (section [7.5](07-await-interception.md#75-the-adaptive-rewrite-challenge-to-b8-adopted)).

Resolution of pulled parameters (decision PO65, RC66). A pulled parameter always refers to a parameter of the origin member, which is the outermost function that contains the site:

| Origin | Parameters that a pull can reach |
|---|---|
| Method, constructor, operator, conversion operator, finalizer | The parameters of the member. |
| Property, indexer or event accessor | The parameters of the accessor, including the implicit `value` of a setter, `init` accessor, `add` or `remove`. |
| Top-level statements | `args`. |
| Field, property or event initializer | None. |

A pull never refers to a parameter of a lambda, an anonymous method or a local function that encloses the site. `PullAction.UseExistingParameter( p )` emits only the name of `p` (FW27 `Advising\PullAction.cs:119`), so the engine checks that the emitted name, bound at the site, resolves to that exact parameter symbol of the origin. The site is not rewritten, and gets LAMA1013 with the reason `PulledParameterNotAccessible`, in these cases:

- A parameter of a lambda, an anonymous method or a local function that encloses the site has the same name and shadows the parameter of the origin. C# lets such a parameter shadow an outer parameter since C# 8.
- A static lambda or a static local function lies between the parameter and the site, so the parameter cannot be captured.
- The parameter is `ref`, `out` or `in`, and a lambda, an anonymous method or a local function lies between it and the site, so it would be captured (CS1628).

The value is never silently wrong. Rule E16 alone would accept a shadowing parameter of the same type, because the name binds to that parameter. `InterceptionContext.IsInNestedFunction` tells a provider that the site is inside a lambda, an anonymous method or a local function, so that it can skip such sites. An expression created with `ExpressionFactory.Parse` receives only the check of E16.

Limitation of version 1. A provider cannot reach the parameters of a lambda that contains the site. For example, the `CancellationToken` parameter of the handler lambda of an ASP.NET Core minimal API is not a parameter of the origin, which is the method that calls `MapGet`. The SDK gives an advanced route that the engine does not check beyond E16: the provider reads the Roslyn syntax around the site through the SDK accessor of section [10.9](10c-oss-reference-graph-design-time.md#109-small-public-helpers-b2g) (`GetSourceSyntax` of an expression of the context), finds the parameter of the enclosing lambda, and pulls it with `ExpressionFactory.Parse( name, type )`. Section [16.11](16-future-directions.md#1611-functions-and-locals-that-enclose-a-site) sketches the supported form.

#### 5.6.10 The caller's instance and method-reference sites

`InterceptorArgument.CallerInstance` gives the `this` of the origin to an interceptor parameter, for example to a static helper (decision PO62, RC63). It is part of version 1. The rules follow the rules of C# for `this`:

- It is not available in static members, instance field initializers, constructor initializers, static lambdas, static local functions and top-level statements, nor in lambdas and local functions of struct members, which cannot capture `this` (CS1673). It is available exactly when `InterceptionContext.CanAccessThis` is `true`. Otherwise the site gets LAMA1013 with the reason `CallerInstanceNotAvailable`.
- The type of the parameter must accept the calling type through an implicit conversion.
- For a calling type that is a class, the parameter is passed by value.
- For a calling type that is a struct, the helper receives a copy, unless its parameter is `in` or `ref`. A `ref` parameter is possible only when its type is the calling struct type, and only in a member that is not `readonly`, because `this` is then a writable variable. In a `readonly` member, `this` is a readonly variable, so only an `in` parameter or a copy is possible.
- Passing `this` from the body of a constructor hands out an object that is not completely initialized, as user code that does the same.

Method-reference sites (RC69). A method group can carry only the receiver, as `Delegate.Target`, and the arguments of the delegate invocation. Only the canonical binding of an unchanged signature can therefore remain a method group. Any other binding, any added parameter and any signature adjustment other than the name, the accessibility and the receiver mapping make the site use a lambda wrapper, with the policy of PO51. The wrapper is the program that the equivalent lambda form produces: pulled values and caller information are evaluated at each invocation of the delegate, and the receiver, when there is one, is evaluated once when the delegate is created (section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites)).

```csharp
// Target: string Formatter.Format( decimal value ). Existing method: static string Helpers.Transform( decimal value,
// DateTime timestamp ), whose parameter timestamp is bound to a pulled expression: global::System.DateTime.Now.
var texts = prices.Select( Formatter.Format );

// Rewritten: the lambda that the equivalent lambda form would contain.
var texts = prices.Select( ( decimal value ) => global::Helpers.Transform( value, timestamp: global::System.DateTime.Now ) );
```

- The outer lambda of a wrapper whose inner lambda captures a pulled value, `this` or a local cannot be `static`, so the wrapper then omits the `static` modifier.
- An event subscription or unsubscription (`+=` and `-=`) that would need a wrapper gets the limitation `DelegateEqualityRequired`, and the warning LAMA1012 when the provider does not skip it. A wrapper creates a delegate that no other delegate equals, so `-=` could not remove a handler that `+=` added.
- A function-pointer site cannot use a wrapper, so it accepts only the canonical binding of an unchanged signature, and gets the limitation `MethodReferenceReceiverNotSupported` otherwise.
- The documentation states the change of delegate identity: `Delegate.Method` is the lambda of the wrapper, and two delegates created by the same site are never equal (section [6.4.12](06b-signatures-and-validation.md#6412-method-reference-sites)).
