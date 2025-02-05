// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Xml;

namespace Metalama.Backstage.Telemetry;

/// <exclude />
public class DefaultExceptionAdapter : IExceptionAdapter
{
    public static DefaultExceptionAdapter Instance { get; } = new DefaultExceptionAdapter();
        
    private DefaultExceptionAdapter() { }
        
    public string? GetTypeFullName( Exception e ) => e.GetType().FullName;

    public string? GetStackTrace( Exception e ) => e.StackTrace;

    public void WriteException( XmlWriter writer, Exception e ) => ExceptionXmlFormatter.WriteException( writer, e );
}