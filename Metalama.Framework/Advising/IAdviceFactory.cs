// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Validation;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

// IMPORTANT: Keep XML doc in this file in sync with AdviserExtensions.

namespace Metalama.Framework.Advising
{
    /// <summary>
    /// Exposes all factory methods to create advice. Exposed on the <see cref="IAspectBuilder.Advice"/> property
    /// of <see cref="IAspectBuilder{TAspectTarget}"/> or <see cref="IAspectBuilder"/>.
    /// </summary>
    /// <seealso href="@advising-code"/>
    [InternalImplement]
    [CompileTime]
    [PublicAPI]
    public interface IAdviceFactory
    {
        /// <summary>
        /// Gets the mutable compilation that the current aspect builder is working on. It includes all modifications done by
        /// the current aspect in the current type using declarative advices and the <see cref="IAdviceFactory"/>.
        /// </summary>
        ICompilation MutableCompilation { get; }

        /// <summary>
        /// Overrides the implementation of a method.
        /// </summary>
        /// <param name="targetMethod">The method to override.</param>
        /// <param name="template">Name of a method in the aspect class whose implementation will be used as a template.
        ///     This property must be annotated with <see cref="TemplateAttribute"/>. To select a different templates according to the kind of target method
        ///     (such as async or iterator methods), use the constructor of the <see cref="MethodTemplateSelector"/> type. To specify a single
        ///     template for all methods, pass a string.</param>
        /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template method.</param>
        /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property
        ///     of the <see cref="meta"/> API.</param>
        /// <seealso href="@overriding-methods"/>
        IOverrideAdviceResult<IMethod> Override( IMethod targetMethod, in MethodTemplateSelector template, object? args = null, object? tags = null );

        /// <summary>
        /// Introduces a new method or overrides the implementation of the existing one.
        /// </summary>
        /// <param name="targetType">The type into which the method must be introduced.</param>
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
        IIntroductionAdviceResult<IMethod> IntroduceMethod(
            INamedType targetType,
            string template,
            IntroductionScope scope = IntroductionScope.Default,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IMethodBuilder>? buildMethod = null,
            object? args = null,
            object? tags = null );

        /// <summary>
        /// Introduces a finalizer or overrides the implementation of the existing one.
        /// </summary>
        /// <param name="targetType">The type into which the finalizer must be introduced.</param>
        /// <param name="template">Name of the method of the aspect class that will be used as a template for the introduced finalizer. This method must be
        ///     annotated with <see cref="TemplateAttribute"/>. This method can parameters and a return type.</param>
        /// <param name="whenExists">Determines the implementation strategy when a finalizer is already declared in the target type.
        ///     The default strategy is to fail with a compile-time error.</param>
        /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template methods.</param>
        /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property
        ///     of the <see cref="meta"/> API.</param>
        /// <seealso href="@introducing-members"/>
        IIntroductionAdviceResult<IMethod> IntroduceFinalizer(
            INamedType targetType,
            string template,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            object? args = null,
            object? tags = null );

        IIntroductionAdviceResult<IMethod> IntroduceUnaryOperator(
            INamedType targetType,
            string template,
            IType inputType,
            IType resultType,
            OperatorKind kind,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IMethodBuilder>? buildOperator = null,
            object? args = null,
            object? tags = null );

        IIntroductionAdviceResult<IMethod> IntroduceBinaryOperator(
            INamedType targetType,
            string template,
            IType leftType,
            IType rightType,
            IType resultType,
            OperatorKind kind,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IMethodBuilder>? buildOperator = null,
            object? args = null,
            object? tags = null );

        IIntroductionAdviceResult<IMethod> IntroduceConversionOperator(
            INamedType targetType,
            string template,
            IType fromType,
            IType toType,
            bool isImplicit = false,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IMethodBuilder>? buildOperator = null,
            object? args = null,
            object? tags = null );

