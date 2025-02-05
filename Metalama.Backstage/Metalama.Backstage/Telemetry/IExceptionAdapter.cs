// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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