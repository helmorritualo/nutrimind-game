namespace NutriMind.App.Routing
{
    /// <summary>
    /// Typed origin for secondary routes whose chrome depends on how they were opened
    /// (e.g. Certificates from Rewards vs More).
    /// </summary>
    public enum AppRouteOrigin
    {
        None = 0,
        Rewards = 1,
        More = 2,
        Progress = 3
    }
}
