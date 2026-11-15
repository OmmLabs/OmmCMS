using FluentMigrator.Runner.Conventions;

namespace OmmCMS.Core.Data.Migrations;

public interface IConventionSource
{
    void Configure(IConventionSet conventionSet);
}