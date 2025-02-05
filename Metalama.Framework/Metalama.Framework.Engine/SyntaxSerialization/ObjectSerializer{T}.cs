// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    internal abstract class ObjectSerializer<T> : ObjectSerializer<T, T>
    {
        protected ObjectSerializer( SyntaxSerializationService service ) : base( service ) { }
    }
}