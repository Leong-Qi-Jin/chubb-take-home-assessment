// Generates docs/generated/database-erd.md from the live SQL Server schema so the ERD always matches what the EF Core migrations actually created.
import { existsSync } from "node:fs";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import process from "node:process";
import sql from "mssql";

const connectionStringEnvironmentVariable = "ConnectionStrings__ClaimsDatabase";
const templatePath = new URL("database-erd.template.md", import.meta.url);
const diagramPlaceholder = "{{diagram}}";

if (process.argv.length > 3) {
  console.error(
    "Usage: npm --prefix tools/database-erd run generate -- [output-path]",
  );
  process.exitCode = 1;
} else {
  await generateErd(process.argv[2]);
}

async function generateErd(requestedOutputPath) {
  const connectionString = process.env[connectionStringEnvironmentVariable];
  if (!connectionString) {
    throw new Error(
      `Set ${connectionStringEnvironmentVariable} before generating the ERD. ` +
        "The repository .env file defines it for local development.",
    );
  }

  const repositoryRoot = findRepositoryRoot();
  const outputPath = path.resolve(
    requestedOutputPath ??
      path.join(repositoryRoot, "docs", "generated", "database-erd.md"),
  );
  const pool = await sql.connect(connectionString);

  try {
    const [tables, relationships] = await Promise.all([
      readTables(pool),
      readRelationships(pool),
    ]);
    const template = await readFile(templatePath, "utf8");
    if (!template.includes(diagramPlaceholder)) {
      throw new Error(`The ERD template must contain ${diagramPlaceholder}.`);
    }
    // A replacer function keeps "$" sequences in the diagram literal.
    const document = template.replace(diagramPlaceholder, () =>
      renderDiagram(tables, relationships),
    );

    await mkdir(path.dirname(outputPath), { recursive: true });
    await writeFile(outputPath, document, "utf8");
    console.log(`Generated ${outputPath}`);
  } finally {
    await pool.close();
  }
}

// Reads every user table column with its type and key markers. UK marks only columns that are unique on their own; composite unique indexes have no per-column Mermaid notation.
async function readTables(pool) {
  const result = await pool.request().query(`
    SELECT
        table_object.name AS TableName,
        column_object.column_id AS ColumnId,
        column_object.name AS ColumnName,
        type_object.name AS DataType,
        column_object.max_length AS MaxLength,
        column_object.precision AS [Precision],
        column_object.scale AS Scale,
        column_object.is_nullable AS IsNullable,
        CASE WHEN EXISTS
        (
            SELECT 1
            FROM sys.indexes AS primary_key
            INNER JOIN sys.index_columns AS primary_key_column
                ON primary_key_column.object_id = primary_key.object_id
                AND primary_key_column.index_id = primary_key.index_id
            WHERE primary_key.object_id = column_object.object_id
                AND primary_key.is_primary_key = 1
                AND primary_key_column.column_id = column_object.column_id
        ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsPrimaryKey,
        CASE WHEN EXISTS
        (
            SELECT 1
            FROM sys.foreign_key_columns AS foreign_key_column
            WHERE foreign_key_column.parent_object_id = column_object.object_id
                AND foreign_key_column.parent_column_id = column_object.column_id
        ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsForeignKey,
        CASE WHEN EXISTS
        (
            SELECT 1
            FROM sys.indexes AS unique_index
            INNER JOIN sys.index_columns AS unique_column
                ON unique_column.object_id = unique_index.object_id
                AND unique_column.index_id = unique_index.index_id
            WHERE unique_index.object_id = column_object.object_id
                AND unique_index.is_unique = 1
                AND unique_index.is_primary_key = 0
                AND unique_index.has_filter = 0
                AND unique_column.column_id = column_object.column_id
                AND unique_column.is_included_column = 0
                AND
                (
                    SELECT COUNT(*)
                    FROM sys.index_columns AS unique_index_column
                    WHERE unique_index_column.object_id = unique_index.object_id
                        AND unique_index_column.index_id = unique_index.index_id
                        AND unique_index_column.is_included_column = 0
                ) = 1
        ) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsSingleColumnUnique
    FROM sys.tables AS table_object
    INNER JOIN sys.columns AS column_object
        ON column_object.object_id = table_object.object_id
    INNER JOIN sys.types AS type_object
        ON type_object.user_type_id = column_object.user_type_id
    WHERE table_object.is_ms_shipped = 0
        AND table_object.name <> N'__EFMigrationsHistory'
    ORDER BY table_object.name, column_object.column_id;
  `);

  const tables = [];
  for (const row of result.recordset) {
    let table = tables.at(-1);
    if (table?.name !== row.TableName) {
      table = { name: row.TableName, columns: [] };
      tables.push(table);
    }

    table.columns.push({
      name: row.ColumnName,
      dataType: row.DataType,
      maxLength: row.MaxLength,
      precision: row.Precision,
      scale: row.Scale,
      isNullable: row.IsNullable,
      isPrimaryKey: row.IsPrimaryKey,
      isForeignKey: row.IsForeignKey,
      isSingleColumnUnique: row.IsSingleColumnUnique,
    });
  }

  return tables;
}

