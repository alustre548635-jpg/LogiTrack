USE [db49724]
GO
SET IDENTITY_INSERT [dbo].[Users] ON 
GO
INSERT [dbo].[Users] ([UserId], [FullName], [Email], [PasswordHash], [Role], [IsActive], [CreatedAt], [LastLogin], [PhoneNumber]) VALUES (1, N'System Admin', N'yabdulrahman826@gmail.com', N'$2a$11$phxHmef89pWYEY0pQgjyz.CNzWxqjlgJMnZVeWu1Yp1rPYsDJVDMG', N'Admin', 1, CAST(N'2026-04-09T15:58:53.960' AS DateTime), CAST(N'2026-05-13T21:05:07.357' AS DateTime), NULL)
INSERT [dbo].[Users] ([UserId], [FullName], [Email], [PasswordHash], [Role], [IsActive], [CreatedAt], [LastLogin], [PhoneNumber]) VALUES (16, N'Ashlee Lustre', N'lustreashlee@gmail.com', N'$2a$11$j/I9covboC99j74LFi9DpupITBAgP5ek/EMvuHPCFIataH8aYcLvC', N'Manager', 1, CAST(N'2026-04-27T15:36:47.720' AS DateTime), CAST(N'2026-05-13T17:01:06.643' AS DateTime), N'09124728439')
SET IDENTITY_INSERT [dbo].[Users] OFF
GO
SET IDENTITY_INSERT [dbo].[Carriers] ON 
GO
INSERT [dbo].[Carriers] ([CarrierId], [Name], [ContactPerson], [ContactEmail], [ContactPhone], [IsActive]) VALUES (1, N'LBC Express', N'Rex Buenaventura', N'rex@lbc.com.ph', N'02-8585-5858', 1)
INSERT [dbo].[Carriers] ([CarrierId], [Name], [ContactPerson], [ContactEmail], [ContactPhone], [IsActive]) VALUES (2, N'J&T Express', N'Joy Tan', N'joy@jtexpress.ph', N'02-7798-7898', 1)
SET IDENTITY_INSERT [dbo].[Carriers] OFF
GO
SET IDENTITY_INSERT [dbo].[Warehouses] ON 
GO
INSERT [dbo].[Warehouses] ([WarehouseId], [Name], [Location], [TotalCapacity], [UsedCapacity], [IsActive]) VALUES (1, N'Manila Central Hub', N'Port Area, Manila', 5000, 3900, 1)
INSERT [dbo].[Warehouses] ([WarehouseId], [Name], [Location], [TotalCapacity], [UsedCapacity], [IsActive]) VALUES (3, N'Cebu City Hub', N'Mandaue City, Cebu', 2500, 1525, 1)
SET IDENTITY_INSERT [dbo].[Warehouses] OFF
GO
SET IDENTITY_INSERT [dbo].[Shipments] ON 
GO
INSERT [dbo].[Shipments] ([ShipmentId], [ShipmentCode], [CreatedByUserId], [CarrierId], [WarehouseId], [Origin], [Destination], [CargoType], [Weight], [Volume], [Priority], [Status], [ScheduledDate], [SpecialHandling], [Notes], [CreatedAt], [ShippingFee], [EstimatedCost], [RouteId]) VALUES (1, N'SHP-2024-0091', 2, 1, 1, N'Manila', N'Cebu City', N'Dry Goods', CAST(245.00 AS Decimal(10, 2)), NULL, N'Standard', N'In Transit', CAST(N'2024-12-10T00:00:00.000' AS DateTime), NULL, NULL, CAST(N'2026-04-09T15:58:54.067' AS DateTime), CAST(0.00 AS Decimal(18, 2)), CAST(0.00 AS Decimal(18, 2)), NULL)
SET IDENTITY_INSERT [dbo].[Shipments] OFF
GO
-- ... (rest of the seed data from your script)
