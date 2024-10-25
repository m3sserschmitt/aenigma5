#!/bin/bash

set -e

DB_NAME="aenigmaDb.sqlite"

rm -fv Migrations/migrate-db.sql
rm -fv "$DB_NAME"
dotnet ef migrations script -o Migrations/migrate-db.sql
sqlite3 "$DB_NAME" < Migrations/migrate-db.sql
