// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Extensibility;
using System;
using System.Reflection;

namespace PostSharp.Aspects
{
    /// <summary>
    /// In Metalama, use <see cref="OverrideMethodAspect"/> and write your own try/catch block.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor |
        AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Property |
        AttributeTargets.Struct | AttributeTargets.Interface,
        AllowMultiple = true,
        Inherited = false )]
    [MulticastAttributeUsage(
        MulticastTargets.Method | MulticastTargets.StaticConstructor | MulticastTargets.InstanceConstructor,
        TargetMemberAttributes =
            MulticastAttributes.NonAbstract | MulticastAttributes.AnyScope | MulticastAttributes.AnyVisibility |
            MulticastAttributes.Managed,
        AllowMultiple = true )]
    public abstract class OnExceptionAspect : MethodLevelAspect, IOnExceptionAspect
    {
        /// <inheritdoc/>
        public virtual void OnException( MethodExecutionArgs args ) { }

        /// <summary>
        /// In Metalama, implement different methods <see cref="OverrideMethodAspect.OverrideMethod"/>, <see cref="OverrideMethodAspect.OverrideAsyncMethod"/>,
        /// <see cref="OverrideMethodAspect.OverrideEnumerableMethod"/> or <see cref="OverrideMethodAspect.OverrideEnumeratorMethod"/>, and
        /// set the properties <see cref="OverrideMethodAspect.UseAsyncTemplateForAnyAwaitable"/> or <see cref="OverrideMethodAspect.UseEnumerableTemplateForAnyEnumerable"/>.
        /// </summary>
        protected SemanticallyAdvisedMethodKinds SemanticallyAdvisedMethodKinds { get; set; }

        /// <summary>
        /// There is no equivalent in Metalama. Unsupported targets will throw an exception.
        /// </summary>
        public new UnsupportedTargetAction UnsupportedTargetAction
        {
            get => base.UnsupportedTargetAction;
            set => base.UnsupportedTargetAction = value;
        }

        /// <summary>
        /// There is no equivalent in Metalama because you write your own try/catch code in the template.
        /// </summary>
        public virtual Type GetExceptionType( MethodBase targetMethod )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// No equivalent in Metalama.
        /// </summary>
        protected sealed override AspectConfiguration CreateAspectConfiguration()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override void SetAspectConfiguration( AspectConfiguration aspectConfiguration, MethodBase targetMethod )
        {
            throw new NotImplementedException();
        }
    }
}