# NotifyPropertyChanged aspect for Metalama
This document provides implementation overview for PostSharp's `NotifyPropertyChanged` aspect, lists important features, analyses performance,
"nice-to-have" features that were not implemented by the original implementation but requested frequently by users and finally discusses 
possible implementation strategies.

# Current Implementation
`NotifyPropertyChanged` aspect automatically implements `INotifyPropertyChanged` and `INotifyPropertyChanging` interfaces with PropertyChanged
and `PropertyChanging` events.

In contrary to a naive implementation, it also attempts to solve the following:
1) Raise PropertyChanged event only when the object is in a consistent state (i.e. delay the event until the control leaves the object's method).
2) Do not raise PropertyChanged multiple times when changed multiple times within one method (implied by 1).
3) Do not raise PropertyChanged when the value of the property has not changed (optional).
4) Allow subscribers to events to be GC'ed, i.e. weak event semantics (optional).

## Aspect interface
The aspect implements an "internal" interface `INotifyChildPropertyChanged`, which is the main mean of communication between instances of the aspect.

It allows to propagate changes to properties of child objects upstream using `ChildPropertyChanged` event, which carries a "property path", a sequence of property names which describe the source of the change.

```
class A
{
    public B Property { get; set; }
}

class B
{
    public int AnotherProperty { get;set }
}
```

In the above example, object `x` of type `A` can raise `ChildPropertyChanged` with `Property.AnotherProperty` argument, which would mean that `x.Property.AnotherProperty` changed.

```
class C
{
    public A A { get; set; }

    public int YetAnotherProperty => this.A.Property.AnotherProperty;
}
```

An object `c` of class `C` above subscribes to `ChildPropertyChanged` of `c.A` property. When it receives a notification for `Property.AnotherProperty` from `c.A`, it means that `c.A.Property.AnotherProperty` has changed and PropertyChanged should be raised for `c.YetAnotherProperty`.

## Compile-time analysis
The aspect automatically analyses the user code of the target class. The analysis is performed on PostSharp's code model, which builds expressions on top of the IL code. 

However, due to limitations of PostSharp, the analysis is performed only on the original code, all other aspects need to integrate with `NotifyPropertyChanged` aspect using internal interfaces and helpers.

During the analysis phase the aspect attempts to build a list of dependencies for each property:
1) Dependencies on fields, i.e. when field `F` changes, value of property `P` potentially also changes.
```
private int F;
public int P => this.F; // Dependency of P on F.
```
2) Dependencies on other property, i.e. when property `Q` changes, value of the property `P` also changes.
```
public int Q { get; set; }
public int P => this.Q; // Dependency of P on Q.
```
3) Dependencies on properties of other objects, i.e. when property `R` of object stored in property `Q` changes, value of property `P` also changes.
```
public A Q { get; set; }
public int P => this.Q.R; // Dependency of P on Q.R.
```

The analysis is performed only for the particular target type that the aspect is applied on. Analysis result from the base class cannot be used. This has two reasons:
1) There is one aspect instance per CLR type (instance-scoped aspect).
2) There is no simple way of communication between aspect instances in PostSharp (currently also limitation in Metalama).

This results in the "glue" between aspects being deferred to runtime because there is not a simple way to push data between aspect instances at compile-time.

### Limited data flow analysis
The analysis algorithm expands method calls to methods of the target class and does limited data-flow analysis for cases like:

```
public int Property1
{
    get 
    {
        var x = this.Property3;
        return x.UnderlyingProperty; // Dependency on Property3.Underlying is detected.
    }
}

public int Property2
{
    get => this.Foo(); // Dependency on Property4 is detected.
}

public int Foo()
{
    return this.Property4;
}
```

### [SafeForDependencyAnalysis]
Dependency analyzer uses validators that "vote" on expressions and decide, whether the expression is correct, acceptable or wrong. Some expressions have dependencies on non-observable sources of values (such as a static method).

When validators fail when encountering an unsupported expression, the aspect will require user to add `[SafeForDependencyAnalysis]` attribute, which
suppresses these errors. This occurs in following cases:
1) Method accesses a static state.
2) Method calls a method of another object.
3) Method calls a delegate.
4) Method calls a virtual method.
5) Method accesses property of a class that does not implement INotifyPropertyChange and does not have the aspect.
6) Method assigns return value within exception handling block.

