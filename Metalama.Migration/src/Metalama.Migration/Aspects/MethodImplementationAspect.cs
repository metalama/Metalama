// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Extensibility;
using System;

namespace PostSharp.Aspects
{
    /// <summary>
    /// In Metalama, use <see cref="OverrideMethodAspect"/>.
    /// </summary>
    [MulticastAttributeUsage(
        MulticastTargets.Method,
        AllowMultiple = false,
        PersistMetaData = false,
        Inheritance = MulticastInheritance.None )]
    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method |
        AttributeTargets.Property |
        AttributeTargets.Event | AttributeTargets.Struct,
        AllowMultiple = true )]
    public abstract class MethodImplementationAspect : MethodLevelAspect, IMethodInterceptionAspect
    {
        /// <summary>
        /// In Metalama, override <see cref="OverrideMethodAspect.OverrideMethod"/>.
        /// </summary>
        public abstract void OnInvoke( MethodInterceptionArgs args );

        /// <inheritdoc/>
        protected override AspectConfiguration CreateAspectConfiguration()
        {
            return new MethodInterceptionAspectConfiguration();
        }
    }
}