        /// <summary>
        /// Overrides the implementation of a constructor.
        /// </summary>
        /// <param name="targetConstructor">The constructor to override.</param>
        /// <param name="template">Name of a method in the aspect class whose implementation will be used as a template.
        ///     This method must be annotated with <see cref="TemplateAttribute"/>.</param>
        /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template method.</param>
        /// <param name="tags">An optional opaque object of anonymous type passed to the template method and exposed under the <see cref="meta.Tags"/> property
        ///     of the <see cref="meta"/> API.</param>
        IOverrideAdviceResult<IConstructor> Override( IConstructor targetConstructor, string template, object? args = null, object? tags = null );

        IIntroductionAdviceResult<IConstructor> IntroduceConstructor(
            INamedType targetType,
            string template,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IConstructorBuilder>? buildConstructor = null,
            object? args = null,
            object? tags = null );

        /// <summary>
        /// Overrides a field or property by specifying a property template.
        /// </summary>
        /// <param name="targetFieldOrProperty">The field or property to override.</param>
        /// <param name="template">The name of a property of the aspect class, with a getter, a setter, or both, whose implementation will be used as a template.
        ///     This property must be annotated with <see cref="TemplateAttribute"/>.</param>
        /// <param name="tags">An optional opaque object of anonymous type passed to the template property and exposed under the <see cref="meta.Tags"/> property of the
        ///     <see cref="meta"/> API.</param>
        /// <seealso href="@overriding-fields-or-properties"/>
        IOverrideAdviceResult<IProperty> Override(
            IFieldOrProperty targetFieldOrProperty,
            string template,
            object? tags = null );

        /// <summary>
        /// Overrides a field or property by specifying a method template for the getter, the setter, or both.
        /// </summary>
        /// <param name="targetFieldOrPropertyOrIndexer">The field or property to override.</param>
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
        IOverrideAdviceResult<IPropertyOrIndexer> OverrideAccessors(
            IFieldOrPropertyOrIndexer targetFieldOrPropertyOrIndexer,
            in GetterTemplateSelector getTemplate = default,
            string? setTemplate = null,
            object? args = null,
            object? tags = null );

        IOverrideAdviceResult<IProperty> OverrideAccessors(
            IFieldOrProperty targetFieldOrProperty,
            in GetterTemplateSelector getTemplate = default,
            string? setTemplate = null,
            object? args = null,
            object? tags = null );

        IOverrideAdviceResult<IIndexer> OverrideAccessors(
            IIndexer targetIndexer,
            in GetterTemplateSelector getTemplate = default,
            string? setTemplate = null,
            object? args = null,
            object? tags = null );

        /// <summary>
        /// Introduces a field to the target type by specifying a template.
        /// </summary>
        /// <param name="targetType">The type into which the property must be introduced.</param>
        /// <param name="template">Name of the introduced field.</param>
        /// <param name="scope">Determines the scope (e.g. <see cref="IntroductionScope.Instance"/> or <see cref="IntroductionScope.Static"/>) of the introduced
        ///     field. The default scope is <see cref="IntroductionScope.Instance"/>.</param>
        /// <param name="whenExists">Determines the implementation strategy when a property of the same name is already declared in the target type.
        ///     The default strategy is to fail with a compile-time error.</param>
        /// <param name="buildField"></param>
        /// <param name="tags"></param>
        /// <returns>An <see cref="IPropertyBuilder"/> that allows to dynamically change the name or type of the introduced property.</returns>
        /// <seealso href="@introducing-members"/>
        IIntroductionAdviceResult<IField> IntroduceField(
            INamedType targetType,
            string template,
            IntroductionScope scope = IntroductionScope.Default,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IFieldBuilder>? buildField = null,
            object? tags = null );

        /// <summary>
        /// Introduces a field to the target type by specifying a field name and <see cref="IType"/>.
        /// </summary>
        /// <param name="targetType">The type into which the property must be introduced.</param>
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
        IIntroductionAdviceResult<IField> IntroduceField(
            INamedType targetType,
            string fieldName,
            IType fieldType,
            IntroductionScope scope = IntroductionScope.Default,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IFieldBuilder>? buildField = null,
            object? tags = null );