Some other validators will "approve" the method, causing the it to be accepted even if there would be other errors. For example, IdempotentMethodValidator will accept method calls to idempotent (pure) methods as their result would never depend on properties that can change. Referring to the above case, idempotent static method is usable and will not create error requiring [SafeForDependencyAnalysis].

**The problem** of having one attribute for the whole method body is that once suppressed, the user may not be aware of another problem with the property after the code changes slightly, unless they remove the attribute.

### Manual dependencies
User can choose and specify a custom dependencies using `Depends.On(this.Property.AnotherProperty)` construct.

However, **using any manual dependency disables the automatic analysis**, leading to a need to specify all the dependencies in the property.

## Evaluation Model
The aspect uses a stack of thread-static contexts. A new context is pushed every time instrumented class method (including accessors) are executed and popped
when the method exits. 

When change of a property of an instrumented object is registered, one of the following happens:
1) If the object is on the top of the stack, the change to the property is stored in an "accumulator".
2) If the object is not on the top of the stack, the change is immediately transformed into events being fired. (this occurs only for publicly accessible fields).

To monitor changes to own fields, all fields are instrumented and their "set value" is monitored (are promoted to properties). When a field change occurs, the aspect retrieves a list of dependent properties and pushes change to these properties to the accumulator.

When a method exits, the one of the following occurs:
1) If the object on the top of the stack does not change after removing the current context, nothing happens and the property changes that were registered are kept in the accumulator. This is a case of
   a method calling another method of the same object.
2) If the object on the top of the stack changes after removing the current context, **all** stored notifications for that object are raised.

### Problems
Method exit behavior results in the following:

```
class A 
{
    public B B {get;set;} // Points to the child.
    public int X {get;set;}
    public int Z {get;set;}

    public void Foo()
    {        
        this.X++;
        this.B.Bar();
    }

    public void Baz()
    {        
        this.Z++;
    }
}

class B
{
    public A Parent {get;set;} // Points to the parent.
    public int Y {get;set;}

    public void Bar()
    {
        this.Y++;
        this.Parent.Baz();
    }
}
```

Evaluation:
1) A.Foo is entered.
2) A.X change is stored.
3) B.Bar is entered.
4) B.Y change is stored.
5) A.Baz is entered.
6) A.Z change is stored.
7) A.Baz is exited, which results in "flushing" changes of A.X and A.Z.
8) B.Bar is exited, which results in "flushing" changes of B.Y.
9) A.Foo is exited, there are no stored changes.

Changes are made in order A.X, B.Y, A.Z, but are reported in order A.X, A.Z, B.Y.

## Features
There are additional features that were either present from the beginning or added in subsequent versions of PostSharp.

The main difference is that with features added later, we had to keep our backward compatibility policies, which limited them or made them optional.

### User implementation and custom handlers (original)
User may implement the interface themselves and define the following method:

```
public void OnPropertyChanged(string propertyName)
{    
    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
```

This will make the aspect use the provided method to fire events. This is often used for dispatching to another thread.

### Manual event raise (original)
User can invoke `NotifyPropertyChangedServices.SignalPropertyChanged(object, string)` to signal a change of property by simulating the property change as processed by an aspect.

### Suspending events (original)
Using `NotifyPropertyChangedServices.SuspendEvents(object)` suspends notifications for the current thread. NotifyPropertyChangedServices.ResumeEvents resumed event notifications,
flushing the accumulated property change notifications.

### Immediate flushing of events for an object (original)
Allows the user to immediately raise event for all stored property changes on a particular object using `NotifyPropertyChangedServices.RaiseEventsImmediate(object)`.

This operation may not have well-defined consequences.

### Support for `INotifyPropertyChanging` (added)
`INotifyPropertyChanging` is an interface that defines `PropertyChanging` event, which signals an incoming change of a property to UI frameworks
that are capable of processing this event.

The aspect raises `PropertyChanging` events immediately when the change of the property is registered for the first time within the context.
At the point the PropertyChanged is being fired, `PropertyChanging` is being also removed from the accumulator.

This means that for each PropertyChanged there should be exactly one `PropertyChanging`.

The support was added when not all platforms supported the interface at the time (e.g. .NET Framework 4.5), the aspect requires the user to provide user implementation of the interface.

### Support for async/iterator methods (added)
The aspect treats "yield" and "await" operations as "points where the object is consistent" (same as method exit). When control
returns to the method, a new context is pushed (same as method entry).

There was no attempt to support async methods that cause the object not to have consistent state until the end of evaluation.

However, objects with inconsistent external state in a multi-threaded environment are not very useful anyway.

