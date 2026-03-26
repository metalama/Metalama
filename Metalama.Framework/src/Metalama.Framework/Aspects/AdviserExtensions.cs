// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Aspects;

[PublicAPI]
[CompileTime]
public static class AdviserExtensions
{
    /// <summary>
    /// Overrides a method's implementation with code from a template.
    /// </summary>
    /// <param name="adviser">An adviser for a method.</param>
    /// <param name="template">
    /// The template that defines the new method implementation. This can be either:
    /// <list type="bullet">
    /// <item>A <see cref="string"/> with the name of a single template method in the aspect class</item>
    /// <item>A <see cref="MethodTemplateSelector"/> to select different templates based on the target method's characteristics (async, iterator, etc.)</item>
    /// </list>
    /// Template methods must be annotated with <see cref="TemplateAttribute"/>.
    /// </param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>, useful for passing context or configuration. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IOverrideAdviceResult{T}"/> providing access to the overridden method declaration.</returns>
    /// <remarks>
    /// <para>
    /// This is the programmatic way to override methods from <see cref="IAspect{T}.BuildAspect"/>. For a simplified approach
    /// when the aspect's only purpose is to override a method, derive from <see cref="OverrideMethodAspect"/> instead.
    /// </para>
    /// <para>
    /// Within the template, use <c>meta.Proceed()</c> to invoke the original method implementation, and <c>meta.Target.Method</c>
    /// to access metadata about the target method (name, parameters, return type, attributes). Access parameter values via
    /// <c>meta.Target.Parameters</c> (e.g., <c>meta.Target.Parameters[0].Value</c> or <c>meta.Target.Parameters["paramName"].Value</c>).
    /// </para>
    /// <para>
    /// For async and iterator methods, the default template automatically adapts <c>meta.Proceed()</c> to match the method's signature
    /// (e.g., adding <c>await</c> for async methods, buffering for iterators). For fine-grained control over async or iterator behavior,
    /// use <see cref="MethodTemplateSelector"/> to specify specialized templates that avoid buffering or enable use of <c>await</c>/<c>yield</c>
    /// in the template code.
    /// </para>
    /// <para>
    /// Use <see cref="IAdviser.With{TNewDeclaration}"/> to override a different method than the current target.
    /// </para>
    /// </remarks>
    /// <seealso cref="OverrideMethodAspect"/>
    /// <seealso cref="MethodTemplateSelector"/>
    /// <seealso href="@overriding-methods"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IOverrideAdviceResult<IMethod> Override(
        this IAdviser<IMethod> adviser,
        in MethodTemplateSelector template,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.Override( adviser.Target, template, args, tags );

    /// <summary>
    /// Introduces a new method to a type, or optionally overrides an existing method with the same signature.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="template">
    /// Name of the template method in the aspect class. This method must be annotated with <see cref="TemplateAttribute"/>.
    /// The template method can have parameters and a return type, which can be further customized using <paramref name="buildMethod"/>.
    /// </param>
    /// <param name="scope">
    /// Determines whether the introduced method is instance or static. Default behavior:
    /// if the template is static, the introduced method is static; otherwise, it matches the target declaration's scope.
    /// </param>
    /// <param name="whenExists">
    /// Strategy when a method with the same name and signature already exists in the target type.
    /// Default is <see cref="OverrideStrategy.Fail"/> (compile-time error). Use <see cref="OverrideStrategy.Override"/>
    /// to replace the existing method, or <see cref="OverrideStrategy.Ignore"/> to skip introduction silently.
    /// </param>
    /// <param name="buildMethod">
    /// Optional delegate to customize the introduced method's characteristics (name, accessibility, modifiers, parameters, etc.)
    /// via <see cref="IMethodBuilder"/>.
    /// </param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> providing access to the introduced method declaration.</returns>
    /// <remarks>
    /// <para>
    /// This is the imperative (programmatic) way to introduce methods from <see cref="IAspect{T}.BuildAspect"/>.
    /// For a declarative approach, use <see cref="IntroduceAttribute"/> on a template method in the aspect class.
    /// </para>
    /// <para>
    /// Use <see cref="IAdviser.With{TNewDeclaration}"/> to introduce the method into a different type than the current target.
    /// </para>
    /// </remarks>
    /// <seealso cref="IntroduceAttribute"/>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@advising-concepts"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IIntroductionAdviceResult<IMethod> IntroduceMethod(
        this IAdviser<INamedType> adviser,
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

    /// <summary>
    /// Introduces a finalizer or overrides the implementation of the existing one.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="template">Name of the method of the aspect class that will be used as a template for the introduced finalizer. This method must be
    ///     annotated with <see cref="TemplateAttribute"/>. This method can have parameters and a return type.</param>
    /// <param name="whenExists">Determines the implementation strategy when a finalizer is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IMethod"/> (finalizer).</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IIntroductionAdviceResult<IMethod> IntroduceFinalizer(
        this IAdviser<INamedType> adviser,
        string template,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceFinalizer(
            adviser.Target,
            template,
            whenExists,
            args,
            tags );

    /// <summary>
    /// Introduces a unary operator to the target type, or overrides the implementation of an existing one.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="template">Name of the method of the aspect class that will be used as a template for the introduced operator. This method must be
    ///     annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="inputType">The type of the input parameter of the operator.</param>
    /// <param name="resultType">The return type of the operator.</param>
    /// <param name="kind">The kind of operator to introduce.</param>
    /// <param name="whenExists">Determines the implementation strategy when an operator of the same kind and signature is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildOperator">An optional delegate to customize the introduced operator via <see cref="IMethodBuilder"/>.</param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IMethod"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    [Obsolete( "Use IntroduceMethod with the buildMethod callback and set IMethodBuilder.OperatorKind instead." )]
    public static IIntroductionAdviceResult<IMethod> IntroduceUnaryOperator(
        this IAdviser<INamedType> adviser,
        string template,
        IType inputType,
        IType resultType,
        OperatorKind kind,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IMethodBuilder>? buildOperator = null,
        object? args = null,
        object? tags = null )
#pragma warning disable CS0618 // Obsolete
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceUnaryOperator(
#pragma warning restore CS0618
            adviser.Target,
            template,
            inputType,
            resultType,
            kind,
            whenExists,
            buildOperator,
            args,
            tags );

    /// <summary>
    /// Introduces a binary operator to the target type, or overrides the implementation of an existing one.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="template">Name of the method of the aspect class that will be used as a template for the introduced operator. This method must be
    ///     annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="leftType">The type of the left operand of the operator.</param>
    /// <param name="rightType">The type of the right operand of the operator.</param>
    /// <param name="resultType">The return type of the operator.</param>
    /// <param name="kind">The kind of operator to introduce.</param>
    /// <param name="whenExists">Determines the implementation strategy when an operator of the same kind and signature is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildOperator">An optional delegate to customize the introduced operator via <see cref="IMethodBuilder"/>.</param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IMethod"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    [Obsolete( "Use IntroduceMethod with the buildMethod callback and set IMethodBuilder.OperatorKind instead." )]
    public static IIntroductionAdviceResult<IMethod> IntroduceBinaryOperator(
        this IAdviser<INamedType> adviser,
        string template,
        IType leftType,
        IType rightType,
        IType resultType,
        OperatorKind kind,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IMethodBuilder>? buildOperator = null,
        object? args = null,
        object? tags = null )
#pragma warning disable CS0618 // Obsolete
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceBinaryOperator(
#pragma warning restore CS0618
            adviser.Target,
            template,
            leftType,
            rightType,
            resultType,
            kind,
            whenExists,
            buildOperator,
            args,
            tags );

    /// <summary>
    /// Introduces a conversion operator to the target type, or overrides the implementation of an existing one.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="template">Name of the method of the aspect class that will be used as a template for the introduced operator. This method must be
    ///     annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="fromType">The type to convert from.</param>
    /// <param name="toType">The type to convert to.</param>
    /// <param name="isImplicit">Determines whether the conversion is implicit (<c>true</c>) or explicit (<c>false</c>). The default value is <c>false</c>.</param>
    /// <param name="isChecked">Determines whether the conversion is checked. The default value is <c>false</c>.</param>
    /// <param name="whenExists">Determines the implementation strategy when an operator of the same signature is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildOperator">An optional delegate to customize the introduced operator via <see cref="IMethodBuilder"/>.</param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IMethod"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    [Obsolete( "Use IntroduceMethod with the buildMethod callback and set IMethodBuilder.OperatorKind instead." )]
    public static IIntroductionAdviceResult<IMethod> IntroduceConversionOperator(
        this IAdviser<INamedType> adviser,
        string template,
        IType fromType,
        IType toType,
        bool isImplicit = false,
        bool isChecked = false,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IMethodBuilder>? buildOperator = null,
        object? args = null,
        object? tags = null )
#pragma warning disable CS0618 // Obsolete
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceConversionOperator(
#pragma warning restore CS0618
            adviser.Target,
            template,
            fromType,
            toType,
            isImplicit,
            isChecked,
            whenExists,
            buildOperator,
            args,
            tags );