        /// <summary>
        /// Introduces a field to the target type by specifying a field name and <see cref="Type"/>.
        /// </summary>
        /// <param name="targetType">The type into which the property must be introduced.</param>
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
        IIntroductionAdviceResult<IField> IntroduceField(
            INamedType targetType,
            string fieldName,
            Type fieldType,
            IntroductionScope scope = IntroductionScope.Default,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IFieldBuilder>? buildField = null,
            object? tags = null );

        /// <summary>
        /// Introduces an auto-implemented property to the target type by specifying a property name and <see cref="Type"/>.
        /// </summary>
        /// <param name="targetType">The type into which the property must be introduced.</param>
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
        IIntroductionAdviceResult<IProperty> IntroduceAutomaticProperty(
            INamedType targetType,
            string propertyName,
            Type propertyType,
            IntroductionScope scope = IntroductionScope.Default,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IPropertyBuilder>? buildProperty = null,
            object? tags = null );

        /// <summary>
        /// Introduces an auto-implemented property to the target type by specifying a property name and <see cref="IType"/>.
        /// </summary>
        /// <param name="targetType">The type into which the property must be introduced.</param>
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
        IIntroductionAdviceResult<IProperty> IntroduceAutomaticProperty(
            INamedType targetType,
            string propertyName,
            IType propertyType,
            IntroductionScope scope = IntroductionScope.Default,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IPropertyBuilder>? buildProperty = null,
            object? tags = null );

        /// <summary>
        /// Introduces a property to the target type, or overrides the implementation of an existing one, by specifying a property template.
        /// </summary>
        /// <param name="targetType">The type into which the property must be introduced.</param>
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
        IIntroductionAdviceResult<IProperty> IntroduceProperty(
            INamedType targetType,
            string template,
            IntroductionScope scope = IntroductionScope.Default,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IPropertyBuilder>? buildProperty = null,
            object? tags = null );

        /// <summary>
        /// Introduces a property to the target type, or overrides the implementation of an existing one, by specifying individual template methods for each accessor. 
        /// </summary>
        /// <param name="targetType">The type into which the property must be introduced.</param>
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
        IIntroductionAdviceResult<IProperty> IntroduceProperty(
            INamedType targetType,
            string name,
            string? getTemplate,
            string? setTemplate,
            IntroductionScope scope = IntroductionScope.Default,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IPropertyBuilder>? buildProperty = null,
            object? args = null,
            object? tags = null );

        /// <summary>
        /// Introduces an indexer to the target type, or overrides the implementation of an existing one, by specifying individual template methods for each accessor. 
        /// </summary>
        /// <param name="targetType">The type into which the indexer must be introduced.</param>
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
        IIntroductionAdviceResult<IIndexer> IntroduceIndexer(
            INamedType targetType,
            IType indexType,
            string? getTemplate,
            string? setTemplate,
            IntroductionScope scope = IntroductionScope.Default,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IIndexerBuilder>? buildIndexer = null,
            object? args = null,
            object? tags = null );

        /// <summary>
        /// Introduces an indexer to the target type, or overrides the implementation of an existing one, by specifying individual template methods for each accessor. 
        /// </summary>
        /// <param name="targetType">The type into which the indexer must be introduced.</param>
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
        IIntroductionAdviceResult<IIndexer> IntroduceIndexer(
            INamedType targetType,
            Type indexType,
            string? getTemplate,
            string? setTemplate,
            IntroductionScope scope = IntroductionScope.Default,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IIndexerBuilder>? buildIndexer = null,
            object? args = null,
            object? tags = null );

        /// <summary>
        /// Introduces an indexer to the target type, or overrides the implementation of an existing one, by specifying individual template methods for each accessor. 
        /// </summary>
        /// <param name="targetType">The type into which the indexer must be introduced.</param>
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
        IIntroductionAdviceResult<IIndexer> IntroduceIndexer(
            INamedType targetType,
            IReadOnlyList<(IType Type, string Name)> indices,
            string? getTemplate,
            string? setTemplate,
            IntroductionScope scope = IntroductionScope.Default,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IIndexerBuilder>? buildIndexer = null,
            object? args = null,
            object? tags = null );