### Weak event semantics (added)
By default events prevent the registered object from being collected, because delegates contains a reference to the target object.

NPC aspect uses `WeakEventHandler` structure which allows for keeping strong or weak references to objects.

When weak semantics are enabled, registered delegates are kept alive using `ConditionalWeakTable` hidden in `DelegateReferenceKeeper` type.

It is an optional feature that can be disabled when applying the aspect (see `WeakEventStrategy` property). 

There was a plan to add a "smart" strategy, that would take strong or weak reference based on the delegate added (closure classes).

### [AggregateAllChanges] (added)
Using this attribute on a property will aggregate changes to the content of the property and report it as a change of the property itself.

Because this was not a priority for general use, the feature is limited only to `ICollection` and `ICollection[T]` and works based on a special `Item[]` property notification, therefore being fired when collection content changes.

### Preventing false positives (added)
In some uses, event notification for a property that did not changed is not desirable.

When PreventFalsePositives property is specified on the aspect, it will invoke property getter when it is about to fire the event and test whether it matches the last remembered value.

This has a significant memory cost (effectively duplicating object state within a concurrent dictionary).

## Performance
Performance problems of the current implementation:

### IChildNotifyPropertyChanged interface
Broadcasts changes to the consumer, including "decorated" changes that it received.
    * Consider A(B X, int W => X.Y.Z), B(C Y), C(int Z)
    * Value of A.W depends on C.Z.
    * Evaluation:
        1) C.Z is changed, C fires CNPC event for changed of 'Z' and NPC event for Z.
        2) B receives 'Z' being changed for C and fires CNPC event for 'Y.Z'.
        3) A receives 'Y.Z' being changed for B and fires CNPC event for 'X.Y.Z' and NPC event for 'W'.

Is used internally to "resolve" dependencies within one instance:
    * Consider A(B X, int Y => X.W, int Z => X + Y), B(int W)
    * Evaluation:
        1) B.W is changed, B fires CNPC event for 'W'.
        2) A receives 'W' from 'B' and fires for 'Y'.
        3) A receives 'Y' from itself and fires 'Z'.

In objects graphs and/or in combinations with [AggregateAllChanges] this may result in hundreds of event notifications being made and processed for a single change of property.

### PropertyPath
Originally, the `INotifyChildPropertyChanged` worked based on strings. This meant that string were constantly allocated and parsed and tested for "prefix"

An optimization at that point was `PropertyPath` class, which is based on interned strings, has pre-computed hash, etc.

However, this only reduces the complexity by a constant factor (length of property names can be treated as a constant).

### Deferring work to run-time
The aspect uses too many string-based dictionary lookups for actions that are known at compile-time.

Because of having multiple aspect instances per single object and other limitations of PostSharp, a lot of work is performed at runtime without any ahead-of-time preparation.

# Future Implementation

The future implementation should take only necessary features that are well-defined (or can become well-defined).

## Dropped or significantly changed features

### Validators
Validators are nice abstraction, but lead to a not well-defined behavior because of voting (i.e. earlier validator can "hide" all the errors). There is no particular idea on whether to change this or how, but it should be reconsidered.

### SafeForDependencyAnalysis

[SafeForDependencyAnalysis] should be removed completely or significantly changed (and renamed).

It should not alter behavior for the whole property when only a part of it is wrong. Messages should be reported on an exact span and should be suppressible individually.

## New features (wish list)

### Ahead-of-time
Most of time, there should be a complete information available at build time for a given type, i.e.:
* Dependency lists for properties - what needs to change for the property to change.
* Effect lists for received change - if change of a dependency is detected, which properties are changed.

### Faster internal interface
See performance above and for an idea how it can be approached see the prototype description below.

### Changing the behavior of a single method/property.
When behavior is configurable, it should be configurable for each property separately when it makes sense.

Specifically, often-requested feature is to disable the aspect for a property or for a field.

### Native support for INotifyCollectionChanged
A property may depends on a one or several values of a collection. The interface is giving detailed notification about changes to the collection. Where PropertyChanged firing is done on every change of any item in the collection, CollectionChanged is more granular.

However, it is not clear if this can be supported without extensive runtime structures that are expensive to maintain.

### Custom "dependency handlers"
Extensibility point that would allow users to resolve dependencies for e.g. extension method even when source is not available to the aspect.

### (possible) Analysis of code created by other aspects
At the moment Sdk aspects/services always run before any aspect. Therefore NPC aspect is not able to analyze dependencies 
created by other aspects. This forced us to have "integration" code in each aspect that was integrating with NPC aspect.

