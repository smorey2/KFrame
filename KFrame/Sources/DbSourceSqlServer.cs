using System.Collections.Generic;
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
        /// Gets the i frame procedure.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <returns>System.String.</returns>
        /// <value>The i frame procedure.</value>
        protected override string GetIFrameProcedure(string chapter) => $"[{Schema}].[p_Get{FormatName(chapter, "IFrame")}]";

        /// <summary>
        /// Gets the p frame procedure.
        /// </summary>
        /// <param name="chapter">The chapter.</param>
        /// <returns>System.String.</returns>
        /// <value>The p frame procedure.</value>
        protected override string GetPFrameProcedure(string chapter) => $"[{Schema}].[p_Get{FormatName(chapter, "PFrame")}]";

        /// <summary>
        /// clear as an asynchronous operation.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        protected override async Task ClearAsync(StringBuilder b, IDbConnection ctx, string chapter, IEnumerable<IKFrameDbSource> sources)
        {
            b.AppendLine("-- TABLES");
            await ExecuteAsync(b, ctx, ClearTables(chapter));
        }

        /// <summary>
        /// install as an asynchronous operation.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        protected override async Task InstallAsync(StringBuilder b, IDbConnection ctx, string chapter, IEnumerable<IKFrameDbSource> sources)
        {
            b.AppendLine("-- TABLES");
            await ExecuteAsync(b, ctx, CreateTables(chapter));
            b.AppendLine("\n-- IFRAME");
            await ExecuteAsync(b, ctx, CreateIFrameSql(chapter, sources));
            b.AppendLine("\n-- PFRAME");
            await ExecuteAsync(b, ctx, CreatePFrameSql(chapter, sources));
        }

        /// <summary>
        /// uninstall as an asynchronous operation.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="ctx">The CTX.</param>
        /// <param name="chapter">The chapter.</param>
        /// <param name="sources">The sources.</param>
        /// <returns>Task.</returns>
        protected override async Task UninstallAsync(StringBuilder b, IDbConnection ctx, string chapter, IEnumerable<IKFrameDbSource> sources)
        {
            b.AppendLine("-- TABLES");
            await ExecuteAsync(b, ctx, DropTables(chapter));
            b.AppendLine("\n-- IFRAME");
            await ExecuteAsync(b, ctx, DropIFrameSql(chapter));
            b.AppendLine("\n-- PFRAME");
            await ExecuteAsync(b, ctx, DropPFrameSql(chapter));
        }

        string ClearTables(string chapter) =>
$@"IF (OBJECT_ID('[{Schema}].[{FormatName(chapter, "Frame")}]', 'U') IS NULL) BEGIN
DELETE [{Schema}].[{FormatName(chapter, "Frame")}];
END;
GO";

        string CreateTables(string chapter) =>
$@"IF NOT EXISTS (SELECT TOP 1 Null FROM sys.schemas WHERE name = '{Schema}') BEGIN
    EXEC('CREATE SCHEMA [{Schema}] AUTHORIZATION [dbo];');