        /// <summary>
        /// Introduces an indexer to the target type, or overrides the implementation of an existing one, by specifying individual template methods for each accessor. 
        /// </summary>
        /// <param name="targetType">The type into which the indexer must be introduced.</param>
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
        IIntroductionAdviceResult<IIndexer> IntroduceIndexer(
            INamedType targetType,
            IReadOnlyList<(Type Type, string Name)> indices,
            string? getTemplate,
            string? setTemplate,
            IntroductionScope scope = IntroductionScope.Default,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IIndexerBuilder>? buildIndexer = null,
            object? args = null,
            object? tags = null );

        /// <summary>
        /// Overrides an event by specifying a template for the adder, the remover, and/or the raiser.
        /// </summary>
        /// <param name="targetEvent">The event to be overridden.</param>
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
        IOverrideAdviceResult<IEvent> OverrideAccessors(
            IEvent targetEvent,
            string? addTemplate,
            string? removeTemplate,
            string? raiseTemplate = null,
            object? args = null,
            object? tags = null );

        /// <summary>
        /// Introduces a new event to the target type, or overrides the implementation of an existing one, by specifying an event template.
        /// </summary>
        /// <param name="targetType">The type into which the event must be introduced.</param>
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
        IIntroductionAdviceResult<IEvent> IntroduceEvent(
            INamedType targetType,
            string eventTemplate,
            IntroductionScope scope = IntroductionScope.Default,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IEventBuilder>? buildEvent = null,
            object? tags = null );

        /// <summary>
        /// Introduces a new event to the target type, or overrides the implementation of an existing one, by specifying individual template methods
        /// for the adder, the remover, and the raiser.
        /// </summary>
        /// <param name="targetType">The type into which the event must be introduced.</param>
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
        IIntroductionAdviceResult<IEvent> IntroduceEvent(
            INamedType targetType,
            string eventName,
            string addTemplate,
            string removeTemplate,
            string? raiseTemplate = null,
            IntroductionScope scope = IntroductionScope.Default,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<IEventBuilder>? buildEvent = null,
            object? args = null,
            object? tags = null );

        /// <summary>
        /// Makes a type implement a new interface specified as an <see cref="INamedType"/>.
        /// Interface members can be introduced by marking an aspect member by <see cref="InterfaceMemberAttribute"/>, 
        /// <see cref="IntroduceAttribute"/> or programmatically using <c>Introduce</c> methods.
        /// </summary>
        /// <param name="targetType">The type that must implement the new interface.</param>
        /// <param name="interfaceType">The type of the implemented interface.</param>
        /// <param name="whenExists">Determines the implementation strategy when the interface is already implemented by the target type.
        ///     The default strategy is to fail with a compile-time error.</param>
        /// <param name="tags">An optional opaque object of anonymous type passed to <see cref="InterfaceMemberAttribute"/> templates and exposed under the <see cref="meta.Tags"/> property of the
        ///     <see cref="meta"/> API. This parameter does not affect members introduced using <see cref="IntroduceAttribute"/> or programmatically.</param>
        /// <seealso href="@implementing-interfaces"/>
        IImplementInterfaceAdviceResult ImplementInterface(
            INamedType targetType,
            INamedType interfaceType,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            object? tags = null );

