using System.Data;
using FluentMigrator;
using OmmCMS.Core.Content.Blogs;
using OmmCMS.Data.Migrations;

namespace OmmCMS.Core.Data.Migrations;

/// <summary>Preserve every existing published or draft blog URL during upgrade.</summary>
[MigrationVersion("2026-11-16 01:00:00", "Central URL records for blog content")]
internal sealed class BlogCentralUrlRecords : Migration
{
    public override void Up()
    {
        Execute.WithConnection((connection, transaction) =>
        {
            var provider = connection.GetType().Name;
            string Quote(string name) => provider.Contains("MySql", StringComparison.OrdinalIgnoreCase)
                ? "`" + name + "`"
                : provider.Equals("SqlConnection", StringComparison.Ordinal) ? "[" + name + "]" : "\"" + name + "\"";

            foreach (var entityName in new[] { nameof(BlogPost), nameof(BlogCategory), nameof(BlogTag) })
            {
                var existing = new List<(int Id, string Slug)>();
                using (var select = connection.CreateCommand())
                {
                    select.Transaction = transaction;
                    select.CommandText = $"SELECT {Quote("Id")}, {Quote("Slug")} FROM {Quote(entityName)}";
                    using var reader = select.ExecuteReader();
                    while (reader.Read())
                        existing.Add((reader.GetInt32(0), reader.GetString(1)));
                }

                foreach (var row in existing)
                {
                    var path = BlogSlugPaths.GetPath(entityName, row.Slug);
                    using (var check = connection.CreateCommand())
                    {
                        check.Transaction = transaction;
                        check.CommandText = $"SELECT {Quote("EntityId")}, {Quote("EntityName")}, {Quote("LanguageId")}, {Quote("IsActive")} FROM {Quote("UrlRecord")} WHERE {Quote("Slug")} = @path";
                        AddParameter(check, "@path", path);
                        using var reader = check.ExecuteReader();
                        if (reader.Read())
                        {
                            if (reader.GetInt32(0) != row.Id || reader.GetString(1) != entityName
                                || reader.GetInt32(2) != 0 || !Convert.ToBoolean(reader.GetValue(3)))
                                throw new InvalidOperationException($"Cannot migrate the existing blog URL '{path}': it is already assigned.");
                            continue;
                        }
                    }

                    using var insert = connection.CreateCommand();
                    insert.Transaction = transaction;
                    insert.CommandText = $"INSERT INTO {Quote("UrlRecord")} ({Quote("EntityId")}, {Quote("EntityName")}, {Quote("Slug")}, {Quote("LanguageId")}, {Quote("IsActive")}) VALUES (@id, @entity, @path, @language, @active)";
                    AddParameter(insert, "@id", row.Id);
                    AddParameter(insert, "@entity", entityName);
                    AddParameter(insert, "@path", path);
                    AddParameter(insert, "@language", 0);
                    AddParameter(insert, "@active", true);
                    insert.ExecuteNonQuery();
                }
            }
        });
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    public override void Down()
        => throw new NotSupportedException("Blog URL history must not be removed by rollback.");
}
