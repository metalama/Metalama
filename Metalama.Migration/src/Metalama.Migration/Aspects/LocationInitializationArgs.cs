// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using PostSharp.Aspects.Internals;
using System;

namespace PostSharp.Aspects
{
    /// <summary>
    /// In PostSharp, this object exposed the run-time execution context to the advice. However, in Metalama, advice do not execute at run time.
    /// Instead, advice are templates that generate run-time code. This run-time code does not need helper objects to represent the execution context.
    /// </summary>
    public sealed class LocationInitializationArgs : LocationLevelAdviceArgs
    {
        internal LocationInitializationArgs()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, use the <c>value</c> template parameter.
        /// </summary>
        public override object Value { get; set; }
    }
}