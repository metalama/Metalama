// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code.DeclarationBuilders;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PostSharp.Reflection
{
    /// <summary>
    /// In Metalama, use <see cref="AttributeConstruction"/>.
    /// </summary>
    [PublicAPI]
    public sealed class ObjectConstruction
    {
        public ObjectConstruction( string typeName, params object[] constructorArguments )
        {
            throw new NotImplementedException();
        }

        public ObjectConstruction( Type type, params object[] constructorArguments )
        {
            throw new NotImplementedException();
        }

        public ObjectConstruction( ConstructorInfo constructor, params object[] constructorArguments )
        {
            throw new NotImplementedException();
        }

        public ObjectConstruction( CustomAttributeData customAttributeData )
        {
            throw new NotImplementedException();
        }

        public string TypeName { get; }

        public ConstructorInfo Constructor { get; }

        public IList<object> ConstructorArguments { get; }

        public IDictionary<string, object> NamedArguments { get; }
    }
}