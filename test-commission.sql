SET QUOTED_IDENTIFIER ON;
GO

-- Insert test commission for agent TG-001 (AgentId = 1)
INSERT INTO [KrabiBeachRentals].[AgentCommission]
    ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
VALUES
    (N'{"AgentId": 1, "Status": "Pending", "CommissionAmount": 100, "BookingTotal": 1000, "AgentCode": "TG-001", "AgentName": "Phuket Paradise Tours"}',
     'test', 'test', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

SELECT 'Commission created for Agent 1' AS Result;
