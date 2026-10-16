// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors
{
    internal sealed partial class ForwardingConstructorHelper
    {
        /// <summary>
        /// Describes the forwarder that <see cref="ApplyForwarder"/> creates or extends for a mutated constructor.
        /// It is returned by <see cref="PlanForwarder"/>.
        /// </summary>
        /// <param name="ExistingForwarder">A forwarder created by an earlier advice, which receives the new argument, or <c>null</c>.</param>
        /// <param name="Action">The action returned by the overloading strategy when there is no existing forwarder.</param>
        /// <param name="PreMutationParameters">The source parameters of the mutated constructor.</param>
        public readonly record struct ForwardingPlan(
            IConstructor? ExistingForwarder,
            ConstructorOverloadingAction Action,
            IReadOnlyList<IParameter>? PreMutationParameters )
        {
            /// <summary>
            /// Gets a value indicating whether the mutated constructor has a forwarder after <see cref="ApplyForwarder"/> runs.
            /// </summary>
            public bool HasForwarder => this.ExistingForwarder != null || this.Action.Kind != ConstructorOverloadingActionKind.None;
        }
    }
}
