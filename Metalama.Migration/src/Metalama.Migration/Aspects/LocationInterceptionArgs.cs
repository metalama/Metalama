// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using PostSharp.Aspects.Internals;
using System;

namespace PostSharp.Aspects
{
    /// <summary>
    /// In PostSharp, this object exposed the run-time execution context to the advice. However, in Metalama, advice do not execute at run time.
    /// Instead, advice are templates that generate run-time code. This run-time code does not need helper objects to represent the execution context.
    /// </summary>
    [PublicAPI]
    public abstract class LocationInterceptionArgs : LocationLevelAdviceArgs, ILocationInterceptionArgs
    {
        protected LocationInterceptionArgs( Arguments index )
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public abstract ILocationBinding Binding { get; }

        /// <inheritdoc/>
        public Arguments Index { get; }

        /// <inheritdoc/>
        public abstract void ProceedGetValue();

        /// <inheritdoc/>
        public abstract void ProceedSetValue();

        /// <inheritdoc/>
        public abstract object GetCurrentValue();

        /// <inheritdoc/>
        public abstract void SetNewValue( object value );

        /// <inheritdoc/>
        public abstract void Execute<TPayload>( ILocationInterceptionArgsAction<TPayload> action, ref TPayload payload );
    }
}