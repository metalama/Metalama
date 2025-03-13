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
    [CompileTime]
    [InternalImplement]
    public interface IMetaTarget
    {
        /// <summary>
        /// Gets the target method or constructor, or the accessor if this is a template for a field, property or event,
        /// or throws an exception if the advice does not target a method, constructor or accessor.
        /// </summary>
        /// <seealso href="@templates"/>
        IMethodBase MethodBase { get; }

        /// <summary>
        /// Gets the target field, or throws an exception if the advice does not target a field.
        /// </summary>
        IField Field { get; }

        /// <summary>
        /// Gets the target field or property, or throws an exception if the advice does not target a field or a property.
        /// </summary>
        /// <seealso href="@templates"/>
        IFieldOrProperty FieldOrProperty { get; }

        /// <summary>
        /// Gets the target field or property or indexer, or throws an exception if the advice does not target a field or a property or an indexer.
        /// </summary>
        /// <seealso href="@templates"/>
        IFieldOrPropertyOrIndexer FieldOrPropertyOrIndexer { get; }

        /// <summary>
        /// Gets the target declaration.
        /// </summary>
        IDeclaration Declaration { get; }

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
        IMethod Method { get; }

        /// <summary>
        /// Gets the target constructor, or throws an exception if the advice does not target a constructor.
        /// </summary>
        IConstructor Constructor { get; }

        /// <summary>
        /// Gets the target field or property, or throws an exception if the advice does not target a field or a property.
        /// </summary>
        IProperty Property { get; }

        /// <summary>
        /// Gets the target event, or throws an exception if the advice does not target an event.
        /// </summary>
        IEvent Event { get; }

        /// <summary>
        /// Gets the list of parameters of the current <see cref="Method"/>, <see cref="Constructor"/>,  <see cref="Property"/> or <see cref="Indexer"/> or throws
        /// an exception if the advice does not target a method, constructor, property or indexer.
        /// </summary>
        IParameterList Parameters { get; }

        /// <summary>
        /// Gets the target parameter or throws an exception if the advice does not target a parameter.
        /// </summary>
        IParameter Parameter { get; }

        /// <summary>
        /// Gets the target indexer, or throws an exception if the advice does not target an indexer.
        /// </summary>
        IIndexer Indexer { get; }

        /// <summary>
        /// Gets the code model of current type including the introductions of the current aspect type.
        /// </summary>
        INamedType Type { get; }

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