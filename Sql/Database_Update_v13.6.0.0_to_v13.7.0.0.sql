UPDATE 	tiers
SET 	city=cities.name
FROM 	dbo.Tiers tiers, dbo.City cities
WHERE 	(tiers.city=cities.name)
GO

UPDATE  [TechnicalParameters]
SET     [value] = 'v13.7.0.0'
WHERE   [name] = 'VERSION'
GO