        /// <summary>
        /// Makes a type implement a new interface specified as a reflection <see cref="Type"/>.
        /// Interface members can be introduced by marking an aspect member by <see cref="InterfaceMemberAttribute"/>, 
        /// <see cref="IntroduceAttribute"/> or programmatically using <c>Introduce</c> methods.
        /// </summary>
        /// <param name="targetType">The type that must implement the new interface.</param>
        /// <param name="interfaceType">The type of the implemented interface.</param>
        /// <param name="whenExists">Determines the implementation strategy when the interface is already implemented by the target type.
        ///     The default strategy is to fail with a compile-time error.</param>
        /// <param name="tags">An optional opaque object of anonymous type passed to <see cref="InterfaceMemberAttribute"/> templates and exposed under the <see cref="meta.Tags"/> property of the
        ///     <see cref="meta"/> API. This parameter does not affect members introduced using <see cref="IntroduceAttribute"/> or programmatically.</param>
        /// <seealso href="@implementing-interfaces"/>
        IImplementInterfaceAdviceResult ImplementInterface(
            INamedType targetType,
            Type interfaceType,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            object? tags = null );

        /// <summary>
        /// Adds a type or instance initializer by using a template. 
        /// </summary>
        /// <param name="targetType">The type into which the initializer should be added.</param>
        /// <param name="template">The name of the template. This method must have no run-time parameter, be of <c>void</c> return type, and be annotated with the <see cref="TemplateAttribute"/> custom attribute.</param>
        /// <param name="kind">The type of initializer to add.</param>
        /// <param name="tags">An optional opaque object of anonymous type passed to templates and exposed under the <see cref="meta.Tags"/> property of the
        ///     <see cref="meta"/> API.</param>
        /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.</param>
        IAddInitializerAdviceResult AddInitializer(
            INamedType targetType,
            string template,
            InitializerKind kind,
            object? tags = null,
            object? args = null );

        /// <summary>
        /// Adds a type or instance initializer by specifying an <see cref="IStatement"/>. 
        /// </summary>
        /// <param name="targetType">The type into which the initializer should be added.</param>
        /// <param name="statement">The statement to be inserted at the top of constructors.</param>
        /// <param name="kind">The type of initializer to add.</param>
        IAddInitializerAdviceResult AddInitializer(
            INamedType targetType,
            IStatement statement,
            InitializerKind kind );

        /// <summary>
        /// Adds an initializer to a specific constructor by using a template.
        /// </summary>
        /// <param name="targetConstructor">The constructor into which the initializer should be added.</param>
        /// <param name="template">The name of the template. This method must have no run-time parameter, be of <c>void</c> return type, and be annotated with the <see cref="TemplateAttribute"/> custom attribute.</param>
        /// <param name="tags">An optional opaque object of anonymous type  passed to templates and exposed under the <see cref="meta.Tags"/> property of the
        ///     <see cref="meta"/> API.</param>
        /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.</param>
        IAddInitializerAdviceResult AddInitializer( IConstructor targetConstructor, string template, object? tags = null, object? args = null );

        /// <summary>
        /// Adds an initializer to a specific constructor by specifying an <see cref="IStatement"/>.
        /// </summary>
        /// <param name="targetConstructor">The constructor into which the initializer should be added.</param>
        /// <param name="statement">The statement to be inserted at the top of the constructor.</param>
        IAddInitializerAdviceResult AddInitializer( IConstructor targetConstructor, IStatement statement );

        /// <summary>
        /// Adds a contract to a parameter. Contracts are usually used to validate parameters (pre- or post-conditions) or to normalize their value (null-to-empty, trimming, normalizing case, ...).
        /// </summary>
        /// <param name="targetParameter">The parameter to which the contract should be added.</param>
        /// <param name="template">The name of the template method. This method must have a single run-time parameter named <c>value</c>, and be annotated with the <see cref="TemplateAttribute"/> custom attribute.</param>
        /// <param name="direction">Direction of the data flow to which the contract should apply. See <see cref="ContractDirection"/> for details.</param>
        /// <param name="tags">An optional opaque object of anonymous type passed to templates and exposed under the <see cref="meta.Tags"/> property of the
        ///     <see cref="meta"/> API.</param>
        /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.</param>
        IAddContractAdviceResult<IParameter> AddContract(
            IParameter targetParameter,
            string template,
            ContractDirection direction = ContractDirection.Default,
            object? tags = null,
            object? args = null );

