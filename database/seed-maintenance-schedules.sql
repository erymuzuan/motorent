-- Create maintenance schedules for motorbikes
-- Bike 1: Oil Change overdue (past due date), others OK
INSERT INTO [MotoRent].[MaintenanceSchedule] ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
VALUES
-- Motorbike 1 - Oil Change OVERDUE
('{"MotorbikeId":1,"ServiceTypeId":1,"LastServiceDate":"2024-11-01T00:00:00+07:00","LastServiceMileage":10000,"NextDueDate":"2024-12-01T00:00:00+07:00","NextDueMileage":13000,"ServiceTypeName":"Oil Change","MotorbikeName":"Honda Click 125 (1234)"}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
-- Motorbike 1 - Brake Check OK
('{"MotorbikeId":1,"ServiceTypeId":2,"LastServiceDate":"2024-12-15T00:00:00+07:00","LastServiceMileage":11000,"NextDueDate":"2025-02-13T00:00:00+07:00","NextDueMileage":16000,"ServiceTypeName":"Brake Check","MotorbikeName":"Honda Click 125 (1234)"}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),

-- Motorbike 2 - Oil Change DUE SOON (within 7 days)
('{"MotorbikeId":2,"ServiceTypeId":1,"LastServiceDate":"2024-12-11T00:00:00+07:00","LastServiceMileage":8000,"NextDueDate":"2025-01-15T00:00:00+07:00","NextDueMileage":11000,"ServiceTypeName":"Oil Change","MotorbikeName":"Honda Click 125 (2345)"}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),

-- Motorbike 3 - All services OK
('{"MotorbikeId":3,"ServiceTypeId":1,"LastServiceDate":"2025-01-05T00:00:00+07:00","LastServiceMileage":5000,"NextDueDate":"2025-02-04T00:00:00+07:00","NextDueMileage":8000,"ServiceTypeName":"Oil Change","MotorbikeName":"Honda PCX 160 (3456)"}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
('{"MotorbikeId":3,"ServiceTypeId":2,"LastServiceDate":"2025-01-05T00:00:00+07:00","LastServiceMileage":5000,"NextDueDate":"2025-03-06T00:00:00+07:00","NextDueMileage":10000,"ServiceTypeName":"Brake Check","MotorbikeName":"Honda PCX 160 (3456)"}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),

-- Motorbike 4 - Tire Inspection OVERDUE
('{"MotorbikeId":4,"ServiceTypeId":3,"LastServiceDate":"2024-09-01T00:00:00+07:00","LastServiceMileage":15000,"NextDueDate":"2024-11-30T00:00:00+07:00","NextDueMileage":23000,"ServiceTypeName":"Tire Inspection","MotorbikeName":"Yamaha NMAX 155 (4567)"}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),

-- Motorbike 5 - General Service DUE SOON
('{"MotorbikeId":5,"ServiceTypeId":4,"LastServiceDate":"2024-07-15T00:00:00+07:00","LastServiceMileage":2000,"NextDueDate":"2025-01-11T00:00:00+07:00","NextDueMileage":17000,"ServiceTypeName":"General Service","MotorbikeName":"Yamaha Aerox 155 (5678)"}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Verify insertion with status overview
SELECT
    ms.MaintenanceScheduleId,
    ms.MotorbikeId,
    ms.ServiceTypeId,
    ms.NextDueDate,
    ms.NextDueMileage,
    CASE
        WHEN ms.NextDueDate < GETDATE() THEN 'OVERDUE'
        WHEN ms.NextDueDate < DATEADD(day, 7, GETDATE()) THEN 'DUE SOON'
        ELSE 'OK'
    END as Status
FROM [MotoRent].[MaintenanceSchedule] ms
ORDER BY ms.MotorbikeId, ms.ServiceTypeId;