Metalama does not currently allow running Sdk aspect after or between framework aspects.

Since all code changes are governed by the aspect linker, which takes the input compilation and injects all transformations, creating and "intermediate compilation"
that is used as a basis for linking, it could also create multiple intermediary compilations, that would contain all injected code.

Sdk aspect could then choose to work on this compilation, instead of the initial one (i.e. being ordered, between framework aspects). 

It could then be supplied information by the linker to be able to analyze the code of expanded templates.

However, this does not seem compatible with design-time pipeline and its intended usage.

### Non-instrumented dependency chains
The current implementation has a limitation for target types of longer dependency chains. For example, if a property depends on `this.Foo.Bar.Quz`, it is expected that Foo uses the aspect (supports INotifyChildPropertyChanged). When both Foo and Foo.Bar are non-instrumented classes manually implementing INPC, the aspect would not receive changes of Quz.

This requires a runtime primitive that is registered to Foo and observes Bar. When the property changes, it unsubscribes and resubscribes the handler on the new value of Bar.

This should be supported on an arbitrary path.

### Analysis results should be available cross-assembly
This is just a reminder that the aspect has to work in an identical way when a derived class of the target is moved to a different assembly. When this happens, all the information about the base class that the aspect requires has to be serialized.

### Dynamic enabling/disabling dependencies
The original aspect define dependencies as a set of "all possible" dependencies. There may be properties (e.g. with large switch statements) that have a large number of possible dependencies but not more than a few at any given point. 

The aspect may (at compile-time) decide that it is worthwhile to track the path execution took during last execution and ignore changes that cannot change the value of the property.

Metalama is quite far from supporting this (requires injecting code into branches etc.). It would be nice to have this option in the future.

### Accepting dependencies from other aspects
Other aspects should be able to add custom dependencies that will be consumed by NotifyPropertyChanged, for example for an introduced property.

# Learning Friday Prototype
Available in PostSharp repo, 'experimental/fastnpc' branch.

LF Prototype attempted to alleviate performance and memory cost of the aspect:
* Removing broadcasting of all changes to all event subscribers and the need to resend all notifications. 
* Analyzing the type as a whole and doing significant work in compile-time.
* Faster and reduced allocation subscription model.
* CPU cache-friendly algorithm (i.e. minimizing usage of hash tables).

## Subscription model
The prototype uses subscription model, where consumer subscribes to receive notification for a specific "source slot" on the target object.

This allows consumers to subscribe to a notification of change of a specific property without the need for dictionary lookup. 

The source slot resolution (determining to which source slot to subscribe to for a given property dependency) can be done either statically or dynamically.

```
class A 
{
    public int B B {get;}
    public int X => B.Z - B.W;
    public int Y => X + B.V;
}

class B
{    
    public int Z {get;set;}
    public int W {get;set;}
    public int V => Z + W;
}
```

Type B has three source slots - slot 0 is fired when B.Z is changed, slot 1 is fired when B.W is changed, slot 2 is fired when B.V is changed. 

When B.Z is assigned, slots 0 and 2 are fired. When B.Y is assigned, slots 1 and 2 are fired.

### Interfaces

There are two interfaces facilitating the subscription model:

```
public interface INpcController
{
    NpcSourceSlot GetSlot(string propertyName);

    ILocationBinding GetPropertyBinding(string propertyName);

    NpcSubscriptionToken Register(NpcSourceSlot sourceSlotId, INpcReceiver target, NpcTargetSlot targetSlotId);

    void Unregister(NpcSubscriptionToken token);
}


public interface INpcReceiver
{
    void ReceiveSlotChange(NpcTargetSlot targetSlotId);
}
```

### Subscribing
When A.B is assigned, the object needs to subscribe to all source slots of B, i.e. slots 0, 1, 2. 

This may look more expensive than adding one handler to an event, but there is no allocation as the subscription record may be a tuple `(INpcReceiver, NpcTargetSlot)` and may be stored in an optimized data structure (shared between slots to save memory and have a better CPU cache performance).

### Intra-type dependencies
One described performance issue of the original implementation was the need to process dependencies between own properties using the same mechanism - raising the event, receiving it, resolving dependent properties, etc.

The prototype creates a "transitive closure" of all dependencies at compile-time:

```
public int _field1;
public int _field2;
public int Foo => this._field1 + this._field2;
public int Bar => this.Foo + this._field3;
public int Quz => this._field3;
```

