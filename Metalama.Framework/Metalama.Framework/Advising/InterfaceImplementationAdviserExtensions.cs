// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Advising;

/// <summary>
/// Provides extension methods for the <see cref="IInterfaceImplementationAdviser"/> interface.
/// </summary>
[CompileTime]
[PublicAPI]
public static class InterfaceImplementationAdviserExtensions
{
    /// <summary>
    /// Introduces a property to the target type, or overrides the implementation of an existing one, by specifying a property template.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An <see cref="IInterfaceImplementationAdviser"/>.</param>
    /// <param name="template">The name of the property in the aspect class that will be used as a template for the new property.
    ///     This property must be annotated with <see cref="TemplateAttribute"/>. The type of this property can be either <c>dynamic</c> or any specific
    ///     type. It is possible to dynamically change the type of the introduced property thanks to the <see cref="IPropertyBuilder"/> returned by
    ///     this method.
    /// </param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     property. The default scope depends on the scope of the template property. If the property is static, the introduced property is static. However, if the
    ///     template property is non-static, then the introduced property copies of the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when a property of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildProperty"></param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template property and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IPropertyBuilder"/> that allows to dynamically change the name or type of the introduced property.</returns>
    /// <seealso href="@introducing-members"/>
    public static IIntroductionAdviceResult<IProperty> IntroduceProperty(
        this IInterfaceImplementationAdviser adviser,
        string template,
        IntroductionScope scope = IntroductionScope.Default,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IPropertyBuilder>? buildProperty = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceProperty(
            adviser.Target,
            template,
            scope,
            whenExists,
            buildProperty,
            tags );

    /// <summary>
    /// Introduces a property to the target type, or overrides the implementation of an existing one, by specifying individual template methods for each accessor. 
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An <see cref="IInterfaceImplementationAdviser"/>.</param>
    /// <param name="name">Name of the introduced property.</param>
    /// <param name="getTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the getter, or <c>null</c>
    ///     the introduced property should not have a getter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>T Get()</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the field or property.</param>
    /// <param name="setTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the getter, or <c>null</c>
    ///     if the introduced property should not have a setter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Set(T value</c>  where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the field or property.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     property. The default scope depends on the scope of the template accessors. If the accessors are static, the introduced property is static. However, if the
    ///     template accessors are non-static, then the introduced property copies of the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when a property of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildProperty"></param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IPropertyBuilder"/> that allows to dynamically change the name or type of the introduced property.</returns>
    /// <seealso href="@introducing-members"/>
    public static IIntroductionAdviceResult<IProperty> IntroduceProperty(
        this IInterfaceImplementationAdviser adviser,
        string name,
        string? getTemplate,
        string? setTemplate,
        IntroductionScope scope = IntroductionScope.Default,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IPropertyBuilder>? buildProperty = null,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceProperty(
            adviser.Target,
            name,
            getTemplate,
            setTemplate,
            scope,
            whenExists,
            buildProperty,
            args,
            tags );

    /// <summary>
    /// Introduces an indexer to the target type, or overrides the implementation of an existing one, by specifying individual template methods for each accessor. 
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An <see cref="IInterfaceImplementationAdviser"/>.</param>
    /// <param name="indexType">The type of the initial index parameter.</param>
    /// <param name="getTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the getter, or <c>null</c>
    ///     the introduced indexer should not have a getter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>T Get()</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="setTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the getter, or <c>null</c>
    ///     if the introduced indexer should not have a setter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Set(T value</c>  where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     indexer. The default scope depends on the scope of the template accessors. If the accessors are static, the introduced indexer is static. However, if the
    ///     template accessors are non-static, then the introduced indexer copies of the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when a indexer of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildIndexer"></param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIndexerBuilder"/> that allows to dynamically change the name or type of the introduced indexer.</returns>
    /// <seealso href="@introducing-members"/>
    public static IIntroductionAdviceResult<IIndexer> IntroduceIndexer(
        this IInterfaceImplementationAdviser adviser,
        IType indexType,
        string? getTemplate,
        string? setTemplate,
        IntroductionScope scope = IntroductionScope.Default,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IIndexerBuilder>? buildIndexer = null,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceIndexer(
            adviser.Target,
            indexType,
            getTemplate,
            setTemplate,
            scope,
            whenExists,
            buildIndexer,
            args,
            tags );

    /// <summary>
    /// Introduces an indexer to the target type, or overrides the implementation of an existing one, by specifying individual template methods for each accessor. 
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An <see cref="IInterfaceImplementationAdviser"/>.</param>
    /// <param name="indexType">The type of the initial index parameter.</param>
    /// <param name="getTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the getter, or <c>null</c>
    ///     the introduced indexer should not have a getter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>T Get()</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="setTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the getter, or <c>null</c>
    ///     if the introduced indexer should not have a setter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Set(T value</c>  where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     indexer. The default scope depends on the scope of the template accessors. If the accessors are static, the introduced indexer is static. However, if the
    ///     template accessors are non-static, then the introduced indexer copies of the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when a indexer of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildIndexer"></param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIndexerBuilder"/> that allows to dynamically change the name or type of the introduced indexer.</returns>
    /// <seealso href="@introducing-members"/>
    public static IIntroductionAdviceResult<IIndexer> IntroduceIndexer(
        this IInterfaceImplementationAdviser adviser,
        Type indexType,
        string? getTemplate,
        string? setTemplate,
        IntroductionScope scope = IntroductionScope.Default,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IIndexerBuilder>? buildIndexer = null,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceIndexer(
            adviser.Target,
            indexType,
            getTemplate,
            setTemplate,
            scope,
            whenExists,
            buildIndexer,
            args,
            tags );

    /// <summary>
    /// Introduces an indexer to the target type, or overrides the implementation of an existing one, by specifying individual template methods for each accessor. 
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An <see cref="IInterfaceImplementationAdviser"/>.</param>
    /// <param name="indices">The types and names of the index parameters.</param>
    /// <param name="getTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the getter, or <c>null</c>
    ///     the introduced indexer should not have a getter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>T Get()</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="setTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the getter, or <c>null</c>
    ///     if the introduced indexer should not have a setter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Set(T value</c>  where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     indexer. The default scope depends on the scope of the template accessors. If the accessors are static, the introduced indexer is static. However, if the
    ///     template accessors are non-static, then the introduced indexer copies of the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when a indexer of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildIndexer"></param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIndexerBuilder"/> that allows to dynamically change the name or type of the introduced indexer.</returns>
    /// <seealso href="@introducing-members"/>
    public static IIntroductionAdviceResult<IIndexer> IntroduceIndexer(
        this IInterfaceImplementationAdviser adviser,
        IReadOnlyList<(IType Type, string Name)> indices,
        string? getTemplate,
        string? setTemplate,
        IntroductionScope scope = IntroductionScope.Default,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IIndexerBuilder>? buildIndexer = null,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceIndexer(
            adviser.Target,
            indices,
            getTemplate,
            setTemplate,
            scope,
            whenExists,
            buildIndexer,
            args,
            tags );

    /// <summary>
    /// Introduces an indexer to the target type, or overrides the implementation of an existing one, by specifying individual template methods for each accessor. 
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An <see cref="IInterfaceImplementationAdviser"/>.</param>
    /// <param name="indices">The types and names of the index parameters.</param>
    /// <param name="getTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the getter, or <c>null</c>
    ///     the introduced indexer should not have a getter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>T Get()</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="setTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the getter, or <c>null</c>
    ///     if the introduced indexer should not have a setter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Set(T value</c>  where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     indexer. The default scope depends on the scope of the template accessors. If the accessors are static, the introduced indexer is static. However, if the
    ///     template accessors are non-static, then the introduced indexer copies of the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when a indexer of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildIndexer"></param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIndexerBuilder"/> that allows to dynamically change the name or type of the introduced indexer.</returns>
    /// <seealso href="@introducing-members"/>
    public static IIntroductionAdviceResult<IIndexer> IntroduceIndexer(
        this IInterfaceImplementationAdviser adviser,
        IReadOnlyList<(Type Type, string Name)> indices,
        string? getTemplate,
        string? setTemplate,
        IntroductionScope scope = IntroductionScope.Default,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IIndexerBuilder>? buildIndexer = null,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceIndexer(
            adviser.Target,
            indices,
            getTemplate,
            setTemplate,
            scope,
            whenExists,
            buildIndexer,
            args,
            tags );

    /// <summary>
    /// Introduces a new event to the target type, or overrides the implementation of an existing one, by specifying an event template.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An <see cref="IInterfaceImplementationAdviser"/>.</param>
    /// <param name="eventTemplate">The name of the event in the aspect class that must be used as a template for the introduced event. This event
    ///     must be annotated with <see cref="TemplateAttribute"/>. The type of the template event can be any delegate type. The type of the introduced event
    ///     can be changed dynamically thanks to the <see cref="IEventBuilder"/> returned by this method. 
    /// </param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     event. The default scope depends on the scope of the template event. If the event is static, the introduced event is static. However, if the
    ///     template event is non-static, then the introduced event copies of the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when an event of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildEvent"></param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template event and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IEventBuilder"/> that allows to change the name and the type of the event.</returns>
    /// <seealso href="@introducing-members"/>
    public static IIntroductionAdviceResult<IEvent> IntroduceEvent(
        this IInterfaceImplementationAdviser adviser,
        string eventTemplate,
        IntroductionScope scope = IntroductionScope.Default,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IEventBuilder>? buildEvent = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceEvent(
            adviser.Target,
            eventTemplate,
            scope,
            whenExists,
            buildEvent,
            tags );

    /// <summary>
    /// Introduces a new event to the target type, or overrides the implementation of an existing one, by specifying individual template methods
    /// for the adder, the remover, and the raiser.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An <see cref="IInterfaceImplementationAdviser"/>.</param>
    /// <param name="eventName">The name of the introduced event.</param>
    /// <param name="addTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the adder.
    ///     This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Add(T value)</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the event.</param>
    /// <param name="removeTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the remover.
    ///     This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Add(T value)</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the event.</param>
    /// <param name="raiseTemplate">Not implemented.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     event. The default scope depends on the scope of the template event. If the event is static, the introduced event is static. However, if the
    ///     template event is non-static, then the introduced event copies of the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when an event of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildEvent"></param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IEventBuilder"/> that allows to change the name and the type of the event.</returns>
    /// <seealso href="@introducing-members"/>
    public static IIntroductionAdviceResult<IEvent> IntroduceEvent(
        this IInterfaceImplementationAdviser adviser,
        string eventName,
        string addTemplate,
        string removeTemplate,
        string? raiseTemplate = null,
        IntroductionScope scope = IntroductionScope.Default,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IEventBuilder>? buildEvent = null,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceEvent(
            adviser.Target,
            eventName,
            addTemplate,
            removeTemplate,
            raiseTemplate,
            scope,
            whenExists,
            buildEvent,
            args,
            tags );

    /// <summary>
    /// Introduces a new method or overrides the implementation of the existing one.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An <see cref="IInterfaceImplementationAdviser"/>.</param>
    /// <param name="template">Name of the method of the aspect class that will be used as a template for the introduced method. This method must be
    ///     annotated with <see cref="TemplateAttribute"/>. This method can have parameters and a return type. The actual parameters and return type
    ///     of the introduced method can be modified using the <see cref="IMethodBuilder"/> returned by this method.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     method. The default scope depends on the scope of the template method.
    ///     If the method is static, the introduced method is static. However, if the template method is non-static, then the introduced method
    ///     copies of the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when a method of the same name and signature is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildMethod"></param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property
    ///     of the <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IMethodBuilder"/> that allows to modify the name or signature, or to add custom attributes.</returns>
    /// <seealso href="@introducing-members"/>
    public static IIntroductionAdviceResult<IMethod> IntroduceMethod(
        this IInterfaceImplementationAdviser adviser,
        string template,
        IntroductionScope scope = IntroductionScope.Default,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IMethodBuilder>? buildMethod = null,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceMethod(
            adviser.Target,
            template,
            scope,
            whenExists,
            buildMethod,
            args,
            tags );

    public static IIntroductionAdviceResult<IMethod> IntroduceUnaryOperator(
        this IInterfaceImplementationAdviser adviser,
        string template,
        IType inputType,
        IType resultType,
        OperatorKind kind,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IMethodBuilder>? buildOperator = null,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceUnaryOperator(
            adviser.Target,
            template,
            inputType,
            resultType,
            kind,
            whenExists,
            buildOperator,
            args,
            tags );
}