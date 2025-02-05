// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.SerializableIds;

public static partial class SerializableDeclarationIdProvider
{
    private const string _assemblyPrefix = "Assembly:";

    private static readonly char[] _separators = [';', '='];
}