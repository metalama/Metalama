// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Reflection;

namespace PostSharp.Aspects
{
    /// <summary>
    /// In PostSharp, this object exposed the run-time execution context to the advice. However, in Metalama, advice do not execute at run time.
    /// Instead, advice are templates that generate run-time code. This run-time code does not need helper objects to represent the execution context.
    /// </summary>
    [PublicAPI]
    public class MethodExecutionArgs : AdviceArgs
    {
        private MethodExecutionArgs()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, use <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.Method"/>. If you need a run-time object,
        /// call <see cref="IMethod"/>.<see cref="IMethod.ToMethodInfo"/>.
        /// </summary>
        public MethodBase Method { get; }

        /// <summary>
        /// In Metalama, use <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.Parameters"/>
        /// </summary>
        public Arguments Arguments { get; }

        /// <summary>
        /// In Metalama, when you call <see cref="meta"/>.<see cref="meta.Proceed"/>, store the return value in a local variable.
        /// </summary>
        public object ReturnValue { get; set; }

        /// <summary>
        /// There is no equivalent in Metalama and no workaround. 
        /// </summary>
        [Obsolete( "", true )]
        public object YieldValue { get; set; }

        /// <summary>
        /// In Metalama, when you have your own <c>try</c>/<c>catch</c>, this is the exception variable of the <c>catch</c> block.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// There is no equivalent in Metalama because you can code any control flow in the template.
        /// </summary>
        public FlowBehavior FlowBehavior { get; set; }

        /// <summary>
        /// In Metalama, use a local variable in the template.
        /// </summary>
        public object MethodExecutionTag { get; set; }
    }
}