        /// <summary>
        /// Adds a contract to a field, property or indexer. Contracts are usually used to validate the value assigned to fields properties or indexers or to normalize their value (null-to-empty, trimming, normalizing case, ...)
        /// before assignment. Alternatively, a contract can be used to validate the value <i>returned</i> by a property or indexer, in which case the <paramref name="direction"/> parameter should be set to <see cref="ContractDirection.Output"/>.
        /// </summary>
        /// <param name="targetMember">The field, property or indexer to which the contract should be added.</param>
        /// <param name="template">The name of the template method. This method must have a single run-time parameter named <c>value</c>, and be annotated with the <see cref="TemplateAttribute"/> custom attribute.</param>
        /// <param name="direction">Direction of the data flow to which the contract should apply. See <see cref="ContractDirection"/> for details.</param>
        /// <param name="tags">An optional opaque object of anonymous type passed to templates and exposed under the <see cref="meta.Tags"/> property of the
        ///     <see cref="meta"/> API.</param>
        /// <param name="args">An object (typically of anonymous type) whose properties map to parameters or type parameters of the template.</param>
        IAddContractAdviceResult<IFieldOrPropertyOrIndexer> AddContract(
            IFieldOrPropertyOrIndexer targetMember,
            string template,
            ContractDirection direction = ContractDirection.Default,
            object? tags = null,
            object? args = null );

        /// <summary>
        /// Adds a custom attribute to a given declaration.
        /// </summary>
        /// <param name="targetDeclaration">The declaration to which the custom attribute should be added.</param>
        /// <param name="attribute">The custom attribute to be added. It can be an existing <see cref="IAttribute"/>, or you can use <see cref="AttributeConstruction"/>
        ///     to specify a new attribute.</param>
        /// <param name="whenExists">Specifies the strategy to follow when an attribute of the same type already exists on the target declaration. <see cref="OverrideStrategy.Fail"/> will fail the
        ///     compilation with an error and is the default strategy. <see cref="OverrideStrategy.Ignore"/> will silently ignore the introduction. <see cref="OverrideStrategy.Override"/> will remove
        ///     all previous instances and replace them by the new one. <see cref="OverrideStrategy.New"/> will add the new instance regardless.</param>
        IIntroductionAdviceResult<IAttribute> IntroduceAttribute(
            IDeclaration targetDeclaration,
            IAttributeData attribute,
            OverrideStrategy whenExists = OverrideStrategy.Default );

        /// <summary>
        /// Removes all custom attributes of a given <see cref="INamedType"/> from a given declaration.
        /// </summary>
        /// <param name="targetDeclaration">The declaration from which custom attributes have to be removed.</param>
        /// <param name="attributeType">The type of custom attributes to be removed.</param>
        IRemoveAttributesAdviceResult RemoveAttributes(
            IDeclaration targetDeclaration,
            INamedType attributeType );

        /// <summary>
        /// Removes all custom attributes of a given <see cref="Type"/> from a given declaration.
        /// </summary>
        /// <param name="targetDeclaration">The declaration from which custom attributes have to be removed.</param>
        /// <param name="attributeType">The type of custom attributes to be removed.</param>
        IRemoveAttributesAdviceResult RemoveAttributes(
            IDeclaration targetDeclaration,
            Type attributeType );

        // We require an explicit TypedConstant value instead of providing 'default' as the default value because a next Metalama version may allow to
        // append parameters without a default value; in this case, we would have a different signature.

        /// <summary>
        /// Appends a parameter to a constructor by specifying its name and <see cref="IType"/>.
        /// </summary>
        /// <param name="constructor">The constructor into which the new parameter will be appended.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="parameterType">The type of the parameter.</param>
        /// <param name="defaultValue">The default value of the parameter (required). It must be type-compatible with <paramref name="parameterType"/>.
        ///     To specify <c>default</c> as the default value, use <see cref="TypedConstant.Default(Metalama.Framework.Code.IType)"/>.</param>
        /// <param name="pullAction">An optional delegate that returns a <see cref="PullAction"/> specifying how to pull the new parameter from other child constructors.
        ///     A <c>null</c> value is equivalent to <see cref="PullAction.None"/>, i.e. <paramref name="defaultValue"/> of the parameter will be used.
        /// </param>
        /// <param name="attributes"></param>
        IIntroductionAdviceResult<IParameter> IntroduceParameter(
            IConstructor constructor,
            string parameterName,
            IType parameterType,
            TypedConstant defaultValue,
            Func<IParameter, IConstructor, PullAction>? pullAction = null,
            ImmutableArray<AttributeConstruction> attributes = default );

