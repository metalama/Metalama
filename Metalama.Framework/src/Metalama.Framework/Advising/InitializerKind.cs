// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Advising
{
    /// <summary>
    /// Specifies the kind of initializer that should be added by an advice operation.
    /// </summary>
    /// <remarks>
    /// This enumeration is used with methods like <see cref="AdviserExtensions.AddInitializer(IAdviser{Code.INamedType}, Code.SyntaxBuilders.IStatement, InitializerKind)"/> to determine
    /// when and where initialization code should be injected into a type's constructors or initialization sequence.
    /// </remarks>
    /// <seealso cref="AdviserExtensions"/>
    /// <seealso cref="IAddInitializerAdviceResult"/>
    /// <seealso href="@initializers"/>
    [CompileTime]
    public enum InitializerKind
    {
        /// <summary>
        /// Indicates that the advice should be executed before any user code in all instance constructors except those that are chained to a constructor of the current class (using the <c>this</c> chaining keyword). The initialization logic executes
        /// after the call to the base constructor.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this when you need to initialize instance state or register instances before any user constructor code runs.
        /// The initialization code runs after <c>: base(...)</c> calls but before any user code in the constructor body.
        /// </para>
        /// <para>
        /// A default constructor will be automatically created if the type does not contain any constructor.
        /// </para>
        /// </remarks>
        /// <seealso href="@initializers"/>
        BeforeInstanceConstructor,

        /// <summary>
        /// Indicates that the advice should be executed before the type constructor (aka static constructor) of the target type. If there is no type constructor, this advice adds one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this when you need to initialize static/type-level state before any user static constructor code runs.
        /// The type constructor is called once when the type is first accessed.
        /// </para>
        /// <para>
        /// The initialization code runs at the beginning of the static constructor, after field initializers.
        /// </para>
        /// </remarks>
        /// <seealso href="@initializers"/>
        BeforeTypeConstructor,

        /// <summary>
        /// Indicates that the advice should run at the end of every instance constructor body, after all
        /// user constructor code. In an inheritance chain it runs only once, in the most-derived layer:
        /// base constructors skip the call so that derived state is fully assigned by the time the template
        /// executes. Constructors chained via <c>: this(...)</c> are skipped just like with
        /// <see cref="BeforeInstanceConstructor"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This kind introduces a virtual <c>OnConstructed(InitializationContext)</c> method on the target
        /// type and injects the template body into it. A call to <c>OnConstructed</c> is appended to the end
        /// of every instance constructor, guarded so that only the most-derived constructor actually invokes
        /// the body — a framework-reserved <see cref="RunTime.Initialization.InitializationSlot.OnConstructed"/>
        /// slot carries the "already handled" signal up the chain.
        /// </para>
        /// <para>
        /// Choose this kind when your initialization logic depends on the constructor body having finished
        /// (e.g. registering the instance in a tracker, starting a background task) but does <em>not</em>
        /// need properties or fields to have been set via an object initializer yet.
        /// If you need those properties to be set first, use <see cref="AfterObjectInitializer"/> instead.
        /// </para>
        /// <para>
        /// The <c>InitializationContext</c> parameter is pulled into constructors via the constructor parameter
        /// introduction mechanism (<see cref="IAdviceFactory.IntroduceParameter(Code.IConstructor, string, Code.IType, Code.TypedConstant, IPullStrategy, System.Collections.Immutable.ImmutableArray{Code.DeclarationBuilders.AttributeConstruction})">IAdviceFactory.IntroduceParameter</see>).
        /// </para>
        /// </remarks>
        /// <seealso href="@initializers"/>
        /// <seealso cref="AfterObjectInitializer"/>
        /// <seealso cref="RunTime.Initialization.InitializationContext"/>
        AfterLastInstanceConstructor,

        /// <summary>
        /// Indicates that the advice should inject statements into the <see cref="RunTime.Initialization.IInitializable.Initialize"/> method of
        /// <see cref="RunTime.Initialization.IInitializable"/> on the target type. If the type does not
        /// already implement <see cref="RunTime.Initialization.IInitializable"/>, the interface and a
        /// virtual <c>Initialize(InitializationContext)</c> method are introduced automatically. 
        /// Metalama rewrites every instrumented <c>new T(...)</c>, <c>new T { ... }</c>, and
        /// <c>with { ... }</c> call site to invoke <see cref="RunTime.Initialization.IInitializable.Initialize"/> after construction, so the template
        /// runs only once properties and fields have been assigned by the object initializer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Choose this kind for cross-cutting concerns that must observe the object in its fully initialized
        /// state — for example, validating property values, computing derived read-only
        /// properties, or wiring back-references between parent and child objects. Unlike
        /// <see cref="AfterLastInstanceConstructor"/>, which runs at the end of the constructor body,
        /// <see cref="AfterObjectInitializer"/> runs <em>after</em> object/collection initializers, so
        /// properties set inline at the call site are visible inside the template.
        /// </para>
        /// <para>
        /// Templates used with this kind may optionally declare an
        /// <see cref="RunTime.Initialization.InitializationContext"/> parameter to inspect caller intent,
        /// metadata (e.g. <see cref="RunTime.Initialization.InitializationMetadata.Modify"/> for
        /// <c>with</c> expressions), or coordination slots passed to the <c>slotFields</c> parameter of
        /// <see cref="IAdviceFactory.AddInitializer(Code.INamedType, string, InitializerKind, object, object, System.Collections.Generic.IEnumerable{Code.IField})">IAdviceFactory.AddInitializer</see>.
        /// </para>
        /// </remarks>
        /// <seealso cref="AfterLastInstanceConstructor"/>
        /// <seealso cref="RunTime.Initialization.IInitializable"/>
        /// <seealso cref="RunTime.Initialization.InitializationContext"/>
        AfterObjectInitializer
    }
}