    /// <summary>
    /// Overrides the implementation of a constructor.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different constructor than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a constructor.</param>
    /// <param name="template">Name of a template method in the aspect class. This method must be annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IOverrideAdviceResult{T}"/> exposing the overridden <see cref="IConstructor"/>.</returns>
    /// <seealso href="@overriding-constructors"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IOverrideAdviceResult<IConstructor> Override(
        this IAdviser<IConstructor> adviser,
        string template,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.Override(
            adviser.Target,
            template,
            args,
            tags );

    /// <summary>
    /// Introduces a constructor to the target type, or overrides the implementation of an existing one.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="template">Name of the method of the aspect class that will be used as a template for the introduced constructor. This method must be
    ///     annotated with <see cref="TemplateAttribute"/>. The parameters of this method will be copied to the introduced constructor.</param>
    /// <param name="whenExists">Determines the implementation strategy when a constructor of the same signature is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildConstructor">An optional delegate to customize the introduced constructor via <see cref="IConstructorBuilder"/>.</param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IConstructor"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IIntroductionAdviceResult<IConstructor> IntroduceConstructor(
        this IAdviser<INamedType> adviser,
        string template,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IConstructorBuilder>? buildConstructor = null,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceConstructor(
            adviser.Target,
            template,
            whenExists,
            buildConstructor,
            args,
            tags );

    /// <summary>
    /// Overrides a field or property by specifying a property template.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different field or property than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a field or property.</param>
    /// <param name="template">The name of a template property in the aspect class. This property must be annotated with <see cref="TemplateAttribute"/>.
    ///     The template property's accessors (get/set) define which accessors are overridden in the target.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IOverrideAdviceResult{T}"/> exposing the overridden <see cref="IProperty"/>.</returns>
    /// <remarks>
    /// <para>
    /// This is a simplified overload for overriding fields or properties using a template property. For an even simpler approach
    /// when the aspect's only purpose is to override a field or property, use <see cref="OverrideFieldOrPropertyAspect"/> instead.
    /// For fine-grained control over individual accessors (e.g., overriding only the getter), use
    /// <see cref="OverrideAccessors(IAdviser{IFieldOrPropertyOrIndexer}, in GetterTemplateSelector, string?, object?, object?)"/> instead.
    /// </para>
    /// <para>
    /// When applied to a field or auto-property, a backing field is automatically introduced. Within the template property,
    /// access the underlying value via <c>meta.Target.FieldOrProperty.Value</c>.
    /// </para>
    /// </remarks>
    /// <seealso cref="OverrideFieldOrPropertyAspect"/>
    /// <seealso href="@overriding-fields-or-properties"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IOverrideAdviceResult<IProperty> Override(
        this IAdviser<IFieldOrProperty> adviser,
        string template,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.Override(
            adviser.Target,
            template,
            tags );

    /// <summary>
    /// Overrides a field, property, or indexer by specifying templates for the getter and/or setter.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different field, property or indexer than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a field, property or indexer.</param>
    /// <param name="getTemplate">The name of the method of the aspect class whose implementation will be used as a template for the getter, or <c>null</c>
    ///     if the getter should not be overridden. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>T Get()</c> where <c>T</c> is either <c>dynamic</c> or compatible with the field, property or indexer type.
    ///     To select different templates for iterator getters (IEnumerable/IEnumerator), use <see cref="GetterTemplateSelector"/> to avoid buffering.</param>
    /// <param name="setTemplate">The name of the method of the aspect class whose implementation will be used as a template for the setter, or <c>null</c>
    ///     if the setter should not be overridden. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Set(T value)</c> where <c>T</c> is either <c>dynamic</c> or compatible with the field, property or indexer type.</param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IOverrideAdviceResult{T}"/> exposing the overridden <see cref="IPropertyOrIndexer"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method provides fine-grained control over accessor overriding by using separate method templates for getters and setters.
    /// Within templates, use <c>meta.Target.FieldOrProperty.Value</c> (for fields/properties) or <c>meta.Target.Indexer[...].Value</c>
    /// (for indexers) to access the underlying value, and <c>meta.Proceed()</c> to invoke the original accessor.
    /// </para>
    /// <para>
    /// When applied to a field or auto-property, a backing field is automatically introduced.
    /// </para>
    /// </remarks>
    /// <seealso cref="GetterTemplateSelector"/>
    /// <seealso href="@overriding-fields-or-properties"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IOverrideAdviceResult<IPropertyOrIndexer> OverrideAccessors(
        this IAdviser<IFieldOrPropertyOrIndexer> adviser,
        in GetterTemplateSelector getTemplate = default,
        string? setTemplate = null,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.OverrideAccessors(
            adviser.Target,
            getTemplate,
            setTemplate,
            args,
            tags );

    /// <summary>
    /// Overrides a field or property by specifying a method template for the getter, the setter, or both.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another field or property than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a field or property.</param>
    /// <param name="getTemplate">The name of the method of the aspect class whose implementation will be used as a template for the getter, or <c>null</c>
    ///     if the getter should not be overridden. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>T Get()</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the field or property.
    ///     To select different templates for iterator getters, use the constructor of the <see cref="GetterTemplateSelector"/> type. To specify a single
    ///     template for all properties, pass a string.</param>
    /// <param name="setTemplate">The name of the method of the aspect class whose implementation will be used as a template for the setter, or <c>null</c>
    ///     if the setter should not be overridden. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Set(T value)</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the field or property.</param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IOverrideAdviceResult{T}"/> exposing the overridden <see cref="IProperty"/>.</returns>
    /// <seealso href="@overriding-fields-or-properties"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IOverrideAdviceResult<IProperty> OverrideAccessors(
        this IAdviser<IFieldOrProperty> adviser,
        in GetterTemplateSelector getTemplate = default,
        string? setTemplate = null,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.OverrideAccessors(
            adviser.Target,
            getTemplate,
            setTemplate,
            args,
            tags );

    /// <summary>
    /// Overrides an indexer by specifying a method template for the getter, the setter, or both.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another indexer than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for an indexer.</param>
    /// <param name="getTemplate">The name of the method of the aspect class whose implementation will be used as a template for the getter, or <c>null</c>
    ///     if the getter should not be overridden. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>T Get()</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.
    ///     To select a different templates for iterator getters, use the constructor of the <see cref="GetterTemplateSelector"/> type. To specify a single
    ///     template for all indexers, pass a string.
    /// </param>
    /// <param name="setTemplate">The name of the method of the aspect class whose implementation will be used as a template for the setter, or <c>null</c>
    ///     if the setter should not be overridden. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Set(T value)</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IOverrideAdviceResult{T}"/> exposing the overridden <see cref="IIndexer"/>.</returns>
    /// <seealso href="@overriding-fields-or-properties"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IOverrideAdviceResult<IIndexer> OverrideAccessors(
        this IAdviser<IIndexer> adviser,
        in GetterTemplateSelector getTemplate = default,
        string? setTemplate = null,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.OverrideAccessors(
            adviser.Target,
            getTemplate,
            setTemplate,
            args,
            tags );

    /// <summary>
    /// Introduces a field to the target type by specifying a template.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="template">Name of the introduced field.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     field. The default scope is <see cref="IntroductionScope.Instance"/>.</param>
    /// <param name="whenExists">Determines the implementation strategy when a field of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildField">An optional delegate that modifies the <see cref="IFieldBuilder"/> representing the introduced field.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IField"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IIntroductionAdviceResult<IField> IntroduceField(
        this IAdviser<INamedType> adviser,
        string template,
        IntroductionScope scope = IntroductionScope.Default,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IFieldBuilder>? buildField = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceField(
            adviser.Target,
            template,
            scope,
            whenExists,
            buildField,
            tags );

    /// <summary>
    /// Introduces a field to the target type by specifying a field name and <see cref="IType"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike the template-based overload, this method allows you to introduce a field programmatically by specifying
    /// its name and type directly, without needing a template field in the aspect class. The field will be initialized
    /// to its type's default value unless customized via <paramref name="buildField"/>.
    /// </para>
    /// <para>
    /// Use <see cref="IAdviser.With{TNewDeclaration}"/> to introduce the field into a different type than the current target.
    /// </para>
    /// </remarks>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="fieldName">Name of the introduced field.</param>
    /// <param name="fieldType">Type of the introduced field.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     field. The default scope is <see cref="IntroductionScope.Instance"/>.</param>
    /// <param name="whenExists">Determines the implementation strategy when a field of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildField">An optional delegate that modifies the <see cref="IFieldBuilder"/> representing the introduced field.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IField"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IIntroductionAdviceResult<IField> IntroduceField(
        this IAdviser<INamedType> adviser,
        string fieldName,
        IType fieldType,
        IntroductionScope scope = IntroductionScope.Default,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IFieldBuilder>? buildField = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceField(
            adviser.Target,
            fieldName,
            fieldType,
            scope,
            whenExists,
            buildField,
            tags );

