// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Code;
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
    /// Overrides the implementation of a method.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different method than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a method.</param>
    /// <param name="template">Name of a method in the aspect class whose implementation will be used as a template.
    ///     This method must be annotated with <see cref="TemplateAttribute"/>. To select different templates according to the kind of target method
    ///     (such as async or iterator methods), use the constructor of the <see cref="MethodTemplateSelector"/> type. To specify a single
    ///     template for all methods, pass a string.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template method.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property
    ///     of the <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IOverrideAdviceResult{T}"/> exposing the overridden <see cref="IMethod"/>.</returns>
    /// <seealso href="@overriding-methods"/>
    public static IOverrideAdviceResult<IMethod> Override(
        this IAdviser<IMethod> adviser,
        in MethodTemplateSelector template,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.Override( adviser.Target, template, args, tags );

    /// <summary>
    /// Introduces a new method or overrides the implementation of an existing one.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="template">Name of the method of the aspect class that will be used as a template for the introduced method. This method must be
    ///     annotated with <see cref="TemplateAttribute"/>. This method can have parameters and a return type. The actual parameters and return type
    ///     of the introduced method can be modified using the <see cref="IMethodBuilder"/> returned by this method.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     method. The default scope depends on the scope of the template method. If the template method is static, the introduced method is static. However, if the template method is non-static, then the introduced method
    ///     copies the scope of the target declaration of the aspect.</param>
    /// <param name="whenExists">Determines the implementation strategy when a method of the same name and signature is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildMethod">An optional delegate that modifies the <see cref="IMethodBuilder"/> representing the introduced or overriding method.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template method.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property
    ///     of the <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IMethod"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template method.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property
    ///     of the <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IMethod"/> (finalizer).</returns>
    /// <seealso href="@introducing-members"/>
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
    /// <param name="buildOperator">An optional delegate that modifies the <see cref="IMethodBuilder"/> representing the introduced or overriding operator.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template method.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property
    ///     of the <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IMethod"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
    /// <param name="buildOperator">An optional delegate that modifies the <see cref="IMethodBuilder"/> representing the introduced or overriding operator.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template method.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property
    ///     of the <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IMethod"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceBinaryOperator(
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
    /// <param name="buildOperator">An optional delegate that modifies the <see cref="IMethodBuilder"/> representing the introduced or overriding operator.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template method.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property
    ///     of the <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IMethod"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceConversionOperator(
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
    /// <param name="template">Name of a method in the aspect class whose implementation will be used as a template.
    ///     This method must be annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template method.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property
    ///     of the <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IOverrideAdviceResult{T}"/> exposing the overridden <see cref="IConstructor"/>.</returns>
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
    /// <param name="buildConstructor">An optional delegate that modifies the <see cref="IConstructorBuilder"/> that represents the introduced constructor.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template method.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property
    ///     of the <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IConstructor"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
    /// <param name="template">The name of a property of the aspect class, with a getter, a setter, or both, whose implementation will be used as a template.
    ///     This property must be annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template property and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IOverrideAdviceResult{T}"/> exposing the overridden <see cref="IProperty"/>.</returns>
    /// <seealso href="@overriding-fields-or-properties"/>
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
    ///     To select different templates for iterator getters, use the constructor of the <see cref="GetterTemplateSelector"/> type. To specify a single
    ///     template for all cases, pass a string.</param>
    /// <param name="setTemplate">The name of the method of the aspect class whose implementation will be used as a template for the setter, or <c>null</c>
    ///     if the setter should not be overridden. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Set(T value)</c> where <c>T</c> is either <c>dynamic</c> or compatible with the field, property or indexer type.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IOverrideAdviceResult{T}"/> exposing the overridden <see cref="IPropertyOrIndexer"/>.</returns>
    /// <seealso href="@overriding-fields-or-properties"/>
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
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IOverrideAdviceResult{T}"/> exposing the overridden <see cref="IProperty"/>.</returns>
    /// <seealso href="@overriding-fields-or-properties"/>
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
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IOverrideAdviceResult{T}"/> exposing the overridden <see cref="IIndexer"/>.</returns>
    /// <seealso href="@overriding-fields-or-properties"/>
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
    /// <param name="tags">An optional opaque object of anonymous type passed to the template and exposed under the <see cref="meta.Tags"/> property.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IField"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="fieldName">Name of the introduced field.</param>
    /// <param name="fieldType">Type of the introduced field.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     field. The default scope is <see cref="IntroductionScope.Instance"/>.</param>
    /// <param name="whenExists">Determines the implementation strategy when a field of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildField">An optional delegate that modifies the <see cref="IFieldBuilder"/> representing the introduced field.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template and exposed under the <see cref="meta.Tags"/> property.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IField"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="fieldName">Name of the introduced field.</param>
    /// <param name="fieldType">Type of the introduced field.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     field. The default scope is <see cref="IntroductionScope.Instance"/>.</param>
    /// <param name="whenExists">Determines the implementation strategy when a field of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildField">An optional delegate that modifies the <see cref="IFieldBuilder"/> representing the introduced field.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template and exposed under the <see cref="meta.Tags"/> property.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IField"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="propertyName">Name of the introduced property.</param>
    /// <param name="propertyType">Type of the introduced property.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     property. The default scope is <see cref="IntroductionScope.Instance"/>.</param>
    /// <param name="whenExists">Determines the implementation strategy when a property of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildProperty">An optional delegate that modifies the <see cref="IPropertyBuilder"/> representing the introduced property.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template and exposed under the <see cref="meta.Tags"/> property.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IProperty"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="propertyName">Name of the introduced property.</param>
    /// <param name="propertyType">Type of the introduced property.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     property. The default scope is <see cref="IntroductionScope.Instance"/>.</param>
    /// <param name="whenExists">Determines the implementation strategy when a property of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildProperty">An optional delegate that modifies the <see cref="IPropertyBuilder"/> representing the introduced property.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template and exposed under the <see cref="meta.Tags"/> property.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IProperty"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
    /// <param name="tags">An optional opaque object of anonymous type passed to the template property and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IProperty"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
    /// Introduces a property to the target type, or overrides the implementation of an existing one, by specifying individual template methods for each accessor.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different type than the current one.
    /// </summary>
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
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IProperty"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different type than the current one.
    /// </summary>
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
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IIndexer"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to a different type than the current one.
    /// </summary>
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
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IIndexer"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to introduce the indexer to a different type than the current one.
    /// </summary>
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
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IIndexer"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to introduce the indexer to a different type than the current one.
    /// </summary>
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
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IIndexer"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
    ///     the event's handlers. The signature of this method must be <c>T Invoke()</c>, <c>T Invoke(U handler)</c>, or <c>T Invoke(U handler, V1 param1, V2 param2, ...)</c>  
    ///     where <c>T</c> is either <c>dynamic</c> or a type compatible with the return value of the event's delegate type, <c>U</c> is either <c>dynamic</c> or the event's 
    ///     delegate type, <c>Vn</c> are types matching the delegate's parameters.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IOverrideAdviceResult{T}"/> exposing the overridden <see cref="IEvent"/>.</returns>
    /// <seealso href="@overriding-events"/>
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
    /// <param name="tags">An optional opaque object of anonymous type passed to the template event and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IEvent"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to introduce the event to a different type than the current one.
    /// </summary>
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
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced or overriding <see cref="IEvent"/>.</returns>
    /// <seealso href="@introducing-members"/>
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
    /// <see cref="Aspects.IntroduceAttribute"/>, or programmatically using introduction methods.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to implement the interface on a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="interfaceType">The type of the implemented interface.</param>
    /// <param name="whenExists">Determines the implementation strategy when the interface is already implemented by the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to <see cref="InterfaceMemberAttribute"/> templates and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API. This parameter does not affect members introduced using <see cref="Aspects.IntroduceAttribute"/> or programmatically.</param>
    /// <returns>An <see cref="IImplementInterfaceAdviceResult"/> exposing the implementation operation.</returns>
    /// <seealso href="@implementing-interfaces"/>
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
    /// <see cref="Aspects.IntroduceAttribute"/> or programmatically using introduction methods.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to implement the interface on a different type than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="interfaceType">The type of the implemented interface.</param>
    /// <param name="whenExists">Determines the implementation strategy when the interface is already implemented by the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to <see cref="InterfaceMemberAttribute"/> templates and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API. This parameter does not affect members introduced using <see cref="Aspects.IntroduceAttribute"/> or programmatically.</param>
    /// <returns>An <see cref="IImplementInterfaceAdviceResult"/> exposing the implementation operation.</returns>
    /// <seealso href="@implementing-interfaces"/>
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
    /// <param name="tags">An optional opaque object of anonymous type passed to templates and exposed under the <see cref="meta.Tags"/> property.</param>
    /// <returns>An <see cref="IAddInitializerAdviceResult"/> exposing the added initializer.</returns>
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
    /// <param name="tags">An optional opaque object of anonymous type passed to templates and exposed under the <see cref="meta.Tags"/> property.</param>
    /// <returns>An <see cref="IAddInitializerAdviceResult"/> exposing the added initializer.</returns>
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
    public static IAddInitializerAdviceResult AddInitializer(
        this IAdviser<IConstructor> adviser,
        IStatement statement )
        => ((IAdviserInternal) adviser).AdviceFactory.AddInitializer(
            adviser.Target,
            statement );

    /// <summary>
    /// Adds a contract to a parameter. Contracts are usually used to validate parameters (pre- or post-conditions) or to normalize their value.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to add the contract to a different parameter than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a parameter.</param>
    /// <param name="template">The name of the template method. This method must have a single run-time parameter named <c>value</c> and be annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="direction">Direction of the data flow to which the contract should apply. See <see cref="ContractDirection"/> for details.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to templates and exposed under the <see cref="meta.Tags"/> property.</param>
    /// <returns>An <see cref="IAddContractAdviceResult{T}"/> exposing the added contract.</returns>
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
    /// Adds a contract to a field, property or indexer.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to add the contract to a different field, property or indexer than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a field, property or indexer.</param>
    /// <param name="template">The name of the template method. This method must have a single run-time parameter named <c>value</c> and be annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="direction">Direction of the data flow to which the contract should apply. See <see cref="ContractDirection"/> for details.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property.</param>
    /// <returns>An <see cref="IAddContractAdviceResult{T}"/> exposing the added contract.</returns>
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
    /// <param name="attribute">The custom attribute to be added. It can be an existing <see cref="IAttributeData"/>, or you can use <see cref="AttributeConstruction"/>
    ///     to specify a new attribute.</param>
    /// <param name="whenExists">Specifies the strategy to follow when an attribute of the same type already exists on the target declaration. <see cref="OverrideStrategy.Fail"/> fails the
    ///     compilation with an error (default). <see cref="OverrideStrategy.Ignore"/> silently ignores the introduction. <see cref="OverrideStrategy.Override"/> removes
    ///     all previous instances and replaces them with the new one. <see cref="OverrideStrategy.New"/> adds the new instance regardless.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IAttribute"/>.</returns>
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
    /// <param name="attributeType">The type of custom attributes to be removed.</param>
    /// <returns>An <see cref="IRemoveAttributesAdviceResult"/> exposing the removal operation.</returns>
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
    /// <param name="attributeType">The type of custom attributes to be removed.</param>
    /// <returns>An <see cref="IRemoveAttributesAdviceResult"/> exposing the removal operation.</returns>
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
    ///     To specify <c>default</c> as the default value, use <see cref="TypedConstant.Default(Metalama.Framework.Code.IType)"/>.</param>
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
    ///     To specify <c>default</c> as the default value, use <see cref="TypedConstant.Default(Metalama.Framework.Code.IType)"/>.</param>
    /// <param name="pullStrategy">An optional <see cref="IPullStrategy"/> that returns a <see cref="PullAction"/> specifying how to pull the new parameter from other child constructors.
    ///     A <c>null</c> value is equivalent to <see cref="PullAction.None"/>, i.e. <paramref name="defaultValue"/> of the parameter will be used.</param>
    /// <param name="attributes">An optional list of custom attributes to add to the introduced parameter.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IParameter"/>.</returns>
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
    ///     To specify <c>default</c> as the default value, use <see cref="TypedConstant.Default(Metalama.Framework.Code.IType)"/>.</param>
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
    ///     To specify <c>default</c> as the default value, use <see cref="TypedConstant.Default(Metalama.Framework.Code.IType)"/>.</param>
    /// <param name="pullStrategy">An optional <see cref="IPullStrategy"/> that returns a <see cref="PullAction"/> specifying how to pull the new parameter from other child constructors.
    ///     A <c>null</c> value is equivalent to <see cref="PullAction.None"/>, i.e. <paramref name="defaultValue"/> of the parameter will be used.</param>
    /// <param name="attributes">An optional list of custom attributes to add to the introduced parameter.</param>
    /// <returns>An <see cref="IIntroductionAdviceResult{T}"/> exposing the introduced <see cref="IParameter"/>.</returns>
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
    /// <param name="name">The namespace, relatively to the current namespace. It can include periods to specify sub-namespaces..</param>
    /// <returns>An <see cref="IAdviser{T}"/> for the child namespace.</returns>
    public static IAdviser<INamespace> WithChildNamespace( this IAdviser<INamespace> adviser, string name )
        => ((IAdviserInternal) adviser).AdviceFactory.WithNamespace(
            adviser.Target,
            name );

    /// <summary>
    /// Adds a custom annotation to a declaration. An annotation is an arbitrary but serializable object that can then be retrieved
    /// using the <see cref="DeclarationEnhancements{T}.GetAnnotations{TAnnotation}"/> method of the <see cref="DeclarationExtensions.Enhancements{T}"/> object.
    /// Annotations are a way of communication between aspects or classes of aspects.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to add the annotation to a different declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a declaration.</param>
    /// <param name="annotation">The annotation.</param>
    /// <param name="export">A value indicating whether the annotation should be exported and made visible to other projects.
    /// Unless this parameter is set to <c>true</c>, the annotation will only be visible to the current project.</param>
    /// <typeparam name="TDeclaration">The type of declaration.</typeparam>
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