In the above example, we can determine dependents for each field:

Dependents(_field1) = [ Foo, Bar ];
Dependents(_field2) = [ Foo, Bar ];
Dependents(_field3) = [ Bar, Quz ];

### Inter-type dependencies
The prototype attempts to optimize subscriptions. Consider:
```
public FieldType _field;
public int A => this._field.B;
public int C => this._field.D + this._field.E.F;
public int G => this._field.B + this._field.E.H;

...

public int B {get;set;}
public int D {get;set;}
public EType E {get;set;}

...

public int F {get;set;}
public int H {get;set;}
```

To relay changes when change of a dependency outside of the object is received, the implementation
defines "target slots", which allow it to quickly perform all the work necessary to process a change of a dependency.

The above example has following dependencies:

`A` depends on `_field.B`.
`C` depends on `_field.D` and `_field.E.F`.
`G` depends on `_field.B` and `_field.E.H`.

Presume that `FieldType` has following source slots:
[0] - change of B
[1] - change of D
[2] - change of E

And `EType` has following source slots:
[0] - change of F
[1] - change of H

The aspect for the target type creates following source slots:
[0] - change of A
[1] - change of C
[2] - change of G

When `_field` is set, the following is performed:
  a) If not null:
    1) Unsubscribe.
    2) Subscribe to _field source slot [0] local target slot [0].
    3) Subscribe to _field source slot [1] local target slot [1].
    4) Subscribe to _field source slot [2] local target slot [2].
    5) If _field.E is not null.
      5.1) If previously subscribe, unsubscribe.
      5.2) Subscribe to _field.E source slot [0] local target slot [3].
      5.3) Subscribe to _field.E source slot [1] local target slot [4].
       If null, unsubscribe.
  b) If null, unsubscribe.
  
Target slot handlers:
[0] - changes local source slots [0], [2] (_field.B changes values of A, G).
[1] - changes local source slot [1] (_field.D changes value of C).
[2] - resubscribe as in above 5), change local source slot [1],[2].
[3] - changes local source slot [1]
[4] - changes local source slot [2]

All of the above also should fire `PropertyChanged` if there are any observers.

### Subscribe trees
Performance of the prototype is suboptimal in the following case:

```
this.X.A1.B1
this.X.A1.B2
...
this.X.A1.Bn
...
this.X.Am.B1
this.X.Am.B2
...
this.X.Am.Bn
```

In this case, changing the value of X means resubscribing to `m` objects and `m + m*n` slots, while changing the value of Ai means resubscribing to `n` slots.

The prototype uses "subscribe trees" to have an optimized way of subscribing and resubscribing to slots. Theses are trees that are stored in an array, where resubscribing as a consequence of a property change equates to iterating through a span of the array.

When `X` can change often and `A1.B1 ... Am.Bn` change sparsely, an optional broadcast interface may be more appropriate.

## Metalama adaptation

Most of the data structures that the prototype uses can be completely generated using templates, without a need for runtime data structures.

The "target slot" lookup (i.e. the decision what to do in `ReceiveSlotChange`) can be made into a large switch, which when based on a range of values is transformed into a jump table by the runtime.

## Performance results

Note - there is currently no accumulation, which would worsen some benchmarks somewhat for the prototype.

|                     Test Name |             Mean |
|------------------------------:|-----------------:|
|                Ctor_Prototype |         8.434 us |
|                   Ctor_Legacy |        32.316 us |
|           InnerCtor_Prototype |        15.652 us |
|              InnerCtor_Legacy |        73.569 us |
|      InnerInnerCtor_Prototype |        16.991 us |
|         InnerInnerCtor_Legacy |        77.073 us |
|              Change_Prototype |         9.398 us |
|                 Change_Legacy |        69.122 us |
|         InnerChange_Prototype |        10.005 us |
|            InnerChange_Legacy |        84.177 us |
|    InnerInnerChange_Prototype |         8.834 us |
|       InnerInnerChange_Legacy |        62.672 us |
| HandlerRegistration_Prototype |         8.560 us |
|    HandlerRegistration_Legacy |        31.693 us |
|       HandlerChange_Prototype |         9.581 us |
|          HandlerChange_Legacy |        33.434 us |
|         ManyObjects_Prototype |    16,641.359 us |
|            ManyObjects_Legacy | 1,011,687.737 us |
|  ManyObjects_Random_Prototype |    42,130.257 us |
|     ManyObjects_Random_Legacy | 1,042,402.887 us |