    /// <summary>
    /// Introduces a field to the target type by specifying a field name and <see cref="Type"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This overload is identical to the <see cref="IType"/>-based overload, but accepts a <see cref="Type"/> parameter
    /// for convenience when working with reflection types. The field will be initialized to its type's default value
    /// unless customized via <paramref name="buildField"/>.
    /// </para>
    /// <para>
    /// Use <see cref="IAdviser.With{TNewDeclaration}"/> to introduce the field into a different type than the current target.
    /// </para>
    /// </remarks>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="fieldName">Name of the introduced field.</param>
    /// <param name="fieldType">Type of the introduced field.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     field. The default scope is <see cref="IntroductionScope.Instance"/>.</param>
    /// <param name="whenExists">Determines the implementation strategy when a field of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildField">An optional delegate that modifies the <see cref="IFieldBuilder"/> representing the introduced field.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IField"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IIntroductionAdviceResult<IField> IntroduceField(
        this IAdviser<INamedType> adviser,
        string fieldName,
        Type fieldType,
        IntroductionScope scope = IntroductionScope.Default,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IFieldBuilder>? buildField = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceField(
            adviser.Target,
            fieldName,
            fieldType,
            scope,
            whenExists,
            buildField,
            tags );

    /// <summary>
    /// Introduces an auto-implemented property to the target type by specifying a property name and <see cref="Type"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method introduces a simple auto-implemented property (similar to <c>public string Name { get; set; }</c>)
    /// without requiring a template. The property will have both a getter and setter with automatic backing field management.
    /// Use <paramref name="buildProperty"/> to customize accessibility or other characteristics.
    /// </para>
    /// <para>
    /// Use <see cref="IAdviser.With{TNewDeclaration}"/> to introduce the property into a different type than the current target.
    /// </para>
    /// </remarks>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="propertyName">Name of the introduced property.</param>
    /// <param name="propertyType">Type of the introduced property.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     property. The default scope is <see cref="IntroductionScope.Instance"/>.</param>
    /// <param name="whenExists">Determines the implementation strategy when a property of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildProperty">An optional delegate that modifies the <see cref="IPropertyBuilder"/> representing the introduced property.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IProperty"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IIntroductionAdviceResult<IProperty> IntroduceAutomaticProperty(
        this IAdviser<INamedType> adviser,
        string propertyName,
        Type propertyType,
        IntroductionScope scope = IntroductionScope.Default,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IPropertyBuilder>? buildProperty = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceAutomaticProperty(
            adviser.Target,
            propertyName,
            propertyType,
            scope,
            whenExists,
            buildProperty,
            tags );

    /// <summary>
    /// Introduces an auto-implemented property to the target type by specifying a property name and <see cref="IType"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This overload is identical to the <see cref="Type"/>-based overload, but accepts an <see cref="IType"/> parameter
    /// for working with Metalama's code model types. The property will be auto-implemented with both getter and setter.
    /// </para>
    /// <para>
    /// Use <see cref="IAdviser.With{TNewDeclaration}"/> to introduce the property into a different type than the current target.
    /// </para>
    /// </remarks>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="propertyName">Name of the introduced property.</param>
    /// <param name="propertyType">Type of the introduced property.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     property. The default scope is <see cref="IntroductionScope.Instance"/>.</param>
    /// <param name="whenExists">Determines the implementation strategy when a property of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildProperty">An optional delegate that modifies the <see cref="IPropertyBuilder"/> representing the introduced property.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IProperty"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IIntroductionAdviceResult<IProperty> IntroduceAutomaticProperty(
        this IAdviser<INamedType> adviser,
        string propertyName,
        IType propertyType,
        IntroductionScope scope = IntroductionScope.Default,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<IPropertyBuilder>? buildProperty = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceAutomaticProperty(
            adviser.Target,
            propertyName,
            propertyType,
            scope,
            whenExists,
            buildProperty,
            tags );

    /// <summary>
    /// Introduces a property to the target type, or overrides the implementation of an existing one, by specifying a property template.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="template">The name of the property in the aspect class that will be used as a template for the new property.
    ///     This property must be annotated with <see cref="TemplateAttribute"/>. The type of this property can be either <c>dynamic</c> or any specific
    ///     type. It is possible to dynamically change the type of the introduced property thanks to the <see cref="IPropertyBuilder"/> returned by
    ///     this method.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     property. The default scope depends on the scope of the template property. If the template property is static, the introduced property is static. However, if the
    ///     template property is non-static, then the introduced property copies the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when a property of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildProperty">An optional delegate that modifies the <see cref="IPropertyBuilder"/> representing the introduced property.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IProperty"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IIntroductionAdviceResult<IProperty> IntroduceProperty(
        this IAdviser<INamedType> adviser,
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
    /// Introduces a property to the target type by specifying individual template methods for each accessor (getter and setter).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike the overload that takes a template property, this method allows you to specify separate template methods
    /// for the getter and setter, providing more flexibility in property implementation. This is useful when you need
    /// different logic for reading and writing the property value.
    /// </para>
    /// <para>
    /// Use <see cref="IAdviser.With{TNewDeclaration}"/> to introduce the property into a different type than the current target.
    /// </para>
    /// </remarks>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="name">Name of the introduced property.</param>
    /// <param name="getTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the getter, or <c>null</c>
    ///     if the introduced property should not have a getter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>T Get()</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the property.</param>
    /// <param name="setTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the setter, or <c>null</c>
    ///     if the introduced property should not have a setter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Set(T value)</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the property.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     property. The default scope depends on the scope of the template accessors. If the accessors are static, the introduced property is static. However, if the
    ///     template accessors are non-static, then the introduced property copies the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when a property of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildProperty">An optional delegate that modifies the <see cref="IPropertyBuilder"/> representing the introduced property.</param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IProperty"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IIntroductionAdviceResult<IProperty> IntroduceProperty(
        this IAdviser<INamedType> adviser,
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
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method introduces an indexer with a single index parameter. Use the overload that accepts <c>IReadOnlyList&lt;(IType, string)&gt;</c>
    /// for indexers with multiple index parameters.
    /// </para>
    /// <para>
    /// Use <see cref="IAdviser.With{TNewDeclaration}"/> to introduce the indexer into a different type than the current target.
    /// </para>
    /// </remarks>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="indexType">The type of the initial index parameter.</param>
    /// <param name="getTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the getter, or <c>null</c>
    ///     if the introduced indexer should not have a getter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>T Get()</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="setTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the setter, or <c>null</c>
    ///     if the introduced indexer should not have a setter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Set(T value)</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     indexer. The default scope depends on the scope of the template accessors. If the accessors are static, the introduced indexer is static. However, if the
    ///     template accessors are non-static, then the introduced indexer copies the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when an indexer of the same signature is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildIndexer">An optional delegate that modifies the <see cref="IIndexerBuilder"/> representing the introduced indexer.</param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IIndexer"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IIntroductionAdviceResult<IIndexer> IntroduceIndexer(
        this IAdviser<INamedType> adviser,
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
    /// </summary>
    /// <remarks>
    /// <para>
    /// This overload is identical to the <see cref="IType"/>-based overload, but accepts a <see cref="Type"/> parameter
    /// for convenience when working with reflection types. This introduces an indexer with a single index parameter.
    /// </para>
    /// <para>
    /// Use <see cref="IAdviser.With{TNewDeclaration}"/> to introduce the indexer into a different type than the current target.
    /// </para>
    /// </remarks>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="indexType">The type of the initial index parameter.</param>
    /// <param name="getTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the getter, or <c>null</c>
    ///     if the introduced indexer should not have a getter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>T Get()</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="setTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the setter, or <c>null</c>
    ///     if the introduced indexer should not have a setter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Set(T value)</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     indexer. The default scope depends on the scope of the template accessors. If the accessors are static, the introduced indexer is static. However, if the
    ///     template accessors are non-static, then the introduced indexer copies the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when an indexer of the same signature is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildIndexer">An optional delegate that modifies the <see cref="IIndexerBuilder"/> representing the introduced indexer.</param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IIndexer"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IIntroductionAdviceResult<IIndexer> IntroduceIndexer(
        this IAdviser<INamedType> adviser,
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
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method allows you to introduce indexers with multiple index parameters (e.g., <c>this[int row, int column]</c>).
    /// For indexers with a single parameter, you can use the simpler overload that accepts a single <see cref="IType"/>.
    /// </para>
    /// <para>
    /// Use <see cref="IAdviser.With{TNewDeclaration}"/> to introduce the indexer into a different type than the current target.
    /// </para>
    /// </remarks>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="indices">The types and names of the index parameters.</param>
    /// <param name="getTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the getter, or <c>null</c>
    ///     if the introduced indexer should not have a getter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>T Get()</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="setTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the setter, or <c>null</c>
    ///     if the introduced indexer should not have a setter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Set(T value)</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     indexer. The default scope depends on the scope of the template accessors. If the accessors are static, the introduced indexer is static. However, if the
    ///     template accessors are non-static, then the introduced indexer copies the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when an indexer of the same signature is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildIndexer">An optional delegate that modifies the <see cref="IIndexerBuilder"/> representing the introduced indexer.</param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IIndexer"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IIntroductionAdviceResult<IIndexer> IntroduceIndexer(
        this IAdviser<INamedType> adviser,
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
    /// </summary>
    /// <remarks>
    /// <para>
    /// This overload is identical to the <c>IReadOnlyList&lt;(IType, string)&gt;</c>-based overload, but accepts <see cref="Type"/> parameters
    /// for convenience when working with reflection types. This method allows indexers with multiple index parameters.
    /// </para>
    /// <para>
    /// Use <see cref="IAdviser.With{TNewDeclaration}"/> to introduce the indexer into a different type than the current target.
    /// </para>
    /// </remarks>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="indices">The types and names of the index parameters.</param>
    /// <param name="getTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the getter, or <c>null</c>
    ///     if the introduced indexer should not have a getter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>T Get()</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="setTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the setter, or <c>null</c>
    ///     if the introduced indexer should not have a setter. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Set(T value)</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the indexer.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     indexer. The default scope depends on the scope of the template accessors. If the accessors are static, the introduced indexer is static. However, if the
    ///     template accessors are non-static, then the introduced indexer copies the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when an indexer of the same signature is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildIndexer">An optional delegate that modifies the <see cref="IIndexerBuilder"/> representing the introduced indexer.</param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IIndexer"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IIntroductionAdviceResult<IIndexer> IntroduceIndexer(
        this IAdviser<INamedType> adviser,
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
    /// Overrides an event by specifying a template for the adder, the remover, and/or the invocation interception.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to override a different event than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for an event.</param>
    /// <param name="addTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the adder, or <c>null</c>
    ///     if the adder should not be overridden. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Add(T value)</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the event.</param>
    /// <param name="removeTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the remover, or <c>null</c>
    ///     if the remover should not be overridden. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Remove(T value)</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the event.</param>
    /// <param name="raiseTemplate">Reserved for future use (not implemented).</param>
    /// <param name="invokeTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for intercepting invocation of
    ///     the event's handlers. This template is invoked once per event handler when the event is raised. The signature of this method must be
    ///     <c>T Invoke()</c>, <c>T Invoke(U handler)</c>, or <c>T Invoke(U handler, V1 param1, V2 param2, ...)</c> where <c>T</c> is either <c>dynamic</c> or
    ///     a type compatible with the return value of the event's delegate type, <c>U</c> is either <c>dynamic</c> or the event's delegate type,
    ///     <c>Vn</c> are types matching the delegate's parameters. Use <c>meta.Proceed()</c> within the template to invoke the actual handler.</param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IOverrideAdviceResult{T}"/> exposing the overridden <see cref="IEvent"/>.</returns>
    /// <remarks>
    /// <para>
    /// This is the programmatic way to override events from <see cref="IAspect{T}.BuildAspect"/>. For a simplified approach
    /// when the aspect's only purpose is to override an event, derive from <see cref="OverrideEventAspect"/> instead.
    /// </para>
    /// <para>
    /// When applied to a field-like event, it is automatically transformed into an explicitly implemented event with a backing field,
    /// similar to how auto-properties are transformed.
    /// </para>
    /// <para>
    /// The <paramref name="invokeTemplate"/> provides powerful per-handler interception capabilities. Since it's invoked once per handler,
    /// you can implement patterns like exception handling with handler removal, async execution, or handler filtering. Within the template,
    /// access event metadata via <c>meta.Target.Event</c> and programmatically add/remove handlers using <c>meta.Target.Event.Add(handler)</c>
    /// and <c>meta.Target.Event.Remove(handler)</c>.
    /// </para>
    /// <para>
    /// <strong>Performance Note:</strong> Overriding the invoke operation uses an <see cref="Metalama.Framework.RunTime.Events.EventBroker{TImplementation, THandler, TArgs}"/>
    /// pattern which adds memory overhead (one broker instance per event per object instance) and allocates memory during event invocation.
    /// This may affect performance for high-frequency events.
    /// </para>
    /// <para>
    /// <strong>Limitations:</strong> Delegate signatures with non-<c>void</c> return types or with <c>ref</c>/<c>out</c> parameters are not
    /// supported. Only handlers added through the event's add/remove accessors will be intercepted.
    /// </para>
    /// </remarks>
    /// <seealso cref="OverrideEventAspect"/>
    /// <seealso href="@overriding-events"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IOverrideAdviceResult<IEvent> OverrideAccessors(
        this IAdviser<IEvent> adviser,
        string? addTemplate = null,
        string? removeTemplate = null,
        string? raiseTemplate = null,
        string? invokeTemplate = null,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.OverrideAccessors(
            adviser.Target,
            addTemplate,
            removeTemplate,
            invokeTemplate,
            raiseTemplate,
            args,
            tags );

