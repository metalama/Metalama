// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.CompileTime.Serialization
{
    internal readonly struct AssemblyTypeName
    {
        public AssemblyTypeName( string typeName, string assemblyName )
        {
            this.TypeName = typeName;
            this.AssemblyName = assemblyName;
        }

        public readonly string TypeName;
        public readonly string AssemblyName;

        public override string ToString() => $"{this.TypeName}, {this.AssemblyName}";
    }
}