END;
GO
IF (OBJECT_ID('[{Schema}].[{FormatName(chapter, "Frame")}]', 'U') IS NULL) BEGIN
CREATE TABLE [{Schema}].[{FormatName(chapter, "Frame")}] (
    [Id] INT NOT NULL IDENTITY(1,1),
    [Frame] DATETIME NOT NULL,
    CONSTRAINT [PK_{FormatName(chapter, "Frame")}] PRIMARY KEY ([Id])
);
END;
GO
IF (OBJECT_ID('[{Schema}].[{FormatName(chapter, "FrameKey")}]', 'U') IS NULL) BEGIN
CREATE TABLE [{Schema}].[{FormatName(chapter, "FrameKey")}] (
    {(UseVariant ? "[Id] SQL_VARIANT NOT NULL," : @"[Id0] INT NOT NULL DEFAULT(-1),
    [Id1] NVARCHAR(50) NOT NULL DEFAULT(''),
    [Id2] UNIQUEIDENTIFIER NOT NULL DEFAULT('00000000-0000-0000-0000-000000000000'),")}
    [Param] NCHAR(1) NOT NULL,
    [FrameId] INT NOT NULL,
    CONSTRAINT [PK_{FormatName(chapter, "FrameKey")}] PRIMARY KEY ({(UseVariant ? "[Id]" : "[Id0], [Id1], [Id2]")}, [Param], [FrameId]),
    CONSTRAINT [FK_{FormatName(chapter, "FrameKey")}_FrameId] FOREIGN KEY ([FrameId]) REFERENCES [{Schema}].[{FormatName(chapter, "Frame")}] ([Id]) ON DELETE CASCADE,
);
END;
GO";

        string DropTables(string chapter) =>
$@"IF (OBJECT_ID('[{Schema}].[{FormatName(chapter, "FrameKey")}]', 'U') IS NOT NULL) BEGIN
DROP TABLE [{Schema}].[{FormatName(chapter, "FrameKey")}];
END;
GO
IF (OBJECT_ID('[{Schema}].[{FormatName(chapter, "Frame")}]', 'U') IS NOT NULL) BEGIN
DROP TABLE [{Schema}].[{FormatName(chapter, "Frame")}];
END;
GO";

        string CreateIFrameSql(string chapter, IEnumerable<IKFrameDbSource> sources)
        {
            var b = new StringBuilder();
            b.AppendLine(
$@"IF (OBJECT_ID('[{Schema}].[p_Get{FormatName(chapter, "IFrame")}]', 'P') IS NOT NULL) BEGIN
    DROP PROCEDURE [{Schema}].[p_Get{FormatName(chapter, "IFrame")}];
END;
GO
CREATE PROCEDURE [{Schema}].[p_Get{FormatName(chapter, "IFrame")}] AS
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
BEGIN TRANSACTION;
Declare @frame datetime = GETUTCDATE(); Set @frame = DATEADD(MS, -DATEPART(MS, @frame), @frame);
Delete [{Schema}].[{FormatName(chapter, "Frame")}]
	Where Frame < @frame - {KFrameTiming.PFrameSourceExpireInDays()};
Insert [{Schema}].[{FormatName(chapter, "Frame")}] (Frame)
Values (@frame);
Declare @frameId int = SCOPE_IDENTITY();

-- FRAME
Select @frame as Frame, @frameId as FrameId;");

            var x = new Source.X();
            foreach (var source in sources)
                b.AppendLine(
$@"-- SOURCE: {source.Param.name?.ToUpperInvariant()}
Insert [{Schema}].[{FormatName(chapter, "FrameKey")}] ({(UseVariant ? "Id" : source.Table.Id)}, Param, FrameId)
{source.Build.key($"[{source.Table.Key}], '{source.Param.key}', @frameId")}
{source.Build.body(null)}");
            b.AppendLine(@"
COMMIT TRANSACTION;
GO");
            return b.ToString();
        }

        string DropIFrameSql(string chapter) =>
$@"IF (OBJECT_ID('[{Schema}].[p_Get{FormatName(chapter, "IFrame")}]', 'P') IS NOT NULL) BEGIN
    DROP PROCEDURE [{Schema}].[p_Get{FormatName(chapter, "IFrame")}];
END;
GO";

        string CreatePFrameSql(string chapter, IEnumerable<IKFrameDbSource> sources)
        {
            var unionSql = string.Join(" Union All\n", sources.Select(y => $"Select Top 1 Null From {y.Table.Name} Where [{y.Table.Key}] = rk.{(UseVariant ? "Id" : y.Table.Id)} And rk.Param = '{y.Param.key}'").ToArray());
            var b = new StringBuilder();
            b.AppendLine(
$@"IF (OBJECT_ID('[{Schema}].[p_Get{FormatName(chapter, "PFrame")}]', 'P') IS NOT NULL) BEGIN
    DROP PROCEDURE [{Schema}].[p_Get{FormatName(chapter, "PFrame")}];
END;
GO
CREATE PROCEDURE [{Schema}].[p_Get{FormatName(chapter, "PFrame")}](@iframe datetime, @iframeL datetime, @expand bit = 0) AS
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
Declare @frame datetime = GETUTCDATE(); Set @frame = DATEADD(MS, -DATEPART(MS, @frame), @frame);
Declare @frameId int = (Select Top 1 Id From [{Schema}].[{FormatName(chapter, "Frame")}] Where Frame = @iframe);

-- FRAME
Select @frame as Frame, @frameId as FrameId;
IF (@frameId Is Null) BEGIN
	Return;
END;

-- DELETE
Select Count(*)
From [{Schema}].[{FormatName(chapter, "FrameKey")}] as rk
	Where rk.FrameId = @frameId And Not Exists(
{unionSql});
IF (@expand = 1) BEGIN
Select {(UseVariant ? "rk.Id" : "rk.Id0, rk.Id1, rk.Id2")}, rk.Param
From [{Schema}].[{FormatName(chapter, "FrameKey")}] as rk
	Where rk.FrameId = @frameId And Not Exists(
{unionSql});
END;");
            var x = new Source.X();
            foreach (var source in sources)
                b.AppendLine($@"
-- SOURCE: {source.Param.name.ToUpperInvariant()}
{source.Build.max(x)}
IF (@expand = 1) BEGIN
{source.Build.body(x)}
END;");
            b.AppendLine("GO");
            return b.ToString();
        }

        string DropPFrameSql(string chapter) =>
$@"IF (OBJECT_ID('[{Schema}].[p_Get{FormatName(chapter, "PFrame")}]', 'P') IS NOT NULL) BEGIN
    DROP PROCEDURE [{Schema}].[p_Get{FormatName(chapter, "PFrame")}];
END;
GO";
    }
}
