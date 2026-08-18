using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Documents.DataStores.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class StoreDocumentContentInObjectStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ObjectKey",
                table: "DocumentPart",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "Size",
                table: "DocumentPart",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.Sql("UPDATE \"DocumentPart\" SET \"ObjectKey\" = \"DocumentId\"::text || '/' || \"Position\"::text, \"Size\" = octet_length(\"Content\")");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "DocumentPart");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ObjectKey",
                table: "DocumentPart");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "DocumentPart");

            migrationBuilder.AddColumn<byte[]>(
                name: "Content",
                table: "DocumentPart",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
