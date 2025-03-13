// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0169, CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.Dynamic.Cast
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var field = meta.Target.Type.Fields.Single();
            object? clone = null;
            field.With( clone ).Value = meta.Cast( field.Type, ( (ICloneable)field.Value! ).Clone() );

            return default;
        }
    }

    // <target>
    internal class TargetCode : IDisposable
    {
        private int field;

        private int Method( int a )
        {
            return a;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}