// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Invokers;
using Metalama.Framework.Project;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Provides access to the target declaration being transformed by an aspect template. Access via <see cref="meta"/><c>.Target</c> in template code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IMetaTarget"/> exposes properties to access the specific declaration type being targeted by a template (e.g., method, property, field, event).
    /// Use these properties to interact with and reference the target declaration within template code.
    /// </para>
    /// <para>
    /// For example, access <see cref="meta"/><c>.Target.Method</c> in a method template to get the method being overridden,
    /// or <see cref="meta"/><c>.Target.FieldOrProperty</c> in a property template to access the underlying field or property.
    /// </para>
    /// </remarks>
    /// <seealso cref="meta"/>
    /// <seealso href="@templates"/>
    /// <seealso href="@template-overview"/>
    [CompileTime]
    [InternalImplement]
    public interface IMetaTarget
    {
        /// <summary>
        /// Gets the target method or constructor, or the accessor if this is a template for a field, property or event,
        /// or throws an exception if the advice does not target a method, constructor or accessor.
        /// </summary>
        /// <seealso cref="Method"/>
        /// <seealso cref="Constructor"/>
        /// <seealso href="@templates"/>
        IMethodBase MethodBase { get; }

        /// <summary>
        /// Gets the target field, or throws an exception if the advice does not target a field.
        /// </summary>
        /// <seealso cref="FieldOrProperty"/>
        /// <seealso cref="Property"/>
        IField Field { get; }

        /// <summary>
        /// Gets the target field or property, or throws an exception if the advice does not target a field or a property.
        /// </summary>
        /// <remarks>
        /// Access the underlying value using <c>meta.Target.FieldOrProperty.Value</c> in property override templates.
        /// This works for both reading and writing the value.
        /// </remarks>
        /// <seealso cref="IExpression.Value"/>
        /// <seealso cref="Field"/>
        /// <seealso cref="Property"/>
        /// <seealso cref="FieldOrPropertyOrIndexer"/>
        /// <seealso href="@overriding-fields-or-properties"/>
        /// <seealso href="@templates"/>
        IFieldOrProperty FieldOrProperty { get; }

        /// <summary>
        /// Gets the target field or property or indexer, or throws an exception if the advice does not target a field or a property or an indexer.
        /// </summary>
        /// <seealso cref="FieldOrProperty"/>
        /// <seealso cref="Indexer"/>
        /// <seealso href="@templates"/>
        IFieldOrPropertyOrIndexer FieldOrPropertyOrIndexer { get; }

        /// <summary>
        /// Gets the target declaration.
        /// </summary>
        IDeclaration Declaration { get; }

        /// <summary>
        /// Gets the target field, property, or parameter as a unified <see cref="IExpression"/>,
        /// or throws an exception if the declaration does not implement <see cref="IExpression"/>.
        /// </summary>
        /// <remarks>
        /// This property is useful when authoring contract templates because it provides unified access to <see cref="IField"/>, <see cref="IProperty"/>, and <see cref="IParameter"/>
        /// as an <see cref="IExpression"/>.
        /// </remarks>
        /// <seealso cref="Field"/>
        /// <seealso cref="FieldOrProperty"/>
        /// <seealso cref="Parameter"/>
        /// <seealso cref="IExpression"/>
        /// <seealso href="@templates"/>
        IExpression Expression { get; }

        /// <summary>
        /// Gets the target member (method, constructor, field, property or event, but not a nested type), or
        /// throws an exception if the advice does not target a member.
        /// </summary>
        /// <seealso href="@templates"/>
        IMember Member { get; }

        /// <summary>
        /// Gets the target method, or the accessor if this is a template for a field, property or event,
        /// or throws an exception if the advice does not target a method or accessor.
        /// </summary>
        /// <remarks>
        /// To invoke the method, use <see cref="IMethodInvoker.Invoke(object?[])"/>,
        /// e.g. <c>meta.Target.Method.Invoke(1, 2, 3);</c>.
        /// </remarks>
        /// <seealso cref="MethodBase"/>
        /// <seealso cref="Parameters"/>
        /// <seealso href="@overriding-methods"/>
        IMethod Method { get; }

        /// <summary>
        /// Gets the target constructor, or throws an exception if the advice does not target a constructor.
        /// </summary>
        /// <seealso cref="MethodBase"/>
        /// <seealso cref="Parameters"/>
        /// <seealso href="@overriding-constructors"/>
        IConstructor Constructor { get; }

        /// <summary>
        /// Gets the target property, or throws an exception if the advice does not target a property.
        /// </summary>
        /// <seealso cref="FieldOrProperty"/>
        /// <seealso cref="Indexer"/>
        IProperty Property { get; }

        /// <summary>
        /// Gets the target event, or throws an exception if the advice does not target an event.
        /// </summary>
        /// <remarks>
        /// Programmatically add or remove handlers using <c>meta.Target.Event.Add(handler)</c> or
        /// <c>meta.Target.Event.Remove(handler)</c> in event override templates.
        /// </remarks>
        /// <seealso cref="IEventInvoker.Add(dynamic?)"/>
        /// <seealso cref="IEventInvoker.Remove(dynamic?)"/>
        /// <seealso cref="Member"/>
        /// <seealso href="@overriding-events"/>
        IEvent Event { get; }

        /// <summary>
        /// Gets the list of parameters of the current <see cref="Method"/>, <see cref="Constructor"/>,  <see cref="Property"/> or <see cref="Indexer"/> or throws
        /// an exception if the advice does not target a method, constructor, property or indexer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Access parameter values using indexer syntax: <c>meta.Target.Parameters[0].Value</c> or <c>meta.Target.Parameters["paramName"].Value</c>.
        /// You can also modify parameter values before calling <c>meta.Proceed()</c>, and the modified values will be passed to the original implementation.
        /// </para>
        /// </remarks>
        /// <seealso cref="IParameterList"/>
        /// <seealso cref="Method"/>
        /// <seealso cref="Constructor"/>
        /// <seealso cref="Parameter"/>
        /// <seealso href="@overriding-methods"/>
        IParameterList Parameters { get; }

        /// <summary>
        /// Gets the target parameter or throws an exception if the advice does not target a parameter.
        /// </summary>
        /// <seealso cref="Parameters"/>
        IParameter Parameter { get; }

        /// <summary>
        /// Gets the target indexer, or throws an exception if the advice does not target an indexer.
        /// </summary>
        /// <seealso cref="FieldOrPropertyOrIndexer"/>
        /// <seealso cref="Property"/>
        /// <seealso cref="Parameters"/>
        IIndexer Indexer { get; }

        /// <summary>
        /// Gets the current type including the introductions of the current aspect type.
        /// If the current context is within an extension block, this property evaluates to the declaring type of the extension block.
        /// </summary>
        /// <seealso cref="ExtensionBlock"/>
        INamedType Type { get; }

        /// <summary>
        /// Gets the current extension block.
        /// </summary>
        IExtensionBlock? ExtensionBlock { get; }

        /// <summary>
        /// Gets the code model of the whole compilation.
        /// </summary>
        ICompilation Compilation { get; }

        /// <summary>
        /// Gets the project being compiled.
        /// </summary>
        IProject Project { get; }

        /// <summary>
        /// Gets the direction of the contract for which the template is being expanded.
        /// </summary>
        ContractDirection ContractDirection { get; }
    }
}