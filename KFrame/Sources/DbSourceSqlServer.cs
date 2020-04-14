using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace KFrame.Sources
{
    public class DbSourceSqlServer : DbSourceAbstract
    {
        public override void DbInstall(StringBuilder b, IDbConnection ctx, IReferenceDbSource[] sources)
        {
            b.AppendLine("-- TABLES");
            Execute(b, ctx, CreateTables(Schema, Prefix));
            b.AppendLine("\n-- KFRAME");
            Execute(b, ctx, CreateKFrameSql(Schema, Prefix, sources));
            b.AppendLine("\n-- IFRAME");
            Execute(b, ctx, CreateIFrameSql(Schema, Prefix, sources));
        }

        public override void DbUninstall(StringBuilder b, IDbConnection ctx, IReferenceDbSource[] sources)
        {
            b.AppendLine("-- TABLES");
            Execute(b, ctx, DropTables(Schema, Prefix));
            b.AppendLine("\n-- KFRAME");
            Execute(b, ctx, DropKFrameSql(Schema, Prefix));
            b.AppendLine("\n-- IFRAME");
            Execute(b, ctx, DropIFrameSql(Schema, Prefix));
        }

        static string CreateTables(string schema, string prefix)
        {
            return $@"
IF NOT EXISTS (SELECT TOP 1 Null FROM sys.schemas WHERE name = '{schema}') BEGIN
    EXEC('CREATE SCHEMA [{schema}] AUTHORIZATION [dbo];');
END;
GO
IF (OBJECT_ID('[{schema}].[{prefix}Frame]', 'U') IS NULL) BEGIN
CREATE TABLE [{schema}].[{prefix}Frame] (
    [Id] INT NOT NULL IDENTITY(1,1),
    [Frame] DATETIME NOT NULL,
    CONSTRAINT [PK_{prefix}Frame] PRIMARY KEY ([Id])
);
END;
GO
IF (OBJECT_ID('[{schema}].[{prefix}FrameKey]', 'U') IS NULL) BEGIN
CREATE TABLE [{schema}].[{prefix}FrameKey] (
    [Id0] INT NOT NULL DEFAULT(-1),
    [Id1] NVARCHAR(50) NOT NULL DEFAULT(''),
    [Id2] UNIQUEIDENTIFIER NOT NULL DEFAULT('00000000-0000-0000-0000-000000000000'),
    [Param] CHAR(1) NOT NULL,
    [FrameId] INT NOT NULL,
    CONSTRAINT [PK_{prefix}FrameKey] PRIMARY KEY ([Id0], [Id1], [Id2], [Param], [FrameId]),
    CONSTRAINT [FK_{prefix}FrameKey_FrameId] FOREIGN KEY ([FrameId]) REFERENCES [{schema}].[{prefix}Frame] ([Id]) ON DELETE CASCADE,
);
END;
GO";
        }

        static string DropTables(string schema, string prefix)
        {
            return $@"
IF (OBJECT_ID('[{schema}].[{prefix}Frame]', 'U') IS NOT NULL) BEGIN
DROP TABLE [{schema}].[{prefix}Frame];
END;
GO
IF (OBJECT_ID('[{schema}].[{prefix}FrameKey]', 'U') IS NOT NULL) BEGIN
DROP TABLE [{schema}].[{prefix}FrameKey];
END;
GO";
        }

        static string CreateKFrameSql(string schema, string prefix, IEnumerable<IReferenceDbSource> sources)
        {
            var b = new StringBuilder();
            b.AppendLine($@"
IF (OBJECT_ID('[{schema}].[p_Get{prefix}KFrame]', 'P') IS NOT NULL) BEGIN
    DROP PROCEDURE [{schema}].[p_Get{prefix}KFrame];
END;
GO
CREATE PROCEDURE [{schema}].[p_Get{prefix}KFrame] AS
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
BEGIN TRANSACTION;
Declare @frame datetime = GETUTCDATE(); Set @frame = DATEADD(MS, -DATEPART(MS, @frame), @frame);
Delete [{schema}].[{prefix}Frame]
	Where Frame < @frame - 2;
Insert [{schema}].[{prefix}Frame] (Frame)
Values (@frame);
Declare @frameId int = SCOPE_IDENTITY();

-- FRAME
Select @frame as Frame, @frameId as FrameId;");

            foreach (var source in sources)
                b.AppendLine($@"
-- SOURCE: {source.Name.ToUpperInvariant()}
Insert [{schema}].[{prefix}FrameKey] ({source.Table.Id}, Param, FrameId)
{source.SqlKey($"{source.Table.Key}, '{source.Param}', @frameId")}
{source.Sql(null)}");
            b.AppendLine(@"
COMMIT TRANSACTION;");
            return b.ToString();
        }

        static string DropKFrameSql(string schema, string prefix)
        {
            return $@"
IF (OBJECT_ID('[{schema}].[p_Get{prefix}KFrame]', 'P') IS NOT NULL) BEGIN
    DROP PROCEDURE [{schema}].[p_Get{prefix}KFrame];
END;
GO";
        }

        static string CreateIFrameSql(string schema, string prefix, IEnumerable<IReferenceDbSource> sources)
        {
            var unionSql = string.Join(" Union All\n", sources.Select(x => $"Select Top 1 Null From {x.Table.Name} Where {x.Table.Key} = rk.{x.Table.Id} And rk.Param = '{x.Param}'").ToArray());
            var b = new StringBuilder();
            b.AppendLine($@"
IF (OBJECT_ID('[{schema}].[p_Get{prefix}IFrame]', 'P') IS NOT NULL) BEGIN
    DROP PROCEDURE [{schema}].[p_Get{prefix}IFrame];
END;
GO
CREATE PROCEDURE [{schema}].[p_Get{prefix}IFrame](@kframe datetime, @kframeL datetime, @expand bit = 0) AS
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
Declare @frame datetime = GETUTCDATE(); Set @frame = DATEADD(MS, -DATEPART(MS, @frame), @frame);
Declare @frameId int = (Select Top 1 Id From [{schema}].[{prefix}Frame] Where Frame = @kframe);

-- FRAME
Select @frame as Frame, @frameId as FrameId;
IF (@frameId Is Null) BEGIN
	Return;
END;

-- DELETE
Select Count(*)
From [{schema}].[{prefix}FrameKey] as rk
	Where rk.FrameId = @frameId And Not Exists(
{unionSql});
IF (@expand = 1) BEGIN
Select rk.Id0, rk.Id1, rk.Id2, rk.Param
From [{schema}].[{prefix}FrameKey] as rk
	Where rk.FrameId = @frameId And Not Exists(
{unionSql});
END;");

            foreach (var source in sources)
                b.AppendLine($@"
-- SOURCE: {source.Name.ToUpperInvariant()}
{source.SqlMax("@kframe", "@kframeL")}
IF (@expand = 1) BEGIN
{source.Sql("@kframe", "@kframeL")}
END;");
            return b.ToString();
        }

        static string DropIFrameSql(string schema, string prefix)
        {
            return $@"
IF (OBJECT_ID('[{schema}].[p_Get{prefix}IFrame]', 'P') IS NOT NULL) BEGIN
    DROP PROCEDURE [{schema}].[p_Get{prefix}IFrame];
END;
GO";
        }
    }
}
