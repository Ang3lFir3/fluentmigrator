﻿using System;
using System.Data;
using System.Data.OleDb;
using FluentMigrator.Builders.Execute;

namespace FluentMigrator.Runner.Processors.Jet
{
	public class JetProcessor : ProcessorBase
	{
		public OleDbConnection Connection { get; set; }

		public JetProcessor(OleDbConnection connection, IMigrationGenerator generator, IAnnouncer announcer, IMigrationProcessorOptions options) : base(generator, announcer, options)
		{
			Connection = connection;
		}

		public override void Process(PerformDBOperationExpression expression)
		{
			if (Connection.State != ConnectionState.Open) Connection.Open();

			if (expression.Operation != null)
				expression.Operation(Connection, null);
		}

		protected override void Process(string sql)
		{
			Announcer.Sql(sql);

			if (Options.PreviewOnly || string.IsNullOrEmpty(sql))
				return;

			if (Connection.State != ConnectionState.Open)
				Connection.Open();

			using (var command = new OleDbCommand(sql, Connection))
			{
				try
				{
					command.ExecuteNonQuery();
				}
				catch (OleDbException ex)
				{
					throw new Exception(string.Format("Exception while processing \"{0}\"", sql), ex);
				}
			}
		}

		public override DataSet ReadTableData(string tableName)
		{
			return Read("SELECT * FROM [{0}]", tableName);
		}

		public override DataSet Read(string template, params object[] args)
		{
			if (Connection.State != ConnectionState.Open) Connection.Open();

			DataSet ds = new DataSet();
			using (var command = new OleDbCommand(String.Format(template, args), Connection))
			using (var adapter = new OleDbDataAdapter(command))
			{
				adapter.Fill(ds);
				return ds;
			}
		}

		public override bool Exists(string template, params object[] args)
		{
			throw new NotImplementedException();
		}

		public override void Execute(string template, params object[] args)
		{
			Process(String.Format(template, args));
		}

		public override bool SchemaExists(string tableName)
		{
			throw new NotImplementedException();
		}

		public override bool TableExists(string tableName)
		{
			if (Connection.State != ConnectionState.Open) Connection.Open();

			var restrict = new object[] { null, null, tableName, "TABLE" };
			using (var tables = Connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, restrict))
			{
				return tables.Rows.Count > 0;
			}
		}

		public override bool ColumnExists(string tableName, string columnName)
		{
			if (Connection.State != ConnectionState.Open) Connection.Open();

			var restrict = new[] { null, null, tableName, null };
			using (var columns = Connection.GetOleDbSchemaTable(OleDbSchemaGuid.Columns, restrict))
			{
				return columns.Rows.Count > 0;
			}
		}

		public override bool ConstraintExists(string tableName, string constraintName)
		{
			if (Connection.State != ConnectionState.Open) Connection.Open();

			var restrict = new[] { null, null, constraintName, null, null, tableName };
			using (var constraints = Connection.GetOleDbSchemaTable(OleDbSchemaGuid.Table_Constraints, restrict))
			{
				return constraints.Rows.Count > 0;
			}
		}
	}
}
