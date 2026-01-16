using System;
using System.Collections.Generic;
using System.Linq;

namespace Gurung.BulkOperations.PostgreSql.QueryBuilder
{
    /// <summary>
    /// PostgreSQL-specific query builder for bulk operations.
    /// Generates optimized queries using COPY, INSERT ON CONFLICT, and temp table strategies.
    /// </summary>
    internal class PostgreQueryBuilder
    {
        /// <summary>
        /// Generates a COPY command for PostgreSQL's binary import.
        /// </summary>
        public static string CopyCommand(string tableName, IEnumerable<string> columns)
        {
            var columnList = string.Join(", ", columns.Select(c => QuoteIdentifier(c)));
            return $"COPY {tableName} ({columnList}) FROM STDIN (FORMAT BINARY);";
        }

        /// <summary>
        /// Generates an UPDATE query using temp table merge strategy.
        /// Only updates rows where temp table has non-null primary keys.
        /// </summary>
        public static string GenerateUpdateQuery(
            string targetTable, 
            string tempTable, 
            IEnumerable<string> allColumns, 
            List<string> primaryKeyColumns)
        {
            var columnsList = allColumns.ToList();
            var updateColumns = columnsList.Where(c => !primaryKeyColumns.Contains(c)).ToList();

            var joinConditions = string.Join(" AND ",
                primaryKeyColumns.Select(pk => $"target.{QuoteIdentifier(pk)} = temp.{QuoteIdentifier(pk)}"));

            var setClause = string.Join(", ",
                updateColumns.Select(col => $"{QuoteIdentifier(col)} = temp.{QuoteIdentifier(col)}"));

            var pkNotNullConditions = string.Join(" AND ",
                primaryKeyColumns.Select(pk => $"temp.{QuoteIdentifier(pk)} IS NOT NULL"));

            var query = $@"
UPDATE {targetTable} AS target
SET {setClause}
FROM {tempTable} AS temp
WHERE {joinConditions} AND {pkNotNullConditions};";

            return query;
        }

        /// <summary>
        /// Generates split merge queries using PostgreSQL's INSERT ... ON CONFLICT.
        /// Handles both UPSERT for existing records and INSERT for new records with null/default PKs.
        /// </summary>
        public static MergeQueryPair GenerateSplitMergeQueries(
            string targetTable, 
            string tempTable,
            IEnumerable<string> allColumns, 
            List<string> primaryKeyColumns, 
            bool hasIdentityColumn)
        {
            var columnsList = allColumns.ToList();
            var updateColumns = columnsList.Where(c => !primaryKeyColumns.Contains(c)).ToList();

            // === QUERY 1: UPSERT for records with existing PKs ===
            var columnList = string.Join(", ", columnsList.Select(c => QuoteIdentifier(c)));
            var insertValues = string.Join(", ", columnsList.Select(c => $"temp.{QuoteIdentifier(c)}"));
            var conflictColumns = string.Join(", ", primaryKeyColumns.Select(pk => QuoteIdentifier(pk)));
            var updateSetClause = string.Join(", ",
                updateColumns.Select(col => $"{QuoteIdentifier(col)} = EXCLUDED.{QuoteIdentifier(col)}"));
            var pkNotNullConditions = string.Join(" AND ",
                primaryKeyColumns.Select(pk => $"temp.{QuoteIdentifier(pk)} IS NOT NULL"));

            var pkNotDefaultConditions = GetPkNotDefaultConditions(primaryKeyColumns);

            var upsertQuery = $@"
INSERT INTO {targetTable} ({columnList})
SELECT {insertValues} FROM {tempTable} AS temp
WHERE {pkNotNullConditions} AND {pkNotDefaultConditions}
ON CONFLICT ({conflictColumns})
DO UPDATE SET {updateSetClause};";

            // === QUERY 2: INSERT for new records (null or default PKs) ===
            var insertColumns = hasIdentityColumn
                ? columnsList.Where(c => !primaryKeyColumns.Contains(c)).ToList()
                : columnsList;

            var insertColumnList = string.Join(", ", insertColumns.Select(c => QuoteIdentifier(c)));
            var insertSelectColumns = string.Join(", ", insertColumns.Select(c => $"temp.{QuoteIdentifier(c)}"));

            var pkIsNullOrDefault = GetPkNullOrDefaultConditions(primaryKeyColumns);

            var insertQuery = $@"
INSERT INTO {targetTable} ({insertColumnList})
SELECT {insertSelectColumns}
FROM {tempTable} AS temp
WHERE {pkIsNullOrDefault};";

            return new MergeQueryPair
            {
                UpsertQuery = upsertQuery,
                InsertNewQuery = insertQuery
            };
        }

