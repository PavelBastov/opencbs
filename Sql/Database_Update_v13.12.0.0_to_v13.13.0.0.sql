 
 UPDATE dbo.ProvisioningRules
SET dbo.ProvisioningRules.provisioning_value =  2  

     

AlTER TABLE dbo.ProvisioningRules  ADD  
           provisioning_interest  FLOAT null  CONSTRAINT  rovisioningIcon  DEFAULT 0 WITH VALUES
		  ,provisioning_penalty   FLOAT null  CONSTRAINT  ProvisioningPcon  DEFAULT 0 WITH VALUES	
 

UPDATE  [TechnicalParameters]
SET     [value] = 'v13.13.0.0'
WHERE   [name] = 'VERSION'
GO
