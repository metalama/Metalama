// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Code.Types
{
    /// <summary>
    /// Represent the <c>dynamic</c> type.
    /// </summary>
    public interface IDynamicType : IType
    {
        new IDynamicType ToNullable();

        new IDynamicType ToNonNullable();
    }
}