        /// <summary>
        /// Helper to build conditions for PKs that are NOT default values.
        /// Handles int (!=0), long (!=0), Guid (!=empty), string (!='' and IS NOT NULL)
        /// </summary>
        private static string GetPkNotDefaultConditions(List<string> primaryKeys)
        {
            var conditions = new List<string>();
            foreach (var pk in primaryKeys)
            {
                // Use OR conditions to avoid CASE type conflicts in PostgreSQL
                // Check: NOT NULL AND (not a default value for any common type)
                conditions.Add($@"(
                    temp.{QuoteIdentifier(pk)} IS NOT NULL 
                    AND (
                        (pg_typeof(temp.{QuoteIdentifier(pk)})::text LIKE '%int%' AND temp.{QuoteIdentifier(pk)} != 0)
                        OR (pg_typeof(temp.{QuoteIdentifier(pk)})::text = 'uuid' AND temp.{QuoteIdentifier(pk)}::text != '00000000-0000-0000-0000-000000000000')
                        OR (pg_typeof(temp.{QuoteIdentifier(pk)})::text LIKE '%char%' AND temp.{QuoteIdentifier(pk)}::text != '')
                        OR (pg_typeof(temp.{QuoteIdentifier(pk)})::text NOT LIKE '%int%' AND pg_typeof(temp.{QuoteIdentifier(pk)})::text != 'uuid' AND pg_typeof(temp.{QuoteIdentifier(pk)})::text NOT LIKE '%char%')
                    )
                )");
            }
            return string.Join(" AND ", conditions);
        }

        /// <summary>
        /// Helper to build conditions for PKs that ARE null or default values.
        /// </summary>
        private static string GetPkNullOrDefaultConditions(List<string> primaryKeys)
        {
            var conditions = new List<string>();
            foreach (var pk in primaryKeys)
            {
                // Use OR conditions to avoid CASE type conflicts in PostgreSQL
                conditions.Add($@"(
                    temp.{QuoteIdentifier(pk)} IS NULL 
                    OR (pg_typeof(temp.{QuoteIdentifier(pk)})::text LIKE '%int%' AND temp.{QuoteIdentifier(pk)} = 0)
                    OR (pg_typeof(temp.{QuoteIdentifier(pk)})::text = 'uuid' AND temp.{QuoteIdentifier(pk)}::text = '00000000-0000-0000-0000-000000000000')
                    OR (pg_typeof(temp.{QuoteIdentifier(pk)})::text LIKE '%char%' AND temp.{QuoteIdentifier(pk)}::text = '')
                )");
            }
            return string.Join(" OR ", conditions);
        }

        /// <summary>
        /// Drops a temporary table if it exists
        /// </summary>
        public static string DropTempTableQuery(string tempTableName)
        {
            return $"DROP TABLE IF EXISTS {tempTableName};";
        }

        /// <summary>
        /// Creates a temporary table from the source table structure
        /// </summary>
        public static string CreateTempTableQuery(string sourceTable, string tempTableName, bool useTempKeyword = true)
        {
            string tempKeyword = useTempKeyword ? "TEMP " : "";
            return $"CREATE {tempKeyword}TABLE {tempTableName} AS TABLE {sourceTable} WITH NO DATA;";
        }

        private static string QuoteIdentifier(string name)
        {
            return $"\"{name}\"";
        }
    }

    /// <summary>
    /// Represents a pair of queries for split merge operations
    /// </summary>
    public class MergeQueryPair
    {
        /// <summary>
        /// Query to UPSERT records that have existing PKs (update if exists, insert if not)
        /// </summary>
        public string UpsertQuery { get; set; }

        /// <summary>
        /// Query to INSERT new records that have NULL or default PKs
        /// </summary>
        public string InsertNewQuery { get; set; }
    }
}
