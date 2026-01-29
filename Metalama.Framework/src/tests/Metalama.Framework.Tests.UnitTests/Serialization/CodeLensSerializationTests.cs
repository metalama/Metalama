// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.CodeLens;
using System.Collections.Immutable;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Serialization;

public sealed class CodeLensSerializationTests : JsonSerializationTestsBase
{
    public CodeLensSerializationTests( ITestOutputHelper output ) : base( output ) { }

    [Fact]
    public void CodeLensSummary_Serialization()
    {
        var input = new CodeLensSummary( "3 aspects", "Click for details" );

        const string expectedJson = """
            {
              "Description": "3 aspects",
              "TooltipText": "Click for details"
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CodeLensSummary_NoTooltip_Serialization()
    {
        var input = new CodeLensSummary( "no aspect" );

        const string expectedJson = """
            {
              "Description": "no aspect",
              "TooltipText": null
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CodeLensSummary_NotAvailable_Serialization()
    {
        var input = CodeLensSummary.NotAvailable;

        const string expectedJson = """
            {
              "Description": "-",
              "TooltipText": null
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CodeLensSummary_NoAspect_Serialization()
    {
        var input = CodeLensSummary.NoAspect;

        const string expectedJson = """
            {
              "Description": "no aspect",
              "TooltipText": null
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CodeLensDetailsHeader_Serialization()
    {
        var input = new CodeLensDetailsHeader( "Aspect Name", "AspectName", isVisible: true, width: 150 );

        const string expectedJson = """
            {
              "DisplayName": "Aspect Name",
              "IsVisible": true,
              "UniqueName": "AspectName",
              "Width": 150.0
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CodeLensDetailsHeader_Hidden_Serialization()
    {
        var input = new CodeLensDetailsHeader( "Hidden Column", "HiddenColumn", isVisible: false, width: 0 );

        const string expectedJson = """
            {
              "DisplayName": "Hidden Column",
              "IsVisible": false,
              "UniqueName": "HiddenColumn",
              "Width": 0.0
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CodeLensDetailsField_Serialization()
    {
        var input = new CodeLensDetailsField( "MyAspect" );

        const string expectedJson = """
            {
              "Text": "MyAspect"
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CodeLensDetailsEntry_Serialization()
    {
        var fields = ImmutableArray.Create(
            new CodeLensDetailsField( "LoggingAspect" ),
            new CodeLensDetailsField( "Adds logging to the method" ) );

        var input = new CodeLensDetailsEntry( fields, "Click to navigate" );

        const string expectedJson = """
            {
              "Fields": [
                {
                  "Text": "LoggingAspect"
                },
                {
                  "Text": "Adds logging to the method"
                }
              ],
              "Tooltip": "Click to navigate"
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CodeLensDetailsEntry_NoTooltip_Serialization()
    {
        var fields = ImmutableArray.Create( new CodeLensDetailsField( "SimpleAspect" ) );

        var input = new CodeLensDetailsEntry( fields );

        const string expectedJson = """
            {
              "Fields": [
                {
                  "Text": "SimpleAspect"
                }
              ],
              "Tooltip": null
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CodeLensDetailsTable_Serialization()
    {
        var headers = ImmutableArray.Create(
            new CodeLensDetailsHeader( "Aspect", "Aspect", true, 100 ),
            new CodeLensDetailsHeader( "Description", "Description", true, 200 ) );

        var entries = ImmutableArray.Create(
            new CodeLensDetailsEntry(
                ImmutableArray.Create(
                    new CodeLensDetailsField( "CachingAspect" ),
                    new CodeLensDetailsField( "Caches the return value" ) ),
                "Caching tooltip" ) );

        var input = new CodeLensDetailsTable( headers, entries );

        const string expectedJson = """
            {
              "Headers": [
                {
                  "DisplayName": "Aspect",
                  "IsVisible": true,
                  "UniqueName": "Aspect",
                  "Width": 100.0
                },
                {
                  "DisplayName": "Description",
                  "IsVisible": true,
                  "UniqueName": "Description",
                  "Width": 200.0
                }
              ],
              "Entries": [
                {
                  "Fields": [
                    {
                      "Text": "CachingAspect"
                    },
                    {
                      "Text": "Caches the return value"
                    }
                  ],
                  "Tooltip": "Caching tooltip"
                }
              ]
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CodeLensDetailsTable_Empty_Serialization()
    {
        var input = CodeLensDetailsTable.Empty;

        const string expectedJson = """
            {
              "Headers": [],
              "Entries": []
            }
            """;

        this.TestSerialization( input, expectedJson );
    }

    [Fact]
    public void CodeLensDetailsTable_MultipleEntries_Serialization()
    {
        var headers = ImmutableArray.Create(
            new CodeLensDetailsHeader( "Name", "Name", true, 1 ) );

        var entries = ImmutableArray.Create(
            new CodeLensDetailsEntry( ImmutableArray.Create( new CodeLensDetailsField( "Aspect1" ) ) ),
            new CodeLensDetailsEntry( ImmutableArray.Create( new CodeLensDetailsField( "Aspect2" ) ) ),
            new CodeLensDetailsEntry( ImmutableArray.Create( new CodeLensDetailsField( "Aspect3" ) ) ) );

        var input = new CodeLensDetailsTable( headers, entries );

        const string expectedJson = """
            {
              "Headers": [
                {
                  "DisplayName": "Name",
                  "IsVisible": true,
                  "UniqueName": "Name",
                  "Width": 1.0
                }
              ],
              "Entries": [
                {
                  "Fields": [
                    {
                      "Text": "Aspect1"
                    }
                  ],
                  "Tooltip": null
                },
                {
                  "Fields": [
                    {
                      "Text": "Aspect2"
                    }
                  ],
                  "Tooltip": null
                },
                {
                  "Fields": [
                    {
                      "Text": "Aspect3"
                    }
                  ],
                  "Tooltip": null
                }
              ]
            }
            """;

        this.TestSerialization( input, expectedJson );
    }
}
