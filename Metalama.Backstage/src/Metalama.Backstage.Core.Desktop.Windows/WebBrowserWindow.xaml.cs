// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Application;
using Uri = System.Uri;

namespace Metalama.Backstage.Desktop.Windows;

/// <summary>
/// A window that hosts a web browser, titled after the product.
/// </summary>
public sealed partial class WebBrowserWindow
{
    public WebBrowserWindow( ProductProfile productProfile )
    {
        this.InitializeComponent();
        this.Title = $"{productProfile.Name} Configuration";
    }

    [UsedImplicitly]
    public Uri Url
    {
        get => this.webView.Source;
        set => this.webView.Source = value;
    }
}
