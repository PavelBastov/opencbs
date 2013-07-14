UPDATE 	tiers
SET 	city=cities.name
FROM 	dbo.Tiers tiers, dbo.City cities
WHERE 	(tiers.city=cities.name)
GO

INSERT INTO [dbo].[EventTypes]([event_type],[description],[sort_order]) VALUES('MSCE','Manual Schedule Change Event',720)

UPDATE  [TechnicalParameters]
SET     [value] = 'v13.7.0.0'
WHERE   [name] = 'VERSION'
GO
