CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

BEGIN TRANSACTION;

CREATE TABLE "Messages" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Messages" PRIMARY KEY AUTOINCREMENT,
    "Destination" TEXT NOT NULL,
    "Content" TEXT NOT NULL,
    "DateReceived" TEXT NOT NULL,
    "Sent" INTEGER NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20231012183937_InitialMigration', '8.0.0');

COMMIT;

BEGIN TRANSACTION;

CREATE TABLE "SharedData" (
    "Tag" TEXT NOT NULL CONSTRAINT "PK_SharedData" PRIMARY KEY,
    "Data" TEXT NOT NULL,
    "DateCreated" TEXT NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240620054308_AddSharedDataTable', '8.0.0');

COMMIT;

BEGIN TRANSACTION;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240620112903_ChangeDateTimeColumnsToDateTimeOffsets', '8.0.0');

COMMIT;

BEGIN TRANSACTION;

ALTER TABLE "SharedData" ADD "AccessCount" INTEGER NOT NULL DEFAULT 0;

ALTER TABLE "SharedData" ADD "MaxAccessCount" INTEGER NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240620132047_AddAccessCountForSharedData', '8.0.0');

COMMIT;

BEGIN TRANSACTION;

CREATE TABLE "AuthorizedServices" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_AuthorizedServices" PRIMARY KEY AUTOINCREMENT,
    "Address" TEXT NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240715123110_AddAuthorizedServicesTable', '8.0.0');

COMMIT;

