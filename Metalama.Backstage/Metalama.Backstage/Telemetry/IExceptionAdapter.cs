// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Xml;

namespace Metalama.Backstage.Telemetry;

/// <exclude />
public interface IExceptionAdapter
{
    string? GetTypeFullName( Exception e );

    string? GetStackTrace( Exception e );

    void WriteException( XmlWriter writer, Exception e );
}