using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.Commands.Rss;

public abstract class RssCommand : BaseCommand<BaseCommandSettings>
{
    // Reading the configuration needs no user interface, and a status command must never trigger a news fetch/toast as a
    // side effect of process startup.
    protected override BackstageInitializationOptions AddBackstageOptions( BackstageInitializationOptions options )
        => options with { AddRssClient = true };
}