// Reads one row per foreign key, grouping composite keys so each becomes a single relationship line. The child side is optional when any foreign key column is nullable and at most one when an unfiltered unique index covers exactly the foreign key columns.
async function readRelationships(pool) {
  const result = await pool.request().query(`
    WITH ForeignKeyShape AS
    (
        SELECT
            foreign_key.object_id AS ForeignKeyId,
            foreign_key.name AS ForeignKeyName,
            parent_table.name AS ParentTableName,
            child_table.object_id AS ChildTableId,
            child_table.name AS ChildTableName,
            MAX(CONVERT(int, child_column.is_nullable)) AS AnyColumnNullable,
            COUNT(*) AS ForeignKeyColumnCount,
            STRING_AGG(CONVERT(nvarchar(max), child_column.name), N'_')
                WITHIN GROUP (ORDER BY foreign_key_column.constraint_column_id) AS ChildColumnNames
        FROM sys.foreign_keys AS foreign_key
        INNER JOIN sys.tables AS child_table
            ON child_table.object_id = foreign_key.parent_object_id
        INNER JOIN sys.tables AS parent_table
            ON parent_table.object_id = foreign_key.referenced_object_id
        INNER JOIN sys.foreign_key_columns AS foreign_key_column
            ON foreign_key_column.constraint_object_id = foreign_key.object_id
        INNER JOIN sys.columns AS child_column
            ON child_column.object_id = foreign_key_column.parent_object_id
            AND child_column.column_id = foreign_key_column.parent_column_id
        WHERE child_table.is_ms_shipped = 0
            AND parent_table.is_ms_shipped = 0
            AND child_table.name <> N'__EFMigrationsHistory'
            AND parent_table.name <> N'__EFMigrationsHistory'
        GROUP BY
            foreign_key.object_id,
            foreign_key.name,
            parent_table.name,
            child_table.object_id,
            child_table.name
    )
    SELECT
        shape.ParentTableName,
        shape.ChildTableName,
        shape.ChildColumnNames,
        CONVERT(bit, shape.AnyColumnNullable) AS IsOptionalOnChild,
        CONVERT(bit, CASE WHEN EXISTS
        (
            SELECT 1
            FROM sys.indexes AS unique_index
            WHERE unique_index.object_id = shape.ChildTableId
                AND unique_index.is_unique = 1
                AND unique_index.is_disabled = 0
                AND unique_index.has_filter = 0
                AND
                (
                    SELECT COUNT(*)
                    FROM sys.index_columns AS index_column
                    WHERE index_column.object_id = unique_index.object_id
                        AND index_column.index_id = unique_index.index_id
                        AND index_column.is_included_column = 0
                ) = shape.ForeignKeyColumnCount
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM sys.index_columns AS index_column
                    WHERE index_column.object_id = unique_index.object_id
                        AND index_column.index_id = unique_index.index_id
                        AND index_column.is_included_column = 0
                        AND NOT EXISTS
                        (
                            SELECT 1
                            FROM sys.foreign_key_columns AS foreign_key_column
                            WHERE foreign_key_column.constraint_object_id = shape.ForeignKeyId
                                AND foreign_key_column.parent_column_id = index_column.column_id
                        )
                )
        ) THEN 1 ELSE 0 END) AS IsUniqueOnChild
    FROM ForeignKeyShape AS shape
    ORDER BY shape.ParentTableName, shape.ChildTableName, shape.ForeignKeyName;
  `);

  return result.recordset.map((row) => ({
    parentTable: row.ParentTableName,
    childTable: row.ChildTableName,
    childColumnNames: row.ChildColumnNames,
    isOptionalOnChild: row.IsOptionalOnChild,
    isUniqueOnChild: row.IsUniqueOnChild,
  }));
}

function renderDiagram(tables, relationships) {
  const lines = ["erDiagram"];

  for (const relationship of relationships) {
    // Mermaid crow's foot notation: the parent end is "exactly one" unless the foreign key is nullable; the child end is "many" unless it is unique.
    const parentCardinality = relationship.isOptionalOnChild ? "o|" : "||";
    const childCardinality = relationship.isUniqueOnChild ? "o|" : "o{";
    lines.push(
      `    ${relationship.parentTable} ${parentCardinality}--${childCardinality} ` +
        `${relationship.childTable} : ${relationship.childColumnNames}`,
    );
  }

  for (const table of tables) {
    lines.push("", `    ${table.name} {`);
    for (const column of table.columns) {
      const nullable = column.isNullable ? ' "nullable"' : "";
      lines.push(
        `        ${formatDataType(column)} ${column.name}${formatKeys(column)}${nullable}`,
      );
    }
    lines.push("    }");
  }

  return lines.join("\n");
}

// Mermaid attribute types cannot contain parentheses or commas, so sizes are joined with underscores, for example nvarchar_200 or decimal_18_2.
function formatDataType(column) {
  switch (column.dataType) {
    // max_length is in bytes, and Unicode types store two bytes per character.
    case "nvarchar":
    case "nchar":
      return `${column.dataType}_${formatLength(column.maxLength / 2)}`;
    case "varchar":
    case "char":
    case "varbinary":
    case "binary":
      return `${column.dataType}_${formatLength(column.maxLength)}`;
    case "decimal":
    case "numeric":
      return `${column.dataType}_${column.precision}_${column.scale}`;
    // sys.types still reports rowversion under its deprecated timestamp name.
    case "timestamp":
      return "rowversion";
    default:
      return column.dataType;
  }
}

// SQL Server reports (max) columns as -1.
function formatLength(maxLength) {
  return maxLength === -1 ? "max" : maxLength;
}

function formatKeys(column) {
  if (column.isPrimaryKey && column.isForeignKey) return " PK, FK";
  if (column.isPrimaryKey) return " PK";
  if (column.isForeignKey) return " FK";
  return column.isSingleColumnUnique ? " UK" : "";
}

// Locates the repository by its solution file so the default output path works from any directory within the repository.
function findRepositoryRoot() {
  let directory = process.cwd();
  while (!existsSync(path.join(directory, "Claims.slnx"))) {
    const parent = path.dirname(directory);
    if (parent === directory) {
      throw new Error("Run the generator from within the Claims repository.");
    }
    directory = parent;
  }
  return directory;
}