    /// <summary>
    /// Introduces a new event to the target type, or overrides the implementation of an existing one, by specifying an event template.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to introduce the event to a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="eventTemplate">The name of the event in the aspect class that must be used as a template for the introduced event. This event
    ///     must be annotated with <see cref="TemplateAttribute"/>. The type of the template event can be any delegate type. The type of the introduced event
    ///     can be changed dynamically thanks to the <see cref="IEventBuilder"/> returned by this method.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     event. The default scope depends on the scope of the template event. If the template event is static, the introduced event is static. However, if the
    ///     template event is non-static, then the introduced event copies the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when an event of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildEvent">An optional delegate that modifies the <see cref="IEventBuilder"/> representing the introduced event.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IEvent"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IIntroductionAdviceResult<IEvent> IntroduceEvent(
        this IAdviser<INamedType> adviser,
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
    /// for the adder and the remover, and optionally an invocation interception template.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike the overload that takes a template event, this method allows you to specify separate template methods
    /// for the add and remove accessors (and optionally an invoke template), providing complete control over the event's behavior.
    /// This is useful when you need custom logic for event subscription/unsubscription or handler invocation.
    /// </para>
    /// <para>
    /// Use <see cref="IAdviser.With{TNewDeclaration}"/> to introduce the event into a different type than the current target.
    /// </para>
    /// </remarks>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="eventName">The name of the introduced event.</param>
    /// <param name="addTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the adder.
    ///     This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Add(T value)</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the event.</param>
    /// <param name="removeTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the remover.
    ///     This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Remove(T value)</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the event.</param>
    /// <param name="invokeTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for intercepting invocation of 
    ///     the event's handlers. The signature of this method must be <c>T Invoke()</c>, <c>T Invoke(U handler)</c>, or <c>T Invoke(U handler, V1 param1, V2 param2, ...)</c>  
    ///     where <c>T</c> is either <c>dynamic</c> or a type compatible with the return value of the event's delegate type, <c>U</c> is either <c>dynamic</c> or the event's 
    ///     delegate type, <c>Vn</c> are types matching the delegate's parameters.</param>
    /// <param name="raiseTemplate">Reserved for future use (not implemented).</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     event. The default scope depends on the scope of the template event. If the template event is static, the introduced event is static. However, if the
    ///     template event is non-static, then the introduced event copies the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when an event of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildEvent">An optional delegate that modifies the <see cref="IEventBuilder"/> representing the introduced event.</param>
    /// <param name="args">An optional object (typically of anonymous type) whose properties map to template method parameters or type parameters. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IEvent"/>.</returns>
    /// <seealso href="@introducing-members"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IIntroductionAdviceResult<IEvent> IntroduceEvent(
        this IAdviser<INamedType> adviser,
        string eventName,
        string addTemplate,
        string removeTemplate,
        string? invokeTemplate = null,
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
            invokeTemplate,
            raiseTemplate,
            scope,
            whenExists,
            buildEvent,
            args,
            tags );

    /// <summary>
    /// Makes a type implement a new interface specified as an <see cref="INamedType"/>.
    /// Interface members can be introduced declaratively by marking an aspect member with <see cref="InterfaceMemberAttribute"/> or
    /// <see cref="IntroduceAttribute"/>, or programmatically using introduction methods.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to implement the interface on a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="interfaceType">The type of the implemented interface.</param>
    /// <param name="whenExists">Determines the implementation strategy when the interface is already implemented by the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to <see cref="InterfaceMemberAttribute"/> templates and accessible via <c>meta.Tags</c>. This parameter does not affect members introduced using <see cref="IntroduceAttribute"/> or programmatically. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IImplementInterfaceAdviceResult"/> exposing the implementation operation.</returns>
    /// <seealso href="@implementing-interfaces"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IImplementInterfaceAdviceResult ImplementInterface(
        this IAdviser<INamedType> adviser,
        INamedType interfaceType,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.ImplementInterface(
            adviser.Target,
            interfaceType,
            whenExists,
            tags );

    /// <summary>
    /// Makes a type implement a new interface specified as a reflection <see cref="Type"/>.
    /// Interface members can be introduced declaratively by marking an aspect member with <see cref="InterfaceMemberAttribute"/>,
    /// <see cref="IntroduceAttribute"/> or programmatically using introduction methods.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to implement the interface on a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="interfaceType">The type of the implemented interface.</param>
    /// <param name="whenExists">Determines the implementation strategy when the interface is already implemented by the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to <see cref="InterfaceMemberAttribute"/> templates and accessible via <c>meta.Tags</c>. This parameter does not affect members introduced using <see cref="IntroduceAttribute"/> or programmatically. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IImplementInterfaceAdviceResult"/> exposing the implementation operation.</returns>
    /// <seealso href="@implementing-interfaces"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IImplementInterfaceAdviceResult ImplementInterface(
        this IAdviser<INamedType> adviser,
        Type interfaceType,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.ImplementInterface(
            adviser.Target,
            interfaceType,
            whenExists,
            tags );

    /// <summary>
    /// Adds a type or instance initializer by using a template.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to add the initializer to a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="template">The name of the template. This method must have no run-time parameter, be of <c>void</c> return type, and be annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="kind">The type of initializer to add.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IAddInitializerAdviceResult"/> exposing the added initializer.</returns>
    /// <remarks>
    /// <para>
    /// This is the programmatic way to add initializers from <see cref="IAspect{T}.BuildAspect"/>.
    /// Initializers inject code before any user constructor code runs (for <see cref="InitializerKind.BeforeInstanceConstructor"/>)
    /// or at the beginning of the static constructor (for <see cref="InitializerKind.BeforeTypeConstructor"/>).
    /// </para>
    /// <para>
    /// For <see cref="InitializerKind.BeforeInstanceConstructor"/>, the initializer will NOT affect constructors that chain
    /// to another constructor using <c>: this(...)</c>. It always runs before any constructor of the current class, but after
    /// the call to the <c>: base(...)</c> constructor.
    /// </para>
    /// <para>
    /// A default constructor will be created automatically if the type does not contain any constructor and
    /// <see cref="InitializerKind.BeforeInstanceConstructor"/> is used.
    /// </para>
    /// </remarks>
    /// <seealso cref="InitializerKind"/>
    /// <seealso href="@initializers"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IAddInitializerAdviceResult AddInitializer(
        this IAdviser<INamedType> adviser,
        string template,
        InitializerKind kind,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.AddInitializer(
            adviser.Target,
            template,
            kind,
            tags,
            args );

    /// <summary>
    /// Adds a type or instance initializer by specifying an <see cref="IStatement"/>.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to add the initializer to a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="statement">The statement to be inserted at the top of constructors.</param>
    /// <param name="kind">The type of initializer to add.</param>
    /// <returns>An <see cref="IAddInitializerAdviceResult"/> exposing the added initializer.</returns>
    /// <remarks>
    /// This overload allows you to inject a programmatically constructed statement instead of using a template.
    /// See <see cref="AddInitializer(IAdviser{INamedType}, string, InitializerKind, object?, object?)"/> for more details on initialization timing.
    /// </remarks>
    /// <seealso cref="InitializerKind"/>
    /// <seealso href="@initializers"/>
    public static IAddInitializerAdviceResult AddInitializer(
        this IAdviser<INamedType> adviser,
        IStatement statement,
        InitializerKind kind )
        => ((IAdviserInternal) adviser).AdviceFactory.AddInitializer(
            adviser.Target,
            statement,
            kind );

    /// <summary>
    /// Adds an initializer to a specific constructor by using a template.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to add the initializer to a different constructor than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a constructor.</param>
    /// <param name="template">The name of the template. This method must have no run-time parameter, be of <c>void</c> return type, and be annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IAddInitializerAdviceResult"/> exposing the added initializer.</returns>
    /// <remarks>
    /// <para>
    /// This overload allows you to target a specific constructor, including constructors that chain to another
    /// constructor using <c>: this(...)</c>. The initialization code runs at the beginning of the constructor,
    /// after the <c>: base(...)</c> or <c>: this(...)</c> call.
    /// </para>
    /// </remarks>
    /// <seealso href="@initializers"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IAddInitializerAdviceResult AddInitializer(
        this IAdviser<IConstructor> adviser,
        string template,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.AddInitializer(
            adviser.Target,
            template,
            tags,
            args );

    /// <summary>
    /// Adds an initializer to a specific constructor by specifying an <see cref="IStatement"/>.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to add the initializer to a different constructor than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a constructor.</param>
    /// <param name="statement">The statement to be inserted at the top of the constructor.</param>
    /// <returns>An <see cref="IAddInitializerAdviceResult"/> exposing the added initializer.</returns>
    /// <remarks>
    /// This overload allows you to inject a programmatically constructed statement to a specific constructor.
    /// See <see cref="AddInitializer(IAdviser{IConstructor}, string, object?, object?)"/> for initialization timing details.
    /// </remarks>
    /// <seealso href="@initializers"/>
    public static IAddInitializerAdviceResult AddInitializer(
        this IAdviser<IConstructor> adviser,
        IStatement statement )
        => ((IAdviserInternal) adviser).AdviceFactory.AddInitializer(
            adviser.Target,
            statement );

    /// <summary>
    /// Adds a contract to a parameter. Contracts validate or normalize parameter values at different points in the data flow,
    /// typically used for precondition checks (input parameters) or postcondition checks (output parameters and return values).
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to add the contract to a different parameter than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a parameter.</param>
    /// <param name="template">The name of the template method. This method must have a single run-time parameter named <c>value</c> and be annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="direction">Direction of the data flow to which the contract should apply. Use <see cref="ContractDirection.Input"/> for preconditions,
    /// <see cref="ContractDirection.Output"/> for postconditions, or <see cref="ContractDirection.Default"/> to select automatically based on parameter type. See <see cref="ContractDirection"/> for details.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IAddContractAdviceResult{T}"/> exposing the added contract.</returns>
    /// <remarks>
    /// <para>
    /// <b>Template Access:</b> Within the template, use <c>meta.Target.Expression</c> for unified access to the target as an <see cref="IExpression"/>,
    /// <c>meta.Target.Parameter</c> for parameter-specific access, and <c>meta.Target.ContractDirection</c> to determine whether you're validating input or output.
    /// </para>
    /// <para>
    /// <b>Performance Note:</b> When possible, provide all contracts to the same method from a single aspect. This approach yields better compile-time performance than using several separate aspects.
    /// </para>
    /// <para>
    /// <b>Ready-Made Contracts:</b> Consider using the <c>Metalama.Patterns.Contracts</c> package, which provides ready-made contract attributes for common validation scenarios
    /// (nullability, strings, numeric ranges, enums, collections). See <see href="@contract-patterns"/> for details.
    /// </para>
    /// </remarks>
    /// <seealso cref="ContractAspect"/>
    /// <seealso cref="IMetaTarget.Expression"/>
    /// <seealso href="@contracts"/>
    /// <seealso href="@contract-patterns"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IAddContractAdviceResult<IParameter> AddContract(
        this IAdviser<IParameter> adviser,
        string template,
        ContractDirection direction = ContractDirection.Default,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.AddContract(
            adviser.Target,
            template,
            direction,
            tags,
            args );

    /// <summary>
    /// Adds a contract to a field, property or indexer. Contracts validate or normalize values, typically used to validate
    /// assigned values (setters) or returned values (getters). Fields with contracts are automatically transformed into properties.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to add the contract to a different field, property or indexer than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a field, property or indexer.</param>
    /// <param name="template">The name of the template method. This method must have a single run-time parameter named <c>value</c> and be annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="direction">Direction of the data flow to which the contract should apply. Use <see cref="ContractDirection.Input"/> to validate values being assigned (setter),
    /// <see cref="ContractDirection.Output"/> to validate values being retrieved (getter), or <see cref="ContractDirection.Default"/> to select automatically. See <see cref="ContractDirection"/> for details.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.</param>
    /// <param name="tags">An optional object (typically of anonymous type) passed to the template and accessible via <c>meta.Tags</c>. See <see href="@sharing-state-with-advice"/> for details.</param>
    /// <returns>An <see cref="IAddContractAdviceResult{T}"/> exposing the added contract.</returns>
    /// <remarks>
    /// <para>
    /// <b>Template Access:</b> Within the template, use <c>meta.Target.Expression</c> for unified access to the target as an <see cref="IExpression"/>,
    /// <c>meta.Target.FieldOrProperty</c> for field/property-specific access, and <c>meta.Target.ContractDirection</c> to determine whether you're validating input or output.
    /// </para>
    /// <para>
    /// <b>Performance Note:</b> When possible, provide all contracts to the same method from a single aspect. This approach yields better compile-time performance than using several separate aspects.
    /// </para>
    /// <para>
    /// <b>Ready-Made Contracts:</b> Consider using the <c>Metalama.Patterns.Contracts</c> package, which provides ready-made contract attributes for common validation scenarios
    /// (nullability, strings, numeric ranges, enums, collections). See <see href="@contract-patterns"/> for details.
    /// </para>
    /// </remarks>
    /// <seealso cref="ContractAspect"/>
    /// <seealso cref="IMetaTarget.Expression"/>
    /// <seealso href="@contracts"/>
    /// <seealso href="@contract-patterns"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static IAddContractAdviceResult<IFieldOrPropertyOrIndexer> AddContract(
        this IAdviser<IFieldOrPropertyOrIndexer> adviser,
        string template,
        ContractDirection direction = ContractDirection.Default,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.AddContract(
            adviser.Target,
            template,
            direction,
            tags,
            args );

    /// <summary>
    /// Adds a custom attribute to a given declaration.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to add the attribute to a different declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a declaration.</param>
    /// <param name="attribute">The custom attribute to be added. It can be an existing <see cref="IAttribute"/> from the code model
    ///     (as it implements <see cref="IAttributeData"/>), or you can use <see cref="AttributeConstruction.Create(Type, System.Collections.Generic.IReadOnlyList{object?}?, System.Collections.Generic.IReadOnlyList{System.Collections.Generic.KeyValuePair{string, object?}}?)"/>
    ///     to create a new attribute programmatically.</param>
    /// <param name="whenExists">Specifies the strategy to follow when an attribute of the same type already exists on the target declaration:
    ///     <see cref="OverrideStrategy.Fail"/> fails the compilation with an error (default);
    ///     <see cref="OverrideStrategy.Ignore"/> silently skips the introduction;
    ///     <see cref="OverrideStrategy.Override"/> removes all previous instances and replaces them with the new one;
    ///     <see cref="OverrideStrategy.New"/> adds the new instance regardless of existing attributes.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IAttribute"/>.</returns>
    /// <seealso cref="RemoveAttributes(IAdviser{IDeclaration}, INamedType)"/>
    /// <seealso cref="AttributeConstruction"/>
    /// <seealso cref="IDeclaration.Attributes"/>
    /// <seealso href="@adding-attributes"/>
    public static IIntroductionAdviceResult<IAttribute> IntroduceAttribute(
        this IAdviser<IDeclaration> adviser,
        IAttributeData attribute,
        OverrideStrategy whenExists = OverrideStrategy.Default )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceAttribute(
            adviser.Target,
            attribute,
            whenExists );

    /// <summary>
    /// Removes all custom attributes of a given <see cref="INamedType"/> from a given declaration.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to remove attributes from a different declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a declaration.</param>
    /// <param name="attributeType">The type of custom attributes to be removed. All attributes whose type is assignable to this type will be removed.</param>
    /// <returns>An <see cref="IRemoveAttributesAdviceResult"/> exposing the removal operation.</returns>
    /// <remarks>
    /// <para>
    /// Note that custom attributes cannot be edited in place. To modify an attribute, remove it using this method
    /// and add a new one using <see cref="IntroduceAttribute"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="IntroduceAttribute"/>
    /// <seealso cref="IDeclaration.Attributes"/>
    /// <seealso href="@adding-attributes"/>
    public static IRemoveAttributesAdviceResult RemoveAttributes(
        this IAdviser<IDeclaration> adviser,
        INamedType attributeType )
        => ((IAdviserInternal) adviser).AdviceFactory.RemoveAttributes(
            adviser.Target,
            attributeType );

    /// <summary>
    /// Removes all custom attributes of a given <see cref="Type"/> from a given declaration.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to remove attributes from a different declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a declaration.</param>
    /// <param name="attributeType">The type of custom attributes to be removed. All attributes whose type is assignable to this type will be removed.</param>
    /// <returns>An <see cref="IRemoveAttributesAdviceResult"/> exposing the removal operation.</returns>
    /// <remarks>
    /// <para>
    /// Note that custom attributes cannot be edited in place. To modify an attribute, remove it using this method
    /// and add a new one using <see cref="IntroduceAttribute"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="IntroduceAttribute"/>
    /// <seealso cref="IDeclaration.Attributes"/>
    /// <seealso href="@adding-attributes"/>
    public static IRemoveAttributesAdviceResult RemoveAttributes(
        this IAdviser<IDeclaration> adviser,
        Type attributeType )
        => ((IAdviserInternal) adviser).AdviceFactory.RemoveAttributes(
            adviser.Target,
            attributeType );

    // We require an explicit TypedConstant value instead of providing 'default' as the default value because a next Metalama version may allow to
    // append parameters without a default value; in this case, we would have a different signature.

    /// <summary>
    /// Appends a parameter to a constructor by specifying its name and <see cref="IType"/>.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to introduce the parameter to a different constructor than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a constructor.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="parameterType">The type of the parameter.</param>
    /// <param name="defaultValue">The default value of the parameter (required). It must be type-compatible with <paramref name="parameterType"/>.
    ///     To specify <c>default</c> as the default value, use <see cref="TypedConstant.Default(IType, bool)"/>.</param>
    /// <param name="pullAction">An optional delegate that returns a <see cref="PullAction"/> specifying how to pull the new parameter from other child constructors.
    ///     A <c>null</c> value is equivalent to <see cref="PullAction.None"/>, i.e. <paramref name="defaultValue"/> of the parameter will be used.</param>
    /// <param name="attributes">An optional list of custom attributes to add to the introduced parameter.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IParameter"/>.</returns>
    [Obsolete( "This overload does not work across project boundaries. Use an overload that accepts an IIntroduceConstructorParameterPullStrategy." )]
    public static IIntroductionAdviceResult<IParameter> IntroduceParameter(
        this IAdviser<IConstructor> adviser,
        string parameterName,
        IType parameterType,
        TypedConstant defaultValue,
        Func<IParameter, IConstructor, PullAction>? pullAction,
        ImmutableArray<AttributeConstruction> attributes = default )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceParameter(
            adviser.Target,
            parameterName,
            parameterType,
            defaultValue,
            pullAction,
            attributes );

    /// <summary>
    /// Appends a parameter to a constructor by specifying its name and <see cref="IType"/>.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different constructor than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a constructor.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="parameterType">The type of the parameter.</param>
    /// <param name="defaultValue">The default value of the parameter (required). It must be type-compatible with <paramref name="parameterType"/>.
    ///     To specify <c>default</c> as the default value, use <see cref="TypedConstant.Default(IType, bool)"/>.</param>
    /// <param name="pullStrategy">An optional <see cref="IPullStrategy"/> that returns a <see cref="PullAction"/> specifying how to pull the new parameter from other child constructors.
    ///     A <c>null</c> value is equivalent to <see cref="PullAction.None"/>, i.e. <paramref name="defaultValue"/> of the parameter will be used.</param>
    /// <param name="attributes">An optional list of custom attributes to add to the introduced parameter.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IParameter"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method is typically used for dependency injection scenarios where a parameter (such as a service dependency)
    /// needs to be added to a constructor. The parameter is appended to the constructor's parameter list.
    /// </para>
    /// <para>
    /// The <paramref name="pullStrategy"/> determines how the new parameter value is obtained in constructors that call
    /// the target constructor using <c>: base(...)</c> or <c>: this(...)</c>. Use <see cref="PullAction.UseExistingParameter"/>
    /// to pass an existing parameter, <see cref="PullAction.UseExpression"/> to pass a custom expression, or
    /// <see cref="PullAction.IntroduceParameterAndPull"/> to introduce the parameter in child constructors as well.
    /// </para>
    /// <para>
    /// <strong>Cross-Project Support:</strong> When using <see cref="PullStrategy.IntroduceParameterAndPull"/>, the parameter
    /// introduction propagates across project boundaries. All derived classes in projects that reference the current project
    /// will automatically have the parameter added to their constructors.
    /// </para>
    /// <para>
    /// A default value must always be provided. This default value is used when no pull action is specified or when
    /// the pull strategy returns <see cref="PullAction.None"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="PullAction"/>
    /// <seealso cref="IPullStrategy"/>
    /// <seealso href="@introducing-constructor-parameters"/>
    public static IIntroductionAdviceResult<IParameter> IntroduceParameter(
        this IAdviser<IConstructor> adviser,
        string parameterName,
        IType parameterType,
        TypedConstant defaultValue,
        IPullStrategy? pullStrategy = null,
        ImmutableArray<AttributeConstruction> attributes = default )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceParameter(
            adviser.Target,
            parameterName,
            parameterType,
            defaultValue,
            pullStrategy,
            attributes );

    /// <summary>
    /// Appends a parameter to a constructor by specifying its name and <see cref="Type"/>.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another constructor than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a constructor.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="parameterType">The type of the parameter.</param>
    /// <param name="defaultValue">The default value of the parameter (required). It must be type-compatible with <paramref name="parameterType"/>.
    ///     To specify <c>default</c> as the default value, use <see cref="TypedConstant.Default(IType, bool)"/>.</param>
    /// <param name="pullAction">An optional delegate that returns a <see cref="PullAction"/> specifying how to pull the new parameter from other child constructors.
    ///     A <c>null</c> value is equivalent to <see cref="PullAction.None"/>, i.e. <paramref name="defaultValue"/> of the parameter will be used.</param>
    /// <param name="attributes">An optional list of custom attributes to add to the introduced parameter.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IParameter"/>.</returns>
    [Obsolete( "This overload does not work across project boundaries. Use an overload that accepts an IIntroduceConstructorParameterPullStrategy." )]
    public static IIntroductionAdviceResult<IParameter> IntroduceParameter(
        this IAdviser<IConstructor> adviser,
        string parameterName,
        Type parameterType,
        TypedConstant defaultValue,
        Func<IParameter, IConstructor, PullAction>? pullAction,
        ImmutableArray<AttributeConstruction> attributes = default )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceParameter(
            adviser.Target,
            parameterName,
            parameterType,
            defaultValue,
            pullAction,
            attributes );

    /// <summary>
    /// Appends a parameter to a constructor by specifying its name and <see cref="Type"/>.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to introduce the parameter to a different constructor than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a constructor.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="parameterType">The type of the parameter.</param>
    /// <param name="defaultValue">The default value of the parameter (required). It must be type-compatible with <paramref name="parameterType"/>.
    ///     To specify <c>default</c> as the default value, use <see cref="TypedConstant.Default(IType, bool)"/>.</param>
    /// <param name="pullStrategy">An optional <see cref="IPullStrategy"/> that returns a <see cref="PullAction"/> specifying how to pull the new parameter from other child constructors.
    ///     A <c>null</c> value is equivalent to <see cref="PullAction.None"/>, i.e. <paramref name="defaultValue"/> of the parameter will be used.</param>
    /// <param name="attributes">An optional list of custom attributes to add to the introduced parameter.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IParameter"/>.</returns>
    /// <remarks>
    /// This is a convenience overload that accepts a <see cref="Type"/> instead of <see cref="IType"/>.
    /// See <see cref="IntroduceParameter(IAdviser{IConstructor}, string, IType, TypedConstant, IPullStrategy?, ImmutableArray{AttributeConstruction})"/>
    /// for detailed documentation on parameter introduction and pull strategies.
    /// </remarks>
    /// <seealso cref="PullAction"/>
    /// <seealso cref="IPullStrategy"/>
    /// <seealso href="@introducing-constructor-parameters"/>
    public static IIntroductionAdviceResult<IParameter> IntroduceParameter(
        this IAdviser<IConstructor> adviser,
        string parameterName,
        Type parameterType,
        TypedConstant defaultValue,
        IPullStrategy? pullStrategy = null,
        ImmutableArray<AttributeConstruction> attributes = default )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceParameter(
            adviser.Target,
            parameterName,
            parameterType,
            defaultValue,
            pullStrategy,
            attributes );

    /// <summary>
    /// Introduces a new class into the current namespace (as a top-level type) or type (as a nested type).
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> or <see cref="WithNamespace"/> method to introduce the class to a different type or namespace
    /// than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type or namespace.</param>
    /// <param name="name">The class name.</param>
    /// <param name="whenExists">Determines the implementation strategy when a class of the same name is already declared in the target type or namespace.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildType">An optional delegate that modifies the <see cref="INamedTypeBuilder"/> that represents the introduced class.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> that exposes the outcome of the operation and the introduced <see cref="INamedType"/>.</returns>
    /// <seealso href="@introducing-types"/>
    public static IIntroductionAdviceResult<INamedType> IntroduceClass(
        this IAdviser<INamespaceOrNamedType> adviser,
        string name,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<INamedTypeBuilder>? buildType = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceClass(
            adviser.Target,
            name,
            whenExists,
            buildType );

    /// <summary>
    /// Introduces a new interface into the current namespace (as a top-level type) or type (as a nested type).
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> or <see cref="WithNamespace"/> method to introduce the interface to a different type or namespace
    /// than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type or namespace.</param>
    /// <param name="name">The interface name.</param>
    /// <param name="whenExists">Determines the implementation strategy when an interface of the same name is already declared in the target type or namespace.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildType">An optional delegate that modifies the <see cref="INamedTypeBuilder"/> that represents the introduced interface.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> that exposes the outcome of the operation and the introduced <see cref="INamedType"/>.</returns>
    /// <seealso href="@introducing-types"/>
    public static IIntroductionAdviceResult<INamedType> IntroduceInterface(
        this IAdviser<INamespaceOrNamedType> adviser,
        string name,
        OverrideStrategy whenExists = OverrideStrategy.Default,
        Action<INamedTypeBuilder>? buildType = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceInterface(
            adviser.Target,
            name,
            whenExists,
            buildType );

    /// <summary>
    /// Introduces a new extension block into a static class. Extension blocks allow adding
    /// extension members (methods, properties, indexers) to a type (represented as an <see cref="IType"/>). Requires C# 14+ and Roslyn 5.0+.
    /// </summary>
    /// <param name="adviser">An adviser for a named type. Must be a static class.</param>
    /// <param name="receiverType">The type being extended. Members introduced into this extension block
    ///     will appear as members of this type.</param>
    /// <param name="receiverParameterName">The name of the receiver parameter. Set to <c>null</c> or empty
    ///     for a static extension (members appear as static members of the extended type).
    ///     Set to a non-empty string (e.g., "self", "value") for an instance extension.</param>
    /// <param name="buildExtensionBlock">An optional callback that allows you to configure the extension block,
    ///     such as adding type parameters or attributes to the receiver parameter.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> representing the result of the advice.
    ///     The <see cref="IIntroductionAdviceResult{T}.Declaration"/> property provides access to the introduced extension block.
    ///     The result also implements <see cref="IAdviser{T}"/> and can be used to introduce members.</returns>
    public static IIntroductionAdviceResult<IExtensionBlock> IntroduceExtensionBlock(
        this IAdviser<INamedType> adviser,
        IType receiverType,
        string? receiverParameterName = null,
        Action<IExtensionBlockBuilder>? buildExtensionBlock = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceExtensionBlock(
            adviser.Target,
            receiverType,
            receiverParameterName,
            buildExtensionBlock );

    /// <summary>
    /// Introduces a new extension block into the target static class. Extension blocks allow adding
    /// extension members (methods, properties, indexers) to a type (represented as an <see cref="Type"/>). Requires C# 14+ and Roslyn 5.0+.
    /// </summary>
    /// <param name="adviser">An adviser for the target static class.</param>
    /// <param name="receiverType">The <see cref="Type"/> being extended.</param>
    /// <param name="receiverParameterName">The name of the receiver parameter. Set to <c>null</c> or empty
    ///     for a static extension. Set to a non-empty string for an instance extension.</param>
    /// <param name="buildExtensionBlock">An optional callback to configure the extension block.</param>
    public static IIntroductionAdviceResult<IExtensionBlock> IntroduceExtensionBlock(
        this IAdviser<INamedType> adviser,
        Type receiverType,
        string? receiverParameterName = null,
        Action<IExtensionBlockBuilder>? buildExtensionBlock = null )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceExtensionBlock(
            adviser.Target,
            receiverType,
            receiverParameterName,
            buildExtensionBlock );

    /// <summary>
    /// Adds an aspect to the target declaration.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to add the aspect to a different declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a declaration.</param>
    /// <param name="aspect">The aspect to add to the target declaration.</param>
    /// <typeparam name="TDeclaration">The type of declaration that the aspect targets.</typeparam>
    public static void AddAspect<TDeclaration>( this IAdviser<TDeclaration> adviser, IAspect<TDeclaration> aspect )
        where TDeclaration : class, IDeclaration
        => ((IAdviserInternal) adviser).AdviceFactory.AddAspect( adviser.Target, aspect );

    /// <summary>
    /// Adds an aspect to the target declaration, using the aspect type's default constructor.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to add the aspect to a different declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a declaration.</param>
    /// <typeparam name="TAspect">The aspect type. It must have a default constructor.</typeparam>
    public static void AddAspect<TAspect>( this IAdviser adviser )
        where TAspect : class, IAspect, new()
        => ((IAdviserInternal) adviser).AdviceFactory.AddAspect( adviser.Target, new TAspect() );

    /// <summary>
    /// Adds an aspect to the target declaration, unless there is already an aspect of that type on the declaration.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to add the aspect to a different declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a declaration.</param>
    /// <typeparam name="TAspect">The aspect type. It must have a default constructor.</typeparam>
    public static void RequireAspect<TAspect>( this IAdviser adviser )
        where TAspect : class, IAspect, new()
        => ((IAdviserInternal) adviser).AdviceFactory.RequireAspect( adviser.Target, typeof(TAspect) );

    /// <summary>
    /// Adds an aspect to the target declaration, unless there is already an aspect of that type on the declaration.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to add the aspect to a different declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a declaration.</param>
    /// <param name="aspectType">The aspect type. It must have a default constructor.</param>
    public static void RequireAspect( this IAdviser adviser, Type aspectType )
        => ((IAdviserInternal) adviser).AdviceFactory.RequireAspect( adviser.Target, aspectType );

    /// <summary>
    /// Adds an aspect to the target type, using the aspect type's default constructor.
    /// </summary>
    /// <remarks>
    /// This overload resolves the ambiguity between <see cref="AddAspect{TAspect}(IAdviser)"/> and <c>AspectQueryExtensions.AddAspect</c>
    /// for <see cref="ITypeAmender"/>, which implements both <see cref="IAdviser"/> and <c>IQuery</c>.
    /// </remarks>
    /// <param name="amender">The type amender.</param>
    /// <typeparam name="TAspect">The aspect type. It must have a default constructor.</typeparam>
    public static void AddAspect<TAspect>( this ITypeAmender amender )
        where TAspect : class, IAspect, new()
        => ((IAdviserInternal) amender).AdviceFactory.AddAspect( amender.Type, new TAspect() );

    /// <summary>
    /// Adds an aspect to the target type, unless there is already an aspect of that type on the declaration.
    /// </summary>
    /// <remarks>
    /// This overload resolves the ambiguity between <see cref="RequireAspect{TAspect}(IAdviser)"/> and <c>AspectQueryExtensions.RequireAspect</c>
    /// for <see cref="ITypeAmender"/>, which implements both <see cref="IAdviser"/> and <c>IQuery</c>.
    /// </remarks>
    /// <param name="amender">The type amender.</param>
    /// <typeparam name="TAspect">The aspect type. It must have a default constructor.</typeparam>
    public static void RequireAspect<TAspect>( this ITypeAmender amender )
        where TAspect : class, IAspect, new()
        => ((IAdviserInternal) amender).AdviceFactory.RequireAspect( amender.Type, typeof(TAspect) );

    /// <summary>
    /// Gets an <see cref="IAdviser{T}"/> for a specific namespace of the current compilation.
    /// </summary>
    /// <param name="adviser">An adviser of the current compilation.</param>
    /// <param name="name">The full namespace.</param>
    /// <returns>An <see cref="IAdviser{T}"/> for the given namespace.</returns>
    public static IAdviser<INamespace> WithNamespace( this IAdviser<ICompilation> adviser, string name )
        => ((IAdviserInternal) adviser).AdviceFactory.WithNamespace(
            adviser.Target.GlobalNamespace,
            name );

    /// <summary>
    /// Gets an <see cref="IAdviser{T}"/> for a specific child namespace of the current namespace.
    /// </summary>
    /// <param name="adviser">An adviser of the current namespace.</param>
    /// <param name="name">The namespace, relatively to the current namespace. It can include periods to specify sub-namespaces.</param>
    /// <returns>An <see cref="IAdviser{T}"/> for the child namespace.</returns>
    public static IAdviser<INamespace> WithChildNamespace( this IAdviser<INamespace> adviser, string name )
        => ((IAdviserInternal) adviser).AdviceFactory.WithNamespace(
            adviser.Target,
            name );

    /// <summary>
    /// Adds a custom annotation to a declaration. Annotations enable communication between aspects by attaching
    /// arbitrary serializable objects to declarations that other aspects can query.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Annotations are useful for:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Inter-aspect communication:</b> Pass information from one aspect to another without using aspect inheritance</description></item>
    /// <item><description><b>Flagging declarations:</b> Mark declarations for special processing by later aspects</description></item>
    /// <item><description><b>Storing computed data:</b> Attach data that other aspects need during code generation</description></item>
    /// </list>
    /// <para>
    /// <b>Reading annotations:</b> Use <see cref="DeclarationEnhancements{T}.GetAnnotations{TAnnotation}"/> via
    /// <see cref="DeclarationExtensions.Enhancements{T}(T)"/> to retrieve annotations from any declaration.
    /// </para>
    /// <para>
    /// <b>Visibility:</b> By default, annotations are only visible within the current project. Set <paramref name="export"/>
    /// to <c>true</c> to make them visible to projects that reference this one.
    /// </para>
    /// <para>
    /// <b>Targeting other declarations:</b> Use <see cref="IAdviser.With{TNewDeclaration}"/> to add annotations to
    /// declarations other than the current target.
    /// </para>
    /// <para>
    /// <b>Annotations vs AspectState:</b> Use <see cref="IAspectBuilder.AspectState"/> to share state with child aspects
    /// and inheriting aspects via the predecessor chain. Use annotations for peer-to-peer aspect communication.
    /// </para>
    /// <para>
    /// <b>Design-time limitation:</b> At design time, Metalama performs partial compilations that only include the
    /// inheritance closure of modified files. Aspects targeting declarations outside this scope do not execute,
    /// so their annotations are unavailable. This does not affect full compile time.
    /// </para>
    /// </remarks>
    /// <param name="adviser">An adviser for a declaration.</param>
    /// <param name="annotation">The annotation to attach to the target declaration.</param>
    /// <param name="export">A value indicating whether the annotation should be exported and made visible to other projects.
    /// Unless this parameter is set to <c>true</c>, the annotation will only be visible to the current project.</param>
    /// <typeparam name="TDeclaration">The type of declaration.</typeparam>
    /// <seealso cref="IAnnotation{T}"/>
    /// <seealso cref="DeclarationEnhancements{T}.GetAnnotations{TAnnotation}"/>
    /// <seealso cref="IAspectBuilder.AspectState"/>
    /// <seealso href="@sharing-state-with-advice"/>
    public static void AddAnnotation<TDeclaration>(
        this IAdviser<TDeclaration> adviser,
        IAnnotation<TDeclaration> annotation,
        bool export = false )
        where TDeclaration : class, IDeclaration
        => ((IAdviserInternal) adviser).AdviceFactory.AddAnnotation(
            adviser.Target,
            annotation,
            export );

    /// <summary>
    /// Gets an <see cref="IAdviser{T}"/> with a different template provider, provided as an <see cref="ITemplateProvider"/>.
    /// </summary>
    /// <param name="adviser">The current <see cref="IAdviser{T}"/>.</param>
    /// <param name="templateProvider">The new <see cref="ITemplateProvider"/>. </param>
    /// <typeparam name="TDeclaration">The type of declaration advised by the current <see cref="IAdviser{T}"/>.</typeparam>
    /// <returns>A new adviser for the current declaration, using <paramref name="templateProvider"/>.</returns>
    public static IAdviser<TDeclaration> WithTemplateProvider<TDeclaration>(
        this IAdviser<TDeclaration> adviser,
        ITemplateProvider templateProvider )
        where TDeclaration : class, IDeclaration
        => new Adviser<TDeclaration>(
            adviser.Target,
            ((IAdviserInternal) adviser).AdviceFactory.WithTemplateProvider( templateProvider ),
            adviser.Diagnostics );

    /// <summary>
    /// Gets an <see cref="IAdviser{T}"/> with a different template provider, specified as a <see cref="TemplateProvider"/>.
    /// </summary>
    /// <param name="adviser">The current <see cref="IAdviser{T}"/>.</param>
    /// <param name="templateProvider">The new <see cref="TemplateProvider"/>. </param>
    /// <typeparam name="TDeclaration">The type of declaration advised by the current <see cref="IAdviser{T}"/>.</typeparam>
    /// <returns>A new adviser for the current declaration, using <paramref name="templateProvider"/>.</returns>
    public static IAdviser<TDeclaration> WithTemplateProvider<TDeclaration>(
        this IAdviser<TDeclaration> adviser,
        in TemplateProvider templateProvider )
        where TDeclaration : class, IDeclaration
        => new Adviser<TDeclaration>(
            adviser.Target,
            ((IAdviserInternal) adviser).AdviceFactory.WithTemplateProvider( templateProvider ),
            adviser.Diagnostics );

    /// <summary>
    /// Gets an <see cref="IAdviser{T}"/> for the declaring type of the current member.
    /// </summary>
    /// <param name="memberAdviser">An adviser for a member.</param>
    /// <returns>An <see cref="IAdviser{T}"/> for the declaring type of the member.</returns>
    public static IAdviser<INamedType> WithDeclaringType( this IAdviser<IMember> memberAdviser ) => memberAdviser.With( memberAdviser.Target.DeclaringType );

    private class Adviser<T> : IAdviser<T>, IAdviserInternal
        where T : class, IDeclaration
    {
        public ScopedDiagnosticSink Diagnostics { get; }

        IDeclaration IAdviser.Target => this.Target;

        public ICompilation Compilation => this.AdviceFactory.Compilation;

        public ICompilation MutableCompilation => this.AdviceFactory.MutableCompilation;

        public T Target { get; }

        public IAdviceFactory AdviceFactory { get; }

        public Adviser( T target, IAdviceFactory adviceFactory, in ScopedDiagnosticSink diagnostics )
        {
            this.Target = target;
            this.AdviceFactory = adviceFactory;
            this.Diagnostics = diagnostics;
        }

        public IAdviser<TNewDeclaration> With<TNewDeclaration>( TNewDeclaration declaration )
            where TNewDeclaration : class, IDeclaration
        {
            if ( !declaration.IsContainedIn( declaration ) && declaration.DeclarationKind is not (DeclarationKind.Compilation or DeclarationKind.Namespace) )
            {
                throw new ArgumentOutOfRangeException( nameof(declaration), $"'{declaration}' is not contained in '{this.Target}'." );
            }

            return new Adviser<TNewDeclaration>( declaration, this.AdviceFactory, this.Diagnostics );
        }
    }
}