namespace NeoWatch.Infrastructure;

/// <summary>Bound from the "NeoWs" section of configuration (appsettings + user-secrets).</summary>
public sealed class NeoWsOptions
{
    public const string SectionName = "NeoWs";

    public string BaseUrl { get; set; } = "https://api.nasa.gov/";

    public string ApiKey { get; set; } = "DEMO_KEY";

    public double PollIntervalHours { get; set; } = 12;
    public int BackfillDays { get; set; } = 90;
    public int LookaheadDays { get; set; } = 7;

    /// <summary>Run the pipeline and export once, then stop the host. For scheduled CI jobs.</summary>
    public bool RunOnce { get; set; }

    /// <summary>Where to write the dashboard JSON. Empty skips the export.</summary>
    public string ExportPath { get; set; } = "";
}
