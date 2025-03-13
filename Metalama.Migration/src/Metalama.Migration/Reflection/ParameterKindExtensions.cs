// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;

namespace PostSharp.Reflection
{
    /// <summary>
    /// In Metalama, use <see cref="RefKindExtensions"/>.
    /// </summary>
    public static class ParameterKindExtensions
    {
        public static bool IsInputParameter( this ParameterKind parameterKind )
        {
            throw new NotImplementedException();
        }

        public static bool IsOutputParameter( this ParameterKind parameterKind )
        {
            throw new NotImplementedException();
        }

        public static bool IsReturn( this ParameterKind parameterKind )
        {
            throw new NotImplementedException();
        }

        public static bool IsByRefParameter( this ParameterKind parameterKind )
        {
            throw new NotImplementedException();
        }

        public static bool IsParameter( this ParameterKind parameterKind )
        {
            throw new NotImplementedException();
        }
    }
}