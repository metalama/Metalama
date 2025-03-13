// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using PostSharp.Aspects.Configuration;
using PostSharp.Extensibility;
using System;

namespace PostSharp.Aspects
{
    /// <summary>
    /// Not implemented in Metalama.
    /// </summary>
    [Obsolete( "", true )]
    [PublicAPI]
    public sealed class ManagedResourceIntroductionAspect : IManagedResourceIntroductionAspect, IAspectBuildSemantics
    {
        public ManagedResourceIntroductionAspect( string name, byte[] data )
        {
            throw new NotImplementedException();
        }

        public ManagedResourceIntroductionAspect( string name, Func<byte[]> dataProvider )
        {
            throw new NotImplementedException();
        }

        public string Name { get; }

        public byte[] Data { get; }

        public Func<byte[]> DataProvider { get; }

        /// <inheritdoc/>
        bool IValidableAnnotation.CompileTimeValidate( object target ) => true;

        AspectConfiguration IAspectBuildSemantics.GetAspectConfiguration( object targetElement )
        {
            throw new NotImplementedException();
        }
    }
}