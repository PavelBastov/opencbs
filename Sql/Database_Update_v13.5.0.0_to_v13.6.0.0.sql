INSERT INTO dbo.MenuItems ([component_name]) VALUES ('mnuSettings')
DECLARE @menuId INT
SELECT @menuId = SCOPE_IDENTITY()
INSERT INTO dbo.AllowedRoleMenus ([menu_item_id],[role_id],[allowed]) VALUES (@menuId,1,'true')
INSERT INTO dbo.AllowedRoleMenus ([menu_item_id],[role_id],[allowed]) VALUES (@menuId,2,'true')
INSERT INTO dbo.AllowedRoleMenus ([menu_item_id],[role_id],[allowed]) VALUES (@menuId,3,'true')
INSERT INTO dbo.AllowedRoleMenus ([menu_item_id],[role_id],[allowed]) VALUES (@menuId,4,'true')
INSERT INTO dbo.AllowedRoleMenus ([menu_item_id],[role_id],[allowed]) VALUES (@menuId,5,'false')

INSERT INTO dbo.MenuItems ([component_name]) VALUES ('mnuSecurity')
SET @menuId=SCOPE_IDENTITY()
INSERT INTO dbo.AllowedRoleMenus ([menu_item_id],[role_id],[allowed]) VALUES (@menuId,1,'true')
INSERT INTO dbo.AllowedRoleMenus ([menu_item_id],[role_id],[allowed]) VALUES (@menuId,2,'true')
INSERT INTO dbo.AllowedRoleMenus ([menu_item_id],[role_id],[allowed]) VALUES (@menuId,3,'true')
INSERT INTO dbo.AllowedRoleMenus ([menu_item_id],[role_id],[allowed]) VALUES (@menuId,4,'true')
INSERT INTO dbo.AllowedRoleMenus ([menu_item_id],[role_id],[allowed]) VALUES (@menuId,5,'false')

INSERT INTO dbo.MenuItems ([component_name]) VALUES ('mnuProducts')
SET @menuId=SCOPE_IDENTITY()
INSERT INTO dbo.AllowedRoleMenus ([menu_item_id],[role_id],[allowed]) VALUES (@menuId,1,'true')
INSERT INTO dbo.AllowedRoleMenus ([menu_item_id],[role_id],[allowed]) VALUES (@menuId,2,'false')
INSERT INTO dbo.AllowedRoleMenus ([menu_item_id],[role_id],[allowed]) VALUES (@menuId,3,'true')
INSERT INTO dbo.AllowedRoleMenus ([menu_item_id],[role_id],[allowed]) VALUES (@menuId,4,'true')
INSERT INTO dbo.AllowedRoleMenus ([menu_item_id],[role_id],[allowed]) VALUES (@menuId,5,'false')
GO

UPDATE dbo.AllowedRoleMenus SET allowed=0 WHERE menu_item_id=3 AND role_id=2
GO

UPDATE  [TechnicalParameters]
SET     [value] = 'v13.6.0.0'
WHERE   [name] = 'VERSION'
GO
