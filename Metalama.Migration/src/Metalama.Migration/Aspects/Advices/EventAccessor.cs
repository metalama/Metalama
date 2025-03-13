// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace PostSharp.Aspects.Advices
{
    // ReSharper disable once TypeParameterCanBeVariant
    /// <summary>
    /// In PostSharp, this delegate allowed the run-time code of the aspect to access an event in the target code. In Metalama,
    /// no run-time helper is required because the template directly generates run-time code.
    /// Use invokers (e.g. <see cref="IEvent"/>.<see cref="IEvent.Add"/>) to generate run-time code for any event. 
    /// </summary>
    public delegate void EventAccessor<TDelegate>( TDelegate @delegate );
}