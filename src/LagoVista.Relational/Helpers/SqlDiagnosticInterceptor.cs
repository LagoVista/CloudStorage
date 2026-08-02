using LagoVista.Core.PlatformSupport;
using LagoVista.Relational.DataContexts;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LagoVista.Relational.Helpers
{
    public sealed class SqlDiagnosticInterceptor : DbCommandInterceptor
    {
        private readonly ILogger _logger;

        public SqlDiagnosticInterceptor(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            WriteSql(command, eventData);
            return result;
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            WriteSql(command, eventData);
            return result;
        }

        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
        {
            WriteSql(command, eventData);
            return result;
        }

        private void WriteSql(DbCommand command, CommandEventData eventData)
        {
            if (eventData.Context is not IRelationalDiagnosticContext diagnosticContext ||
                !diagnosticContext.SqlDiagnosticsEnabled)
            {
                return;
            }

            _logger.AddCustomEvent(
                LogLevel.Message,
                nameof(SqlDiagnosticInterceptor),
                command.CommandText);
        }
    }
}
