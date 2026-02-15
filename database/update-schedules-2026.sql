-- Update some schedules to be OK and DueSoon for testing (current date: Jan 10, 2026)
-- Motorbike 1, ServiceType 2 (Brake Check): OK (Feb 13, 2026)
UPDATE "MaintenanceSchedule"
SET "Json" = '{"MotorbikeId":1,"ServiceTypeId":2,"LastServiceDate":"2025-12-15T00:00:00+07:00","LastServiceMileage":11000,"NextDueDate":"2026-02-13T00:00:00+07:00","NextDueMileage":16000,"ServiceTypeName":"Brake Check","MotorbikeName":"Honda Click 125 (1234)"}'::jsonb
WHERE "MaintenanceScheduleId" = 2;

-- Motorbike 3, ServiceType 1 (Oil Change): OK (Feb 4, 2026)
UPDATE "MaintenanceSchedule"
SET "Json" = '{"MotorbikeId":3,"ServiceTypeId":1,"LastServiceDate":"2026-01-05T00:00:00+07:00","LastServiceMileage":5000,"NextDueDate":"2026-02-04T00:00:00+07:00","NextDueMileage":8000,"ServiceTypeName":"Oil Change","MotorbikeName":"Honda PCX 160 (3456)"}'::jsonb
WHERE "MaintenanceScheduleId" = 4;

-- Motorbike 3, ServiceType 2 (Brake Check): OK (Mar 6, 2026)
UPDATE "MaintenanceSchedule"
SET "Json" = '{"MotorbikeId":3,"ServiceTypeId":2,"LastServiceDate":"2026-01-05T00:00:00+07:00","LastServiceMileage":5000,"NextDueDate":"2026-03-06T00:00:00+07:00","NextDueMileage":10000,"ServiceTypeName":"Brake Check","MotorbikeName":"Honda PCX 160 (3456)"}'::jsonb
WHERE "MaintenanceScheduleId" = 5;

-- Motorbike 5, ServiceType 4 (General Service): DUE SOON (Jan 15, 2026 - within 7 days)
UPDATE "MaintenanceSchedule"
SET "Json" = '{"MotorbikeId":5,"ServiceTypeId":4,"LastServiceDate":"2025-12-15T00:00:00+07:00","LastServiceMileage":2000,"NextDueDate":"2026-01-15T00:00:00+07:00","NextDueMileage":17000,"ServiceTypeName":"General Service","MotorbikeName":"Yamaha Aerox 155 (5678)"}'::jsonb
WHERE "MaintenanceScheduleId" = 7;

-- Verify updated status
SELECT
    ms."MaintenanceScheduleId",
    ms."MotorbikeId",
    ms."ServiceTypeId",
    ms."NextDueDate",
    CASE
        WHEN ms."NextDueDate" < NOW() THEN 'OVERDUE'
        WHEN ms."NextDueDate" < NOW() + INTERVAL '7 days' THEN 'DUE SOON'
        ELSE 'OK'
    END as Status
FROM "MaintenanceSchedule" ms
ORDER BY ms."MotorbikeId", ms."ServiceTypeId";
