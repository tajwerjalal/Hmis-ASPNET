using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <summary>
    /// Snapshot-synchronisation migration.
    ///
    /// [BEDTRANSFER], [COMPANYTRANSFER], the transfer-time columns of
    /// [PATIENTENCOUNTER] and the ward / bed position of [PATIENTROOM] were
    /// introduced by 20261022120000_RegistrationTransfers, which is a guarded
    /// hand-written migration. Because that migration was not produced by the
    /// EF tooling, the model snapshot did not contain the new model, so later
    /// `dotnet ef migrations add` runs would have generated the same changes
    /// again.
    ///
    /// Running `dotnet ef migrations add` regenerated the snapshot against the
    /// current model and produced this migration. The physical objects already
    /// exist (created by 20261022120000_RegistrationTransfers), therefore the
    /// body is intentionally empty: it only records the snapshot sync in the
    /// migration history.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 23-Oct-2026
    /// Time      : 10:05
    /// Change Log: 23-Oct-2026 - created as an empty snapshot-sync migration.
    /// </summary>
    public partial class SyncRegistrationTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. The columns / tables are created by the
            // guarded migration 20261022120000_RegistrationTransfers.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. Removing the objects is owned by the
            // guarded migration 20261022120000_RegistrationTransfers.
        }
    }
}
