using System;
using System.Collections.Generic;
using System.Linq;

namespace Gurung.EfBulkOperations.PostgreSql.QueryBuilder
{
    /// <summary>
    /// PostgreSQL-specific query builder for bulk operations.
    /// Generates optimized queries using COPY, INSERT ON CONFLICT, and temp table strategies.
    /// </summary>
    internal class PostgreQueryBuilder
    {
        /// <summary>
        /// Generates a PostgreSQL COPY command for bulk importing data into a specified table with the given columns.
        /// </summary>
        /// <remarks>The generated command uses the STDIN source and binary format. This is typically used
        /// with PostgreSQL bulk import operations.</remarks>
        /// <param name="tableName">The name of the target table into which data will be imported. Cannot be null or empty.</param>
        /// <param name="columns">A collection of column names to include in the COPY command. Each column name will be quoted as an
        /// identifier. Cannot be null or empty.</param>
        /// <returns>A string containing the formatted PostgreSQL COPY command for the specified table and columns.</returns>
        public static string CopyCommand(string tableName, IEnumerable<string> columns)
        {
            var columnList = string.Join(", ", columns.Select(c => QuoteIdentifier(c)));
            return $"COPY {tableName} ({columnList}) FROM STDIN (FORMAT BINARY);";
        }

        /// <summary>
        /// Generates a SQL UPDATE statement that updates rows in a target table using values from a temporary table,
        /// matching on specified primary key columns.
        /// </summary>
        /// <remarks>The generated SQL statement uses an INNER JOIN on the specified primary key columns and only
        /// updates rows where all primary key values in the temporary table are not null. The method assumes that column
        /// and table names are properly quoted to prevent SQL injection or syntax errors.</remarks>
        /// <param name="targetTable">The name of the target table to be updated. Cannot be null or empty.</param>
        /// <param name="tempTable">The name of the temporary table containing updated values. Cannot be null or empty.</param>
        /// <param name="allColumns">A collection of all column names involved in the update operation, including both primary key and non-primary
        /// key columns. Cannot be null or empty.</param>
        /// <param name="primaryKeyColumns">A list of column names that make up the primary key and are used to match rows between the target and temporary
        /// tables. Cannot be null or empty.</param>
        /// <returns>A string containing the generated SQL UPDATE statement that sets non-primary key columns in the target table to
        /// values from the temporary table where primary key columns match and are not null.</returns>
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
        /// Generates SQL queries for merging data from a temporary table into a target table using split logic for upserts and
        /// inserts.
        /// </summary>
        /// <remarks>The generated queries support both upsert (update or insert on conflict) for rows with existing
        /// primary keys and insert for new rows where primary keys are null or default. This method is intended for use with
        /// PostgreSQL or similar SQL dialects that support ON CONFLICT clauses. The presence of an identity column affects
        /// which columns are included in the insert query.</remarks>
        /// <param name="targetTable">The name of the target table into which data will be merged.</param>
        /// <param name="tempTable">The name of the temporary table containing the source data to merge.</param>
        /// <param name="allColumns">A collection of all column names involved in the merge operation. The order determines the column mapping in the
        /// generated queries.</param>
        /// <param name="primaryKeyColumns">A list of column names that make up the primary key for the target table. Used to determine conflict resolution and
        /// row uniqueness.</param>
        /// <param name="hasIdentityColumn">true if the target table contains an identity column that should be excluded from certain insert operations;
        /// otherwise, false.</param>
        /// <returns>A MergeQueryPair containing the generated upsert and insert SQL queries for merging data from the temporary table
        /// into the target table.</returns>
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
        /// Builds a SQL condition that checks whether each specified primary key column is not null and does not have its
        /// default value.
        /// </summary>
        /// <remarks>This method generates conditions suitable for use in PostgreSQL queries, handling common data
        /// types such as integers, UUIDs, and character types. The resulting condition can be used to filter out rows where
        /// primary key columns are either null or set to their default values, which is useful for upsert or merge
        /// operations.</remarks>
        /// <param name="primaryKeys">A list of primary key column names for which to generate the non-default value conditions.</param>
        /// <returns>A SQL WHERE clause fragment that evaluates to true only if all specified primary key columns are not null and do
        /// not contain their default values.</returns>
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
        /// Generates a SQL statement to drop a temporary table if it exists.
        /// </summary>
        /// <param name="tempTableName">The name of the temporary table to be dropped. Must be a valid SQL table identifier.</param>
        /// <returns>A SQL command string that drops the specified temporary table if it exists.</returns>
        public static string DropTempTableQuery(string tempTableName)
        {
            return $"DROP TABLE IF EXISTS {tempTableName};";
        }

        /// <summary>
        /// Generates a SQL query string to create a temporary table based on the structure of an existing source table,
        /// without copying any data.
        /// </summary>
        /// <remarks>The generated query uses the 'WITH NO DATA' clause to ensure that only the table structure is
        /// copied, not the data. The TEMP keyword is included only if useTempKeyword is set to true. This method does not
        /// validate the existence or validity of the provided table names.</remarks>
        /// <param name="sourceTable">The name of the existing table whose structure will be used to create the temporary table. Cannot be null or
        /// empty.</param>
        /// <param name="tempTableName">The name to assign to the new temporary table. Cannot be null or empty.</param>
        /// <param name="useTempKeyword">true to include the TEMP keyword in the CREATE TABLE statement, creating a temporary table; otherwise, false to
        /// omit the keyword.</param>
        /// <returns>A SQL query string that creates a temporary table with the specified name and structure, but without any data.</returns>
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
    /// Represents a pair of SQL queries used for merging data, including queries for upserting existing records and
    /// inserting new records.
    /// </summary>
    /// <remarks>Use this class to encapsulate the SQL statements required for data merge operations where both
    /// upsert and insert-new logic are needed. This is commonly used in scenarios where records may either need to be
    /// updated if they exist or inserted if they are new.</remarks>
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