        /// <summary>
        /// Appends a parameter to a constructor by specifying its name and <see cref="Type"/>.
        /// </summary>
        /// <param name="constructor">The constructor into which the new parameter will be appended.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="parameterType">The type of the parameter.</param>
        /// <param name="defaultValue">The default value of the parameter (required). It must be type-compatible with <paramref name="parameterType"/>.
        ///     To specify <c>default</c> as the default value, use <see cref="TypedConstant.Default(Metalama.Framework.Code.IType)"/>.</param>
        /// <param name="pullAction">An optional delegate that returns a <see cref="PullAction"/> specifying how to pull the new parameter from other child constructors.
        ///     A <c>null</c> value is equivalent to <see cref="PullAction.None"/>, i.e. <paramref name="defaultValue"/> of the parameter will be used.
        /// </param>
        /// <param name="attributes"></param>
        IIntroductionAdviceResult<IParameter> IntroduceParameter(
            IConstructor constructor,
            string parameterName,
            Type parameterType,
            TypedConstant defaultValue,
            Func<IParameter, IConstructor, PullAction>? pullAction = null,
            ImmutableArray<AttributeConstruction> attributes = default );

        IClassIntroductionAdviceResult IntroduceClass(
            INamespaceOrNamedType targetNamespaceOrType,
            string name,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<INamedTypeBuilder>? buildType = null );

        IClassIntroductionAdviceResult IntroduceInterface(
            INamespaceOrNamedType targetNamespaceOrType,
            string name,
            OverrideStrategy whenExists = OverrideStrategy.Default,
            Action<INamedTypeBuilder>? buildType = null );

        IAdviser<INamespace> WithNamespace(
            INamespace targetNamespace,
            string name );

        /// <summary>
        /// Adds a custom annotation to a declaration. An annotation is an arbitrary but serializable object that can then be retrieved
        /// using the <see cref="DeclarationEnhancements{T}.GetAnnotations{TAnnotation}"/> method of the <see cref="DeclarationExtensions.Enhancements{T}"/> object.
        /// Annotations are a way of communication between aspects or classes of aspects.
        /// </summary>
        /// <param name="declaration">The declaration to which the annotation should be added.</param>
        /// <param name="annotation">The annotation.</param>
        /// <param name="export">A value indicating whether the annotation should be exported and made visible to other projects.
        /// Unless this parameter is set to <c>true</c>, the annotation will only be visible to the current project.</param>
        /// <typeparam name="TDeclaration">The type of declaration.</typeparam>
        void AddAnnotation<TDeclaration>( TDeclaration declaration, IAnnotation<TDeclaration> annotation, bool export = false )
            where TDeclaration : class, IDeclaration;

        /// <summary>
        /// Returns a copy of the current <see cref="IAdviceFactory"/> that will use the specified object to find template methods.
        /// </summary>
        /// <param name="templateProvider">A <see cref="TemplateProvider"/>.</param>
        /// <returns>An <see cref="IAdviceFactory"/>.</returns>
        IAdviceFactory WithTemplateProvider( TemplateProvider templateProvider );

        /// <summary>
        /// Returns a copy of the current <see cref="IAdviceFactory"/> that will use the specified object to find template methods.
        /// </summary>
        /// <param name="templateProvider">An <see cref="ITemplateProvider"/>.</param>
        /// <returns>An <see cref="IAdviceFactory"/>.</returns>
        IAdviceFactory WithTemplateProvider( ITemplateProvider templateProvider );
    }
}