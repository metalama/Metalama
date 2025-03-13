// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.CodeModel.Source
{
    internal sealed class AssemblyIdentityModel : IAssemblyIdentity
    {
        public AssemblyIdentity Identity { get; }

        public AssemblyIdentityModel( AssemblyIdentity assemblyIdentity )
        {
            this.Identity = assemblyIdentity;
        }

        public string Name => this.Identity.Name;

        public Version Version => this.Identity.Version;

        public string CultureName => this.Identity.CultureName;

        public ImmutableArray<byte> PublicKey => this.Identity.PublicKey;

        public ImmutableArray<byte> PublicKeyToken => this.Identity.PublicKeyToken;

        public bool IsStrongNamed => this.Identity.IsStrongName;

        public bool HasPublicKey => this.Identity.HasPublicKey;

        public override string ToString() => this.Identity.ToString();

        public bool Equals( IAssemblyIdentity? other )
        {
            if ( ReferenceEquals( null, other ) )
            {
                return false;
            }

            if ( ReferenceEquals( this, other ) )
            {
                return true;
            }

            return this.Identity.Equals( ((AssemblyIdentityModel) other).Identity );
        }

        public override bool Equals( object? obj ) => ReferenceEquals( this, obj ) || (obj is IAssemblyIdentity other && this.Equals( other ));

        public override int GetHashCode() => this.Identity.GetHashCode();
    }
}