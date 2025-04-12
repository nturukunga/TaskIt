-- First, disable foreign key checks to make structural changes
SET session_replication_role = 'replica';

-- Rename lowercase tables to avoid conflicts
ALTER TABLE IF EXISTS tasks RENAME TO tasks_old;
ALTER TABLE IF EXISTS users RENAME TO users_old;

-- Fix data types in Tasks table
ALTER TABLE "Tasks" 
  ALTER COLUMN "AssignedToId" TYPE text USING "AssignedToId"::text,
  ALTER COLUMN "CreatedById" TYPE text USING "CreatedById"::text;

-- Clear invalid foreign keys
UPDATE "Tasks" 
SET "AssignedToId" = NULL 
WHERE "AssignedToId" IS NOT NULL AND 
      "AssignedToId" NOT IN (SELECT "Id" FROM "AspNetUsers");

-- Set valid CreatedById values
UPDATE "Tasks" 
SET "CreatedById" = (SELECT "Id" FROM "AspNetUsers" LIMIT 1)
WHERE "CreatedById" IS NULL OR 
      "CreatedById" NOT IN (SELECT "Id" FROM "AspNetUsers");

-- Recreate correct foreign key constraints
ALTER TABLE "Tasks" DROP CONSTRAINT IF EXISTS "FK_Tasks_AspNetUsers_AssignedToId";
ALTER TABLE "Tasks" ADD CONSTRAINT "FK_Tasks_AspNetUsers_AssignedToId" 
  FOREIGN KEY ("AssignedToId") REFERENCES "AspNetUsers"("Id") ON DELETE SET NULL;

ALTER TABLE "Tasks" DROP CONSTRAINT IF EXISTS "FK_Tasks_AspNetUsers_CreatedById";
ALTER TABLE "Tasks" ADD CONSTRAINT "FK_Tasks_AspNetUsers_CreatedById" 
  FOREIGN KEY ("CreatedById") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE;

-- Re-enable foreign key checks
SET session_replication_role = 'origin'; 