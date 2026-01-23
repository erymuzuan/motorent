-- =============================================
-- Seed Core data for KrabiBeachRentals tenant
-- =============================================

SET QUOTED_IDENTIFIER ON
GO

-- 1. Create Organization
IF NOT EXISTS (SELECT 1 FROM [Core].[Organization] WHERE [AccountNo] = 'KrabiBeachRentals')
BEGIN
    INSERT INTO [Core].[Organization] ([Json])
    VALUES (N'{
        "$type": "Organization",
        "AccountNo": "KrabiBeachRentals",
        "Name": "Krabi Beach Rentals",
        "Currency": "THB",
        "Timezone": 7,
        "Language": "en-US",
        "FirstDay": 1,
        "Subscriptions": ["rental", "maintenance", "cashier"],
        "IsActive": true,
        "Email": "info@krabibeachrentals.com",
        "Phone": "+66-75-123-4567",
        "Address": {
            "Street": "123 Beach Road",
            "City": "Ao Nang",
            "Province": "Krabi",
            "PostalCode": "81000",
            "Country": "Thailand"
        }
    }')
    PRINT 'Organization created: Krabi Beach Rentals'
END
ELSE
BEGIN
    PRINT 'Organization already exists: Krabi Beach Rentals'
END
GO

-- 2. Create OrgAdmin user
IF NOT EXISTS (SELECT 1 FROM [Core].[User] WHERE [UserName] = 'admin@krabirentals.com')
BEGIN
    INSERT INTO [Core].[User] ([Json])
    VALUES (N'{
        "$type": "User",
        "UserName": "admin@krabirentals.com",
        "Email": "admin@krabirentals.com",
        "FullName": "Krabi Admin",
        "CredentialProvider": "Google",
        "IsLockedOut": false,
        "AccountCollection": [
            {
                "AccountNo": "KrabiBeachRentals",
                "StartPage": "/",
                "IsFavourite": true,
                "Roles": ["OrgAdmin"]
            }
        ]
    }')
    PRINT 'User created: admin@krabirentals.com (OrgAdmin)'
END
ELSE
BEGIN
    PRINT 'User already exists: admin@krabirentals.com'
END
GO

-- 3. Create ShopManager user
IF NOT EXISTS (SELECT 1 FROM [Core].[User] WHERE [UserName] = 'manager@krabirentals.com')
BEGIN
    INSERT INTO [Core].[User] ([Json])
    VALUES (N'{
        "$type": "User",
        "UserName": "manager@krabirentals.com",
        "Email": "manager@krabirentals.com",
        "FullName": "Shop Manager",
        "CredentialProvider": "Google",
        "IsLockedOut": false,
        "AccountCollection": [
            {
                "AccountNo": "KrabiBeachRentals",
                "StartPage": "/staff",
                "IsFavourite": true,
                "Roles": ["ShopManager"]
            }
        ]
    }')
    PRINT 'User created: manager@krabirentals.com (ShopManager)'
END
ELSE
BEGIN
    PRINT 'User already exists: manager@krabirentals.com'
END
GO

-- 4. Create Staff user (for testing)
IF NOT EXISTS (SELECT 1 FROM [Core].[User] WHERE [UserName] = 'staff@krabirentals.com')
BEGIN
    INSERT INTO [Core].[User] ([Json])
    VALUES (N'{
        "$type": "User",
        "UserName": "staff@krabirentals.com",
        "Email": "staff@krabirentals.com",
        "FullName": "Staff User",
        "CredentialProvider": "Google",
        "IsLockedOut": false,
        "AccountCollection": [
            {
                "AccountNo": "KrabiBeachRentals",
                "StartPage": "/staff",
                "IsFavourite": true,
                "Roles": ["Staff"]
            }
        ]
    }')
    PRINT 'User created: staff@krabirentals.com (Staff)'
END
ELSE
BEGIN
    PRINT 'User already exists: staff@krabirentals.com'
END
GO

PRINT 'Core seed data for KrabiBeachRentals completed'
GO
