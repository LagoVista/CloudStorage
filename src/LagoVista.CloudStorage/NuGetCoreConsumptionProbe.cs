namespace LagoVista.CloudStorage
{
    /// <summary>
    /// F219 compile-time proof of the newly published Core Workspace NuGet
    /// package. The referenced public type exists only in the producer's
    /// new 7.0.36-wsp generation, not in the previously stable Core package.
    /// This file exists only on disposable acceptance Workspace source.
    /// </summary>
    internal static class NuGetCoreConsumptionProbe
    {
        internal const string RestoredCoreMarker =
            LagoVista.Core.NuGetRestoreAcceptanceMarker.Revision;
    }
}
