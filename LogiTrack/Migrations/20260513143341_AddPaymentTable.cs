using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogiTrack.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [Payments] (
                        [PaymentId] int NOT NULL IDENTITY,
                        [ShipmentId] int NOT NULL,
                        [Amount] decimal(18,2) NOT NULL,
                        [Status] nvarchar(50) NOT NULL,
                        [PaymentMethod] nvarchar(50) NOT NULL,
                        [ReferenceNumber] nvarchar(100) NULL,
                        [PaymentDate] datetime2 NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [Notes] nvarchar(500) NULL,
                        CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentId]),
                        CONSTRAINT [FK_Payments_Shipments_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [Shipments] ([ShipmentId]) ON DELETE CASCADE
                    );
                END
                ELSE
                BEGIN
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND name = 'PaymentMethod')
                        ALTER TABLE [Payments] ADD [PaymentMethod] nvarchar(50) NOT NULL DEFAULT 'Bank Transfer';
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND name = 'CreatedAt')
                        ALTER TABLE [Payments] ADD [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE();
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND name = 'Notes')
                        ALTER TABLE [Payments] ADD [Notes] nvarchar(500) NULL;
                END

                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Payments_ShipmentId' AND object_id = OBJECT_ID('Payments'))
                BEGIN
                    CREATE INDEX [IX_Payments_ShipmentId] ON [Payments] ([ShipmentId]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");
        }
    }
}
