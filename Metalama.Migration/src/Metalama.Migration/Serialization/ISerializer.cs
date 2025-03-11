// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using PostSharp.Constraints;
using System;

namespace PostSharp.Serialization
{
    /// <summary>
    /// In Metalama, use <see cref="Metalama.Framework.Serialization.ISerializer"/>.
    /// </summary>
    [InternalImplement]
    public interface ISerializer
    {
        object Convert( object value, Type targetType );

        object CreateInstance( Type type, IArgumentsReader constructorArguments );

        void DeserializeFields( ref object obj, IArgumentsReader initializationArguments );

        void SerializeObject( object obj, IArgumentsWriter constructorArguments, IArgumentsWriter initializationArguments );

        bool IsTwoPhase { get; }
    }
}