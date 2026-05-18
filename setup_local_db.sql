USE [master]
GO
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'db49724')
BEGIN
    CREATE DATABASE [db49724]
END
GO
USE [db49724]
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [dbo].[sp_fulltext_database] @action = 'enable'
end
GO
CREATE TABLE [dbo].[Users](
	[UserId] [int] IDENTITY(1,1) NOT NULL,
	[FullName] [nvarchar](100) NOT NULL,
	[Email] [nvarchar](100) NOT NULL,
	[PasswordHash] [nvarchar](256) NOT NULL,
	[Role] [nvarchar](50) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[LastLogin] [datetime] NULL,
	[PhoneNumber] [nvarchar](20) NULL,
PRIMARY KEY CLUSTERED ([UserId] ASC))
GO
CREATE TABLE [dbo].[Carriers](
	[CarrierId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[ContactPerson] [nvarchar](100) NULL,
	[ContactEmail] [nvarchar](100) NULL,
	[ContactPhone] [nvarchar](20) NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED ([CarrierId] ASC))
GO
CREATE TABLE [dbo].[Warehouses](
	[WarehouseId] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[Location] [nvarchar](200) NOT NULL,
	[TotalCapacity] [int] NOT NULL,
	[UsedCapacity] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED ([WarehouseId] ASC))
GO
CREATE TABLE [dbo].[Shipments](
	[ShipmentId] [int] IDENTITY(1,1) NOT NULL,
	[ShipmentCode] [nvarchar](30) NOT NULL,
	[CreatedByUserId] [int] NOT NULL,
	[CarrierId] [int] NOT NULL,
	[WarehouseId] [int] NOT NULL,
	[Origin] [nvarchar](100) NOT NULL,
	[Destination] [nvarchar](100) NOT NULL,
	[CargoType] [nvarchar](50) NOT NULL,
	[Weight] [decimal](10, 2) NOT NULL,
	[Volume] [decimal](10, 2) NULL,
	[Priority] [nvarchar](20) NOT NULL,
	[Status] [nvarchar](30) NOT NULL,
	[ScheduledDate] [datetime] NOT NULL,
	[SpecialHandling] [nvarchar](200) NULL,
	[Notes] [nvarchar](500) NULL,
	[CreatedAt] [datetime] NOT NULL,
	[ShippingFee] [decimal](18, 2) NOT NULL,
	[EstimatedCost] [decimal](18, 2) NOT NULL,
	[RouteId] [int] NULL,
PRIMARY KEY CLUSTERED ([ShipmentId] ASC))
GO
CREATE TABLE [dbo].[TrackingEvents](
	[EventId] [int] IDENTITY(1,1) NOT NULL,
	[ShipmentId] [int] NOT NULL,
	[DriverId] [int] NOT NULL,
	[EventType] [nvarchar](50) NOT NULL,
	[Location] [nvarchar](200) NULL,
	[Latitude] [decimal](9, 6) NULL,
	[Longitude] [decimal](9, 6) NULL,
	[Notes] [nvarchar](300) NULL,
	[EventTime] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED ([EventId] ASC))
GO
CREATE TABLE [dbo].[Drivers](
	[DriverId] [int] IDENTITY(1,1) NOT NULL,
	[FullName] [nvarchar](100) NOT NULL,
	[LicenseNumber] [nvarchar](50) NOT NULL,
	[VehiclePlate] [nvarchar](20) NOT NULL,
	[VehicleType] [nvarchar](50) NOT NULL,
	[Status] [nvarchar](30) NOT NULL,
	[AssignedWarehouseId] [int] NULL,
	[LicenseExpiry] [datetime2](7) NULL,
	[OnTimeDeliveryRate] [decimal](18, 2) NOT NULL,
	[SafetyScore] [int] NOT NULL,
	[UserId] [int] NULL,
PRIMARY KEY CLUSTERED ([DriverId] ASC))
GO
CREATE TABLE [dbo].[WarehouseZones](
	[ZoneId] [int] IDENTITY(1,1) NOT NULL,
	[WarehouseId] [int] NOT NULL,
	[ZoneName] [nvarchar](50) NOT NULL,
	[TotalSlots] [int] NOT NULL,
	[UsedSlots] [int] NOT NULL,
	[ZoneType] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED ([ZoneId] ASC))
GO
CREATE TABLE [dbo].[Routes](
	[RouteId] [int] IDENTITY(1,1) NOT NULL,
	[StartHub] [nvarchar](100) NOT NULL,
	[EndHub] [nvarchar](100) NOT NULL,
	[Waypoints] [nvarchar](500) NULL,
	[VehicleType] [nvarchar](50) NOT NULL,
	[LoadCapacity] [decimal](5, 2) NULL,
	[DistanceKm] [decimal](10, 2) NULL,
	[EstimatedMinutes] [int] NULL,
	[FuelCostEstimate] [decimal](10, 2) NULL,
	[TollCost] [decimal](10, 2) NULL,
	[OptimizationType] [nvarchar](20) NOT NULL,
	[Status] [nvarchar](20) NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[DriverId] [int] NULL,
	[RouteNumber] [nvarchar](30) NOT NULL,
PRIMARY KEY CLUSTERED ([RouteId] ASC))
GO
CREATE TABLE [dbo].[ActivityLogs](
	[LogId] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[Action] [nvarchar](200) NOT NULL,
	[TableAffected] [nvarchar](100) NULL,
	[IPAddress] [nvarchar](50) NULL,
	[Timestamp] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED ([LogId] ASC))
GO
CREATE TABLE [dbo].[DockSchedules](
	[DockScheduleId] [int] IDENTITY(1,1) NOT NULL,
	[WarehouseId] [int] NOT NULL,
	[ShipmentId] [int] NOT NULL,
	[DockNumber] [int] NOT NULL,
	[StartTime] [datetime] NOT NULL,
	[EndTime] [datetime] NOT NULL,
	[Status] [nvarchar](20) NOT NULL,
PRIMARY KEY CLUSTERED ([DockScheduleId] ASC))
GO
CREATE TABLE [dbo].[FreightRateCards](
	[RateCardId] [int] IDENTITY(1,1) NOT NULL,
	[CarrierId] [int] NOT NULL,
	[Zone] [nvarchar](100) NOT NULL,
	[WeightBracket] [nvarchar](50) NOT NULL,
	[BaseRatePerKg] [decimal](10, 2) NOT NULL,
	[FuelSurchargePercent] [decimal](5, 2) NOT NULL,
	[HandlingFee] [decimal](10, 2) NOT NULL,
	[EffectiveDate] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED ([RateCardId] ASC))
GO
CREATE TABLE [dbo].[Invoices](
	[InvoiceId] [int] IDENTITY(1,1) NOT NULL,
	[InvoiceCode] [nvarchar](30) NOT NULL,
	[CarrierId] [int] NOT NULL,
	[Amount] [decimal](12, 2) NOT NULL,
	[IssueDate] [datetime] NOT NULL,
	[DueDate] [datetime] NOT NULL,
	[Status] [nvarchar](20) NOT NULL,
	[PaidAt] [datetime] NULL,
PRIMARY KEY CLUSTERED ([InvoiceId] ASC))
GO
CREATE TABLE [dbo].[Payments](
	[PaymentId] [int] IDENTITY(1,1) NOT NULL,
	[ShipmentId] [int] NOT NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[PaymentDate] [datetime] NULL,
	[Status] [nvarchar](50) NULL,
	[ReferenceNumber] [nvarchar](100) NULL,
	[PaymentMethod] [nvarchar](50) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[Notes] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED ([PaymentId] ASC))
GO
CREATE TABLE [dbo].[Staff](
	[StaffId] [int] IDENTITY(1,1) NOT NULL,
	[FullName] [nvarchar](100) NOT NULL,
	[WarehouseId] [int] NOT NULL,
	[Zone] [nvarchar](50) NULL,
	[CurrentTask] [nvarchar](200) NULL,
	[IsOnShift] [bit] NOT NULL,
	[ShiftType] [nvarchar](20) NULL,
	[UserId] [int] NULL,
PRIMARY KEY CLUSTERED ([StaffId] ASC))
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
PRIMARY KEY CLUSTERED ([MigrationId] ASC))
GO
