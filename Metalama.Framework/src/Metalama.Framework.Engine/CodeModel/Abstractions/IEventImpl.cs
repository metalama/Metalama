// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.CodeModel.Abstractions
{
    internal interface IEventImpl : IEvent, IHasAccessorsImpl
    {
        /// <summary>
        /// Gets the raise method for internal advice/template binding purposes.
        /// Unlike <see cref="IEvent.RaiseMethod"/>, which returns <c>null</c> for non-field-like events,
        /// this always returns a raise method (a <see cref="Source.Pseudo.PseudoRaiser"/> for source events, or an internal accessor for introduced events)
        /// so that the invoke template can be bound even for accessor-based events.
        /// </summary>
        IMethod GetRaiseMethodForAdvice();
    }
}