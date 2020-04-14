using System.Collections.Generic;
using Dapper;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KFrame.Sources
{
    /// <summary>
    /// Class DbSourceSqlServer.
    /// Implements the <see cref="KFrame.Sources.DbSourceAbstract" />
    /// </summary>
    /// <seealso cref="KFrame.Sources.DbSourceAbstract" />
    public class DbSourceSqlServer : DbSourceAbstract
    {
        /// <summary>
        /// Gets the k frame procedure.
        /// </summary>
        /// <value>The k frame procedure.</value>
        protected override string KFrameProcedure => $"[{Schema}].[p_Get{Prefix}KFrame]";

        /// <summary>
        /// Gets the i frame procedure.
        /// </summary>
        /// <value>The i frame procedure.</value>
        protected override string IFrameProcedure => $"[{Schema}].[p_Get{Prefix}IFrame]";

        /// <summary>
        /// database install as an asynchronous operation.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        protected override async Task DbInstallAsync(StringBuilder b, IDbConnection ctx, IReferenceDbSource[] sources)
        {
            b.AppendLine("-- TABLES");
            await ExecuteAsync(b, ctx, CreateTables());
            b.AppendLine("\n-- KFRAME");
            await ExecuteAsync(b, ctx, CreateKFrameSql(sources));
            b.AppendLine("\n-- IFRAME");
            await ExecuteAsync(b, ctx, CreateIFrameSql(sources));
        }

        /// <summary>
        /// database uninstall as an asynchronous operation.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        protected override async Task DbUninstallAsync(StringBuilder b, IDbConnection ctx, IReferenceDbSource[] sources)
        {
            b.AppendLine("-- TABLES");
            await ExecuteAsync(b, ctx, DropTables());
            b.AppendLine("\n-- KFRAME");
            await ExecuteAsync(b, ctx, DropKFrameSql());
            b.AppendLine("\n-- IFRAME");
            await ExecuteAsync(b, ctx, DropIFrameSql());
        }

        string CreateTables() =>
$@"
IF NOT EXISTS (SELECT TOP 1 Null FROM sys.schemas WHERE name = '{Schema}') BEGIN
    EXEC('CREATE SCHEMA [{Schema}] AUTHORIZATION [dbo];');
END;
GO
IF (OBJECT_ID('[{Schema}].[{Prefix}Frame]', 'U') IS NULL) BEGIN
CREATE TABLE [{Schema}].[{Prefix}Frame] (
    [Id] INT NOT NULL IDENTITY(1,1),
    [Frame] DATETIME NOT NULL,
    CONSTRAINT [PK_{Prefix}Frame] PRIMARY KEY ([Id])
);
END;
GO
IF (OBJECT_ID('[{Schema}].[{Prefix}FrameKey]', 'U') IS NULL) BEGIN
CREATE TABLE [{Schema}].[{Prefix}FrameKey] (
    [Id0] INT NOT NULL DEFAULT(-1),
    [Id1] NVARCHAR(50) NOT NULL DEFAULT(''),
    [Id2] UNIQUEIDENTIFIER NOT NULL DEFAULT('00000000-0000-0000-0000-000000000000'),
    [Param] CHAR(1) NOT NULL,
    [FrameId] INT NOT NULL,
    CONSTRAINT [PK_{Prefix}FrameKey] PRIMARY KEY ([Id0], [Id1], [Id2], [Param], [FrameId]),
    CONSTRAINT [FK_{Prefix}FrameKey_FrameId] FOREIGN KEY ([FrameId]) REFERENCES [{Schema}].[{Prefix}Frame] ([Id]) ON DELETE CASCADE,
);
END;
GO";

         string DropTables() =>
$@"
IF (OBJECT_ID('[{Schema}].[{Prefix}Frame]', 'U') IS NOT NULL) BEGIN
DROP TABLE [{Schema}].[{Prefix}Frame];
END;
GO
IF (OBJECT_ID('[{Schema}].[{Prefix}FrameKey]', 'U') IS NOT NULL) BEGIN
DROP TABLE [{Schema}].[{Prefix}FrameKey];
END;
GO";

        string CreateKFrameSql(IEnumerable<IReferenceDbSource> sources)
        {
            var b = new StringBuilder();
            b.AppendLine($@"
IF (OBJECT_ID('[{Schema}].[p_Get{Prefix}KFrame]', 'P') IS NOT NULL) BEGIN
    DROP PROCEDURE [{Schema}].[p_Get{Prefix}KFrame];
END;
GO
CREATE PROCEDURE [{Schema}].[p_Get{Prefix}KFrame] AS
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
BEGIN TRANSACTION;
Declare @frame datetime = GETUTCDATE(); Set @frame = DATEADD(MS, -DATEPART(MS, @frame), @frame);
Delete [{Schema}].[{Prefix}Frame]
	Where Frame < @frame - 2;
Insert [{Schema}].[{Prefix}Frame] (Frame)
Values (@frame);
Declare @frameId int = SCOPE_IDENTITY();

-- FRAME
Select @frame as Frame, @frameId as FrameId;");

            foreach (var source in sources)
                b.AppendLine($@"
-- SOURCE: {source.Param.name.ToUpperInvariant()}
Insert [{Schema}].[{Prefix}FrameKey] ({source.Table.Id}, Param, FrameId)
{source.Build.key($"{source.Table.Key}, '{source.Param.key}', @frameId")}
{source.Build.body(null)}");
            b.AppendLine(@"
COMMIT TRANSACTION;");
            return b.ToString();
        }

        string DropKFrameSql() =>
$@"
IF (OBJECT_ID('[{Schema}].[p_Get{Prefix}KFrame]', 'P') IS NOT NULL) BEGIN
    DROP PROCEDURE [{Schema}].[p_Get{Prefix}KFrame];
END;
GO";

        string CreateIFrameSql(IEnumerable<IReferenceDbSource> sources)
        {
            var unionSql = string.Join(" Union All\n", sources.Select(y => $"Select Top 1 Null From {y.Table.Name} Where {y.Table.Key} = rk.{y.Table.Id} And rk.Param = '{y.Param.key}'").ToArray());
            var b = new StringBuilder();
            b.AppendLine($@"
IF (OBJECT_ID('[{Schema}].[p_Get{Prefix}IFrame]', 'P') IS NOT NULL) BEGIN
    DROP PROCEDURE [{Schema}].[p_Get{Prefix}IFrame];
END;
GO
CREATE PROCEDURE [{Schema}].[p_Get{Prefix}IFrame](@kframe datetime, @kframeL datetime, @expand bit = 0) AS
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
Declare @frame datetime = GETUTCDATE(); Set @frame = DATEADD(MS, -DATEPART(MS, @frame), @frame);
Declare @frameId int = (Select Top 1 Id From [{Schema}].[{Prefix}Frame] Where Frame = @kframe);

-- FRAME
Select @frame as Frame, @frameId as FrameId;
IF (@frameId Is Null) BEGIN
	Return;
END;

-- DELETE
Select Count(*)
From [{Schema}].[{Prefix}FrameKey] as rk
	Where rk.FrameId = @frameId And Not Exists(
{unionSql});
IF (@expand = 1) BEGIN
Select rk.Id0, rk.Id1, rk.Id2, rk.Param
From [{Schema}].[{Prefix}FrameKey] as rk
	Where rk.FrameId = @frameId And Not Exists(
{unionSql});
END;");
            var x = new Reference.X();
            foreach (var source in sources)
                b.AppendLine($@"
-- SOURCE: {source.Param.name.ToUpperInvariant()}
{source.Build.max(x)}
IF (@expand = 1) BEGIN
{source.Build.body(x)}
END;");
            return b.ToString();
        }

        string DropIFrameSql() =>
$@"
IF (OBJECT_ID('[{Schema}].[p_Get{Prefix}IFrame]', 'P') IS NOT NULL) BEGIN
    DROP PROCEDURE [{Schema}].[p_Get{Prefix}IFrame];
END;
GO";
    }
}
