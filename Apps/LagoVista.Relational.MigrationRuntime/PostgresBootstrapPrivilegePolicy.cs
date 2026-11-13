namespace LagoVista.Relational.MigrationRuntime;

/// <summary>
/// PostgreSQL CREATE DATABASE ... OWNER requires the creating role to be
/// permitted to become the specified owner. PostgreSQL 16+ distinguishes
/// SET ROLE authorization from plain membership; PostgreSQL 15 uses MEMBER.
/// </summary>
public static class PostgresBootstrapPrivilegePolicy
{
    public static string RequiredOwnerMembership(int serverMajor)
    {
        if (serverMajor < 15)
            throw new ArgumentOutOfRangeException(nameof(serverMajor),
                "PostgreSQL bootstrap readiness requires a supported server (15+).");
        return serverMajor >= 16 ? "SET" : "MEMBER";
    }
}
