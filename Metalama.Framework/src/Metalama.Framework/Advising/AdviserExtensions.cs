// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Advising;

// IMPORTANT: Keep XML doc in this file in sync with IAdviceFactory.

[PublicAPI]
[CompileTime]
public static class AdviserExtensions
{
    /// <summary>
    /// Overrides the implementation of a method.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a method.</param>
    /// <param name="template">Name of a method in the aspect class whose implementation will be used as a template.
    ///     This method must be annotated with <see cref="TemplateAttribute"/>. To select a different templates according to the kind of target method
    ///     (such as async or iterator methods), use the constructor of the <see cref="MethodTemplateSelector"/> type. To specify a single
    ///     template for all methods, pass a string.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template method.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property
    ///     of the <see cref="meta"/> API.</param>
    /// <seealso href="@overriding-methods"/>
    public static IOverrideAdviceResult<IMethod> Override(
        this IAdviser<IMethod> adviser,
        in MethodTemplateSelector template,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.Override( adviser.Target, template, args, tags );

    /// <summary>
    /// Introduces a new method or overrides the implementation of the existing one.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="template">Name of the method of the aspect class that will be used as a template for the introduced finalizer. This method must be
    ///     annotated with <see cref="TemplateAttribute"/>. This method can parameters and a return type.</param>
    /// <param name="whenExists">Determines the implementation strategy when a finalizer is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property
    ///     of the <see cref="meta"/> API.</param>
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

    public static IIntroductionAdviceResult<IMethod> IntroduceConversionOperator(
        this IAdviser<INamedType> adviser,
        string template,
        IType fromType,
        IType toType,
        bool isImplicit = false,
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
            whenExists,
            buildOperator,
            args,
            tags );

    /// <summary>
    /// Overrides the implementation of a constructor.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a constructor.</param>
    /// <param name="template">Name of a method in the aspect class whose implementation will be used as a template.
    ///     This method must be annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template method.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property
    ///     of the <see cref="meta"/> API.</param>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a property.</param>
    /// <param name="template">The name of a property of the aspect class, with a getter, a setter, or both, whose implementation will be used as a template.
    ///     This property must be annotated with <see cref="TemplateAttribute"/>.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template property and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
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
    /// Overrides a field or property by specifying a method template for the getter, the setter, or both.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a field, or a property or an indexer.</param>
    /// <param name="getTemplate">The name of the method of the aspect class whose implementation will be used as a template for the getter, or <c>null</c>
    ///     if the getter should not be overridden. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>T Get()</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the field or property.
    ///     To select a different templates for iterator getters, use the constructor of the <see cref="GetterTemplateSelector"/> type. To specify a single
    ///     template for all properties, pass a string.
    /// </param>
    /// <param name="setTemplate">The name of the method of the aspect class whose implementation will be used as a template for the getter, or <c>null</c>
    ///     if the getter should not be overridden. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Set(T value</c>  where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the field or property.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="template">Name of the introduced field.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     field. The default scope is <see cref="IntroductionScope.Instance"/>.</param>
    /// <param name="whenExists">Determines the implementation strategy when a property of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildField"></param>
    /// <param name="tags"></param>
    /// <returns>An <see cref="IPropertyBuilder"/> that allows to dynamically change the name or type of the introduced property.</returns>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="fieldName">Name of the introduced field.</param>
    /// <param name="fieldType">Type of the introduced field.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     field. The default scope is <see cref="IntroductionScope.Instance"/>.</param>
    /// <param name="whenExists">Determines the implementation strategy when a property of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildField"></param>
    /// <param name="tags"></param>
    /// <returns>An <see cref="IPropertyBuilder"/> that allows to dynamically change the name or type of the introduced property.</returns>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="fieldName">Name of the introduced field.</param>
    /// <param name="fieldType">Type of the introduced field.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     field. The default scope is <see cref="IntroductionScope.Instance"/>.</param>
    /// <param name="whenExists">Determines the implementation strategy when a property of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildField"></param>
    /// <param name="tags"></param>
    /// <returns>An <see cref="IPropertyBuilder"/> that allows to dynamically change the name or type of the introduced property.</returns>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="propertyName">Name of the introduced field.</param>
    /// <param name="propertyType">Type of the introduced field.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     field. The default scope is <see cref="IntroductionScope.Instance"/>.</param>
    /// <param name="whenExists">Determines the implementation strategy when a property of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildProperty"></param>
    /// <param name="tags"></param>
    /// <returns>An <see cref="IPropertyBuilder"/> that allows to dynamically change the name or type of the introduced property.</returns>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="propertyName">Name of the introduced field.</param>
    /// <param name="propertyType">Type of the introduced field.</param>
    /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
    ///     field. The default scope is <see cref="IntroductionScope.Instance"/>.</param>
    /// <param name="whenExists">Determines the implementation strategy when a property of the same name is already declared in the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="buildProperty"></param>
    /// <param name="tags"></param>
    /// <returns>An <see cref="IPropertyBuilder"/> that allows to dynamically change the name or type of the introduced property.</returns>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
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
    /// Overrides an event by specifying a template for the adder, the remover, and/or the raiser.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for an event.</param>
    /// <param name="addTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the adder, or <c>null</c>
    ///     the adder should not be overridden. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Add(T value)</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the event.</param>
    /// <param name="removeTemplate">The name of the method of the aspect class whose type and implementation will be used as a template for the remover, or <c>null</c>
    ///     the adder should not be overridden. This method must be annotated with <see cref="TemplateAttribute"/>. The signature of this method must
    ///     be <c>void Remove(T value)</c> where <c>T</c> is either <c>dynamic</c> or a type compatible with the type of the event.</param>
    /// <param name="raiseTemplate">Not yet implemented.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
    /// <seealso href="@overriding-events"/>
    public static IOverrideAdviceResult<IEvent> OverrideAccessors(
        this IAdviser<IEvent> adviser,
        string? addTemplate,
        string? removeTemplate,
        string? raiseTemplate = null,
        object? args = null,
        object? tags = null )
        => ((IAdviserInternal) adviser).AdviceFactory.OverrideAccessors(
            adviser.Target,
            addTemplate,
            removeTemplate,
            raiseTemplate,
            args,
            tags );

    /// <summary>
    /// Introduces a new event to the target type, or overrides the implementation of an existing one, by specifying an event template.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
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
    /// for the adder, the remover, and the raiser.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
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
        this IAdviser<INamedType> adviser,
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
    /// Makes a type implement a new interface specified as an <see cref="INamedType"/>.
    /// Interface members can be introduced declaratively by marking an aspect member by <see cref="InterfaceMemberAttribute"/> or 
    /// <see cref="Aspects.IntroduceAttribute"/>, or programmatically using <c>Introduce</c> methods for public implementations of <c>result.ExplicitImplementations.Introduce</c> for private implementations.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="interfaceType">The type of the implemented interface.</param>
    /// <param name="whenExists">Determines the implementation strategy when the interface is already implemented by the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to <see cref="InterfaceMemberAttribute"/> templates and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API. This parameter does not affect members introduced using <see cref="Aspects.IntroduceAttribute"/> or programmatically.</param>
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
    /// Interface members can be introduced by marking an aspect member by <see cref="InterfaceMemberAttribute"/>, 
    /// <see cref="Aspects.IntroduceAttribute"/> or programmatically using <c>Introduce</c> methods.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="interfaceType">The type of the implemented interface.</param>
    /// <param name="whenExists">Determines the implementation strategy when the interface is already implemented by the target type.
    ///     The default strategy is to fail with a compile-time error.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to <see cref="InterfaceMemberAttribute"/> templates and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API. This parameter does not affect members introduced using <see cref="Aspects.IntroduceAttribute"/> or programmatically.</param>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="template">The name of the template. This method must have no run-time parameter, be of <c>void</c> return type, and be annotated with the <see cref="TemplateAttribute"/> custom attribute.</param>
    /// <param name="kind">The type of initializer to add.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to templates and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a named type.</param>
    /// <param name="statement">The statement to be inserted at the top of constructors.</param>
    /// <param name="kind">The type of initializer to add.</param>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a constructor.</param>
    /// <param name="template">The name of the template. This method must have no run-time parameter, be of <c>void</c> return type, and be annotated with the <see cref="TemplateAttribute"/> custom attribute.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.</param>
    /// <param name="tags">An optional opaque object of anonymous type  passed to templates and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a constructor.</param>
    /// <param name="statement">The statement to be inserted at the top of the constructor.</param>
    public static IAddInitializerAdviceResult AddInitializer(
        this IAdviser<IConstructor> adviser,
        IStatement statement )
        => ((IAdviserInternal) adviser).AdviceFactory.AddInitializer(
            adviser.Target,
            statement );

    /// <summary>
    /// Adds a contract to a parameter. Contracts are usually used to validate parameters (pre- or post-conditions) or to normalize their value (null-to-empty, trimming, normalizing case, ...).
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a parameter.</param>
    /// <param name="template">The name of the template method. This method must have a single run-time parameter named <c>value</c>, and be annotated with the <see cref="TemplateAttribute"/> custom attribute.</param>
    /// <param name="direction">Direction of the data flow to which the contract should apply. See <see cref="ContractDirection"/> for details.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to templates and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
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
    /// Adds a contract to a field, property or indexer. Contracts are usually used to validate the value assigned to fields properties or indexers or to normalize their value (null-to-empty, trimming, normalizing case, ...)
    /// before assignment. Alternatively, a contract can be used to validate the value <i>returned</i> by a property or indexer, in which case the <paramref name="direction"/> parameter should be set to <see cref="ContractDirection.Output"/>.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a field, or an property or an indexer.</param>
    /// <param name="template">The name of the template method. This method must have a single run-time parameter named <c>value</c>, and be annotated with the <see cref="TemplateAttribute"/> custom attribute.</param>
    /// <param name="direction">Direction of the data flow to which the contract should apply. See <see cref="ContractDirection"/> for details.</param>
    /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.</param>
    /// <param name="tags">An optional opaque object of anonymous type passed to templates and exposed under the <see cref="meta.Tags"/> property of the
    ///     <see cref="meta"/> API.</param>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a declaration.</param>
    /// <param name="attribute">The custom attribute to be added. It can be an existing <see cref="IAttribute"/>, or you can use <see cref="AttributeConstruction"/>
    ///     to specify a new attribute.</param>
    /// <param name="whenExists">Specifies the strategy to follow when an attribute of the same type already exists on the target declaration. <see cref="OverrideStrategy.Fail"/> will fail the
    ///     compilation with an error and is the default strategy. <see cref="OverrideStrategy.Ignore"/> will silently ignore the introduction. <see cref="OverrideStrategy.Override"/> will remove
    ///     all previous instances and replace them by the new one. <see cref="OverrideStrategy.New"/> will add the new instance regardless.</param>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a declaration.</param>
    /// <param name="attributeType">The type of custom attributes to be removed.</param>
    public static IRemoveAttributesAdviceResult RemoveAttributes(
        this IAdviser<IDeclaration> adviser,
        INamedType attributeType )
        => ((IAdviserInternal) adviser).AdviceFactory.RemoveAttributes(
            adviser.Target,
            attributeType );

    /// <summary>
    /// Removes all custom attributes of a given <see cref="Type"/> from a given declaration.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a declaration.</param>
    /// <param name="attributeType">The type of custom attributes to be removed.</param>
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a constructor.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="parameterType">The type of the parameter.</param>
    /// <param name="defaultValue">The default value of the parameter (required). It must be type-compatible with <paramref name="parameterType"/>.
    ///     To specify <c>default</c> as the default value, use <see cref="TypedConstant.Default(Metalama.Framework.Code.IType)"/>.</param>
    /// <param name="pullAction">An optional delegate that returns a <see cref="PullAction"/> specifying how to pull the new parameter from other child constructors.
    ///     A <c>null</c> value is equivalent to <see cref="PullAction.None"/>, i.e. <paramref name="defaultValue"/> of the parameter will be used.
    /// </param>
    /// <param name="attributes"></param>
    public static IIntroductionAdviceResult<IParameter> IntroduceParameter(
        this IAdviser<IConstructor> adviser,
        string parameterName,
        IType parameterType,
        TypedConstant defaultValue,
        Func<IParameter, IConstructor, PullAction>? pullAction = null,
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
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
    /// </summary>
    /// <param name="adviser">An adviser for a constructor.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="parameterType">The type of the parameter.</param>
    /// <param name="defaultValue">The default value of the parameter (required). It must be type-compatible with <paramref name="parameterType"/>.
    ///     To specify <c>default</c> as the default value, use <see cref="TypedConstant.Default(Metalama.Framework.Code.IType)"/>.</param>
    /// <param name="pullAction">An optional delegate that returns a <see cref="PullAction"/> specifying how to pull the new parameter from other child constructors.
    ///     A <c>null</c> value is equivalent to <see cref="PullAction.None"/>, i.e. <paramref name="defaultValue"/> of the parameter will be used.
    /// </param>
    /// <param name="attributes"></param>
    public static IIntroductionAdviceResult<IParameter> IntroduceParameter(
        this IAdviser<IConstructor> adviser,
        string parameterName,
        Type parameterType,
        TypedConstant defaultValue,
        Func<IParameter, IConstructor, PullAction>? pullAction = null,
        ImmutableArray<AttributeConstruction> attributes = default )
        => ((IAdviserInternal) adviser).AdviceFactory.IntroduceParameter(
            adviser.Target,
            parameterName,
            parameterType,
            defaultValue,
            pullAction,
            attributes );

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

    public static void AddAspect<TDeclaration>( this IAdviser<TDeclaration> adviser, IAspect<TDeclaration> aspect )
        where TDeclaration : class, IDeclaration
        => ((IAdviserInternal) adviser).AdviceFactory.AddAspect( adviser.Target, aspect );

    public static void AddAspect<TAspect>( this IAdviser adviser )
        where TAspect : class, IAspect, new()
        => ((IAdviserInternal) adviser).AdviceFactory.AddAspect( adviser.Target, new TAspect() );

    public static void RequireAspect<TAspect>( this IAdviser adviser )
        where TAspect : class, IAspect, new()
        => ((IAdviserInternal) adviser).AdviceFactory.RequireAspect( adviser.Target, typeof(TAspect) );

    public static void RequireAspect( this IAdviser adviser, Type aspectType )
        => ((IAdviserInternal) adviser).AdviceFactory.RequireAspect( adviser.Target, aspectType );

    public static IAdviser<INamespace> WithNamespace( this IAdviser<ICompilation> adviser, string name )
        => ((IAdviserInternal) adviser).AdviceFactory.WithNamespace(
            adviser.Target.GlobalNamespace,
            name );

    public static IAdviser<INamespace> WithChildNamespace( this IAdviser<INamespace> adviser, string name )
        => ((IAdviserInternal) adviser).AdviceFactory.WithNamespace(
            adviser.Target,
            name );

    /// <summary>
    /// Adds a custom annotation to a declaration. An annotation is an arbitrary but serializable object that can then be retrieved
    /// using the <see cref="DeclarationEnhancements{T}.GetAnnotations{TAnnotation}"/> method of the <see cref="DeclarationExtensions.Enhancements{T}"/> object.
    /// Annotations are a way of communication between aspects or classes of aspects.
    /// Use the <see cref="IAdviser.With{TNewDeclaration}"/> method to apply the advice to another declaration than the current one.
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

    public static IAdviser<TDeclaration> WithTemplateProvider<TDeclaration>(
        this IAdviser<TDeclaration> adviser,
        ITemplateProvider templateProvider )
        where TDeclaration : class, IDeclaration
        => new Adviser<TDeclaration>(
            adviser.Target,
            ((IAdviserInternal) adviser).AdviceFactory.WithTemplateProvider( templateProvider ),
            adviser.Diagnostics );

    private class Adviser<T> : IAdviser<T>, IAdviserInternal
        where T : class, IDeclaration
    {
        public ScopedDiagnosticSink Diagnostics { get; }

        IDeclaration IAdviser.Target => this.Target;

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