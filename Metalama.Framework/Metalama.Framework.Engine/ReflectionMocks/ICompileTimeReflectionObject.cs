// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using System;

namespace Metalama.Framework.Engine.ReflectionMocks
{
    internal interface ICompileTimeReflectionObject<out T> : IUserExpression
        where T : class, ICompilationElement
    {
        IRef<T> Target { get; }

        Type ReflectionType { get; }
    }
}