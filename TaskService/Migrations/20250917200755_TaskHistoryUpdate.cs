using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskService.Migrations
{
    /// <inheritdoc />
    public partial class TaskHistoryUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Сначала преобразуем тип с использованием USING
            migrationBuilder.Sql(@"
        ALTER TABLE ""TaskHistories"" 
        ALTER COLUMN ""ChangedByUserId"" TYPE uuid 
        USING ""ChangedByUserId""::uuid
    ");

            // Затем устанавливаем NOT NULL и DEFAULT значение
            migrationBuilder.Sql(@"UPDATE ""TaskHistories"" SET ""ChangedByUserId"" = '00000000-0000-0000-0000-000000000000' WHERE ""ChangedByUserId"" IS NULL");

            migrationBuilder.Sql(@"ALTER TABLE ""TaskHistories"" ALTER COLUMN ""ChangedByUserId"" SET NOT NULL");

            migrationBuilder.Sql(@"ALTER TABLE ""TaskHistories"" ALTER COLUMN ""ChangedByUserId"" SET DEFAULT '00000000-0000-0000-0000-000000000000'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Убираем DEFAULT и NOT NULL сначала
            migrationBuilder.Sql(@"ALTER TABLE ""TaskHistories"" ALTER COLUMN ""ChangedByUserId"" DROP DEFAULT");

            migrationBuilder.Sql(@"ALTER TABLE ""TaskHistories"" ALTER COLUMN ""ChangedByUserId"" DROP NOT NULL");

            // Затем преобразуем обратно в text
            migrationBuilder.Sql(@"
        ALTER TABLE ""TaskHistories"" 
        ALTER COLUMN ""ChangedByUserId"" TYPE text 
        USING ""ChangedByUserId""::text
    ");
        }
    }
}
