using FluentMigrator;
using OmmCMS.Data.Migrations;

namespace OmmCMS.Core.Data.Migrations;

[MigrationVersion("2026-11-16 00:00:00", "Native CMS blog and taxonomy")]
internal sealed class BlogSchema : Migration
{
    public override void Up()
    {
        if (!Schema.Table("BlogPost").Exists())
        {
            Create.Table("BlogPost").WithIdColumn()
                .WithColumn("Title").AsString(250).NotNullable()
                .WithColumn("Slug").AsString(180).NotNullable().Unique()
                .WithColumn("Intro").AsString(1000).Nullable()
                .WithColumn("Body").AsMaxString().Nullable()
                .WithColumn("Author").AsString(150).Nullable()
                .WithColumn("PreviewImageId").AsInt32().Nullable()
                .WithColumn("CoverImageId").AsInt32().Nullable()
                .WithColumn("StoreId").AsInt32().NotNullable()
                .WithColumn("LanguageId").AsInt32().NotNullable()
                .WithColumn("IsPublished").AsBoolean().NotNullable()
                .WithColumn("PublishedOnUtc").AsDateTime2().NotNullable()
                .WithColumn("ExpiresOnUtc").AsDateTime2().Nullable()
                .WithColumn("CreatedOnUtc").AsDateTime2().NotNullable()
                .WithColumn("UpdatedOnUtc").AsDateTime2().NotNullable()
                .WithColumn("MetaTitle").AsString(250).Nullable()
                .WithColumn("MetaDescription").AsString(500).Nullable()
                .WithColumn("IncludeInSitemap").AsBoolean().NotNullable();
            Create.Index("IX_BlogPost_IsPublished_PublishedOnUtc").OnTable("BlogPost")
                .OnColumn("IsPublished").Ascending().OnColumn("PublishedOnUtc").Ascending();
        }
        if (!Schema.Table("BlogCategory").Exists())
        {
            Create.Table("BlogCategory").WithIdColumn()
                .WithColumn("Name").AsString(150).NotNullable()
                .WithColumn("Slug").AsString(180).NotNullable().Unique()
                .WithColumn("Description").AsString(500).Nullable()
                .WithColumn("DisplayOrder").AsInt32().NotNullable();
        }
        if (!Schema.Table("BlogTag").Exists())
        {
            Create.Table("BlogTag").WithIdColumn()
                .WithColumn("Name").AsString(150).NotNullable()
                .WithColumn("Slug").AsString(180).NotNullable().Unique()
                .WithColumn("Description").AsString(500).Nullable();
        }
        CreateMapping("BlogPostCategory", "CategoryId", "BlogCategory");
        CreateMapping("BlogPostTag", "TagId", "BlogTag");
    }

    private void CreateMapping(string table, string termId, string termTable)
    {
        if (Schema.Table(table).Exists())
            return;

        Create.Table(table)
            .WithColumn("PostId").AsInt32().NotNullable().PrimaryKey()
                .ForeignKey("BlogPost", "Id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn(termId).AsInt32().NotNullable().PrimaryKey()
                .ForeignKey(termTable, "Id").OnDelete(System.Data.Rule.Cascade);
        Create.Index("IX_" + table + "_" + termId).OnTable(table).OnColumn(termId).Ascending();
    }

    public override void Down()
        => throw new NotSupportedException("Blog data must not be removed by an automatic rollback.");
}
