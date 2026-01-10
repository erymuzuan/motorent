-- =============================================
-- MotoRent Core Seed Data
-- SuperAdmins and First Tenant: Adam Motor Golok
-- =============================================

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- 1. Insert Organization (First Tenant)
-- =============================================
PRINT 'Creating Organization: Adam Motor Golok (AdamMotorGolok)'

IF NOT EXISTS (SELECT 1 FROM [Core].[Organization] WHERE [AccountNo] = 'AdamMotorGolok')
BEGIN
    INSERT INTO [Core].[Organization] ([Json])
    VALUES (N'{
        "$type": "Organization",
        "AccountNo": "AdamMotorGolok",
        "Name": "Adam Motor Golok",
        "Currency": "THB",
        "Timezone": 7,
        "Language": "th-TH",
        "FirstDay": 1,
        "Subscriptions": ["rental", "maintenance"],
        "IsActive": true,
        "Email": "contact@adam-motor.co.th",
        "Phone": "+66-73-123-456",
        "Address": {
            "Street": "123 Charoen Pradit Road",
            "City": "Sungai Golok",
            "Province": "Narathiwat",
            "PostalCode": "96120",
            "Country": "Thailand"
        }
    }')
    PRINT 'Organization created: Adam Motor Golok'
END
ELSE
BEGIN
    PRINT 'Organization already exists: Adam Motor Golok'
END
GO

-- =============================================
-- 2. Insert SuperAdmin Users (Platform Administrators)
-- NOTE: SuperAdmins have NO tenant accounts (empty AccountCollection)
-- They are identified via SUPER_ADMIN environment variable
-- Must impersonate tenant users to access tenant features
-- =============================================
PRINT 'Creating SuperAdmin Users'

-- SuperAdmin 1: erymuzuan@gmail.com
IF NOT EXISTS (SELECT 1 FROM [Core].[User] WHERE [UserName] = 'erymuzuan@gmail.com')
BEGIN
    INSERT INTO [Core].[User] ([Json])
    VALUES (N'{
        "$type": "User",
        "UserName": "erymuzuan@gmail.com",
        "Email": "erymuzuan@gmail.com",
        "FullName": "Erymuzuan Mustapa",
        "CredentialProvider": "Google",
        "IsLockedOut": false,
        "AccountCollection": []
    }')
    PRINT 'SuperAdmin created: erymuzuan@gmail.com'
END
ELSE
BEGIN
    PRINT 'SuperAdmin already exists: erymuzuan@gmail.com'
END
GO

-- SuperAdmin 2: alwee.hay@gmail.com
IF NOT EXISTS (SELECT 1 FROM [Core].[User] WHERE [UserName] = 'alwee.hay@gmail.com')
BEGIN
    INSERT INTO [Core].[User] ([Json])
    VALUES (N'{
        "$type": "User",
        "UserName": "alwee.hay@gmail.com",
        "Email": "alwee.hay@gmail.com",
        "FullName": "Alwee Hay",
        "CredentialProvider": "Google",
        "IsLockedOut": false,
        "AccountCollection": []
    }')
    PRINT 'SuperAdmin created: alwee.hay@gmail.com'
END
ELSE
BEGIN
    PRINT 'SuperAdmin already exists: alwee.hay@gmail.com'
END
GO

-- SuperAdmin 3: c.muhammadnurdin@gmail.com
IF NOT EXISTS (SELECT 1 FROM [Core].[User] WHERE [UserName] = 'c.muhammadnurdin@gmail.com')
BEGIN
    INSERT INTO [Core].[User] ([Json])
    VALUES (N'{
        "$type": "User",
        "UserName": "c.muhammadnurdin@gmail.com",
        "Email": "c.muhammadnurdin@gmail.com",
        "FullName": "Muhammad Nurdin",
        "CredentialProvider": "Google",
        "IsLockedOut": false,
        "AccountCollection": []
    }')
    PRINT 'SuperAdmin created: c.muhammadnurdin@gmail.com'
END
ELSE
BEGIN
    PRINT 'SuperAdmin already exists: c.muhammadnurdin@gmail.com'
END
GO

-- =============================================
-- 3. Insert Tenant Users (AdamMotorGolok Organization)
-- =============================================
PRINT 'Creating Tenant Users for AdamMotorGolok'

-- OrgAdmin (Owner & Manager): sudarat-daoh.golok@gmail.com (Google OAuth)
-- Full control: organization settings, user management, all features
IF NOT EXISTS (SELECT 1 FROM [Core].[User] WHERE [UserName] = 'sudarat-daoh.golok@gmail.com')
BEGIN
    INSERT INTO [Core].[User] ([Json])
    VALUES (N'{
        "$type": "User",
        "UserName": "sudarat-daoh.golok@gmail.com",
        "Email": "sudarat-daoh.golok@gmail.com",
        "FullName": "Sudarat Daoh",
        "CredentialProvider": "Google",
        "IsLockedOut": false,
        "AccountCollection": [
            {
                "AccountNo": "AdamMotorGolok",
                "StartPage": "/",
                "IsFavourite": true,
                "Roles": ["OrgAdmin"]
            }
        ]
    }')
    PRINT 'Tenant user created: sudarat-daoh.golok@gmail.com (OrgAdmin)'
END
ELSE
BEGIN
    PRINT 'Tenant user already exists: sudarat-daoh.golok@gmail.com'
END
GO

-- ShopManager: kahn@adam.co.th (Microsoft OAuth)
-- Daily shop operations, rentals, inventory, reports
IF NOT EXISTS (SELECT 1 FROM [Core].[User] WHERE [UserName] = 'kahn@adam.co.th')
BEGIN
    INSERT INTO [Core].[User] ([Json])
    VALUES (N'{
        "$type": "User",
        "UserName": "kahn@adam.co.th",
        "Email": "kahn@adam.co.th",
        "FullName": "Kahn",
        "CredentialProvider": "Microsoft",
        "IsLockedOut": false,
        "AccountCollection": [
            {
                "AccountNo": "AdamMotorGolok",
                "StartPage": "/staff",
                "IsFavourite": true,
                "Roles": ["ShopManager"]
            }
        ]
    }')
    PRINT 'Tenant user created: kahn@adam.co.th (ShopManager)'
END
ELSE
BEGIN
    PRINT 'Tenant user already exists: kahn@adam.co.th'
END
GO

PRINT 'Core seed data created successfully'
GO
