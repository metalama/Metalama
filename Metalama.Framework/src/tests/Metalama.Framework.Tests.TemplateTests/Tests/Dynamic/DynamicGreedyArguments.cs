// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.ComponentModel;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.AspectTests.Templating.Dynamic.DynamicGreedyArguments
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            // Dynamic argument of build-time-only method.
            meta.Target.Method.Invoke( new PropertyChangedEventArgs( meta.Target.Parameters[0].Name ) );

            // Invocation in dynamic context.
            meta.This.PropertyChanged.Invoke( new PropertyChangedEventArgs( meta.Target.Parameters[0].Name ) );

            // Conditional access in dynamic context.
            meta.This.PropertyChanged.Invoke( new PropertyChangedEventArgs( meta.Target.Parameters[0].Name ) );

            return default;
        }
    }

    // <target>
    internal class TargetCode
    {
        private int Method( PropertyChangedEventArgs a )
        {
            return 0;
        }
    }
}