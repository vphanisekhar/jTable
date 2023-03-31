using Microsoft.Practices.EnterpriseLibrary.Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jTable.AdoNet.Data
{
    public class MasterTableDAL
    {
        public static string GetJsonFields(string tableName)
        {
            //TODO : We may need to discard this algorithm and put this configuration in to database field.
            //Table Structure
            //TableId, TableName, Configuration, IsAuditField, IsActive
            Database db;
            string sqlCommand;
            DbCommand dbCommand;
            DatabaseProviderFactory factory = new DatabaseProviderFactory();
            db = factory.Create("ExampleDatabase");
            //how to avoid injection ?
            sqlCommand = "select * from " + tableName;
            dbCommand = db.GetSqlStringCommand(sqlCommand);
            DataSet ds = db.ExecuteDataSet(dbCommand);
            string primaryKeyName = GetPrimaryKey(tableName);
            if (ds != null && ds.Tables.Count > 0)
            {
                //prepare structure for jtable

                string jsonString = "{";

                //prepare primary key; keeping it in a separate loop , not sure what can be the position
                foreach (DataColumn col in ds.Tables[0].Columns)
                {
                    if (col.ColumnName == primaryKeyName)
                    {
                        jsonString = jsonString + col.ColumnName + ":{ key:true,create:false,edit:false,list:false},";
                    }
                }

                //add other fields; these fields will be listable, editable etc.
                string fieldsToExclude = "CreatedOn,LastUpdatedOn,CreatedByID,LastUpdatedByID,IsDeleted";
                string[] fieldsToExcludearr = fieldsToExclude.Split(',');
                foreach (DataColumn col in ds.Tables[0].Columns)
                {
                    if (col.ColumnName != primaryKeyName && !fieldsToExcludearr.Any(x => x == col.ColumnName))
                    {
                        jsonString = jsonString + col.ColumnName + ":{" + "title:'" + col.ColumnName + "'},";
                    }
                }

                return jsonString.TrimEnd(',') + "}";

            }

            return string.Empty;
        }
        public static string GetListOfRecords(string tableName, string startIndex, string pageSize, string sortField="1")
        {
            //get total count of records
            //TODO : We need to pass tableName as parameter instead of string concatenation.
            string countQry = "Select count(*) from " + tableName + " WHERE ISDELETED = 0";
            string recordsQry = @"SELECT  *
                                FROM    ( SELECT    ROW_NUMBER() OVER ( ORDER BY {0} ) AS RowNum, *
                                          FROM      {1} WHERE ISDELETED = 0
                                        ) AS RowConstrainedResult
                                WHERE   RowNum >= {2}
                                    AND RowNum < {3}
                                ORDER BY RowNum";


            Database db;
            string sqlCommand;
            DbCommand dbCommand;
            DatabaseProviderFactory factory = new DatabaseProviderFactory();
            db = factory.Create("ExampleDatabase");
            sqlCommand = countQry;
            dbCommand = db.GetSqlStringCommand(sqlCommand);
            int count;
            count = (int)db.ExecuteScalar(dbCommand);

            //get records
            int endIndex = Int32.Parse(startIndex) + Int32.Parse(pageSize);
            string getRecordsQry = String.Format(recordsQry, sortField, tableName, startIndex, endIndex.ToString());
            dbCommand = db.GetSqlStringCommand(getRecordsQry);
            DataSet ds = db.ExecuteDataSet(dbCommand);

            string allRec = "";

            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                var rec = GetSingleRecord(ds,dr) + "},";
                allRec = allRec + rec;
            }
            string jsonString = "{\"Result\":\"OK\",\"Records\":[" + allRec.TrimEnd(',') + "],\"TotalRecordCount\":" + count.ToString() + "}";
            JToken token = JToken.Parse(jsonString);
            JObject json = JObject.Parse(token.ToString());
            return json.ToString();
            //return this.Content(returntext, "application/json");
        }
        public static string AddRecord(string tableName, string fieldList, string fieldValues)
        {
            string sqlCommand = "insert into " + tableName + "(" + fieldList + ") Values (" + fieldValues + "); select @@IDENTITY";
            DatabaseProviderFactory factory = new DatabaseProviderFactory();
            Database db = factory.Create("ExampleDatabase");
            DbCommand dbCommand = db.GetSqlStringCommand(sqlCommand);
            int newId = Convert.ToInt32(db.ExecuteScalar(dbCommand));

            //get primary key
            string primaryKey = GetPrimaryKey(tableName);
    
            //return the record
            sqlCommand = "select * from " + tableName + " where " + primaryKey + " = " + newId.ToString();
            dbCommand = db.GetSqlStringCommand(sqlCommand);
            DataSet ds = db.ExecuteDataSet(dbCommand);
            //assuming that we will get a table and a row everytime
            string newRec = GetSingleRecord(ds, ds.Tables[0].Rows[0]);
            string newRecSuccess = "{\"Result\":\"OK\",\"Record\": " + newRec + "}}";
            return newRecSuccess;
        }
        public static bool UpdateRecord(string tableName, string updateColumnValues)
        {
            string sqlCommand = "update " + tableName + " SET " + updateColumnValues;
            DatabaseProviderFactory factory = new DatabaseProviderFactory();
            Database db = factory.Create("ExampleDatabase");
            DbCommand dbCommand = db.GetSqlStringCommand(sqlCommand);
            db.ExecuteNonQuery(dbCommand);
            return true;
        }
        public static bool DeleteRecord(string tableName, string primaryKey, string primaryKeyValue)
        {
                //string sqlCommand = "delete from " + tableName + " WHERE " + primaryKey + " = " + primaryKeyValue;
                string sqlCommand = "UPDATE " + tableName +  " SET ISDELETED = 1 WHERE " + primaryKey + " = " + primaryKeyValue;
                DatabaseProviderFactory factory = new DatabaseProviderFactory();
                Database db = factory.Create("ExampleDatabase");
                DbCommand dbCommand = db.GetSqlStringCommand(sqlCommand);
                db.ExecuteNonQuery(dbCommand);
                return true;
        }

        public static Dictionary<string, string> GetColumns(string tableName)
        {
            Database db;
            string sqlCommand;
            DbCommand dbCommand;
            DatabaseProviderFactory factory = new DatabaseProviderFactory();
            db = factory.Create("ExampleDatabase");
            sqlCommand = "select top 1 * from " + tableName;
            dbCommand = db.GetSqlStringCommand(sqlCommand);
            DataSet ds = db.ExecuteDataSet(dbCommand);
            string primaryKeyName = GetPrimaryKey(tableName);

            //first element will always be primary key
            Dictionary<string, string> ColumnsList = new Dictionary<string, string>();
            ColumnsList.Add(primaryKeyName, "System.Int32");


            if (ds != null && ds.Tables.Count > 0)
            {

                //prepare primary key; keeping it in a separate loop , not sure what can be the position
                foreach (DataColumn col in ds.Tables[0].Columns)
                {
                    if (col.ColumnName != primaryKeyName)
                    {
                        ColumnsList.Add(col.ColumnName, col.DataType.ToString());
                    }
                }
            }

            return ColumnsList;
        }

        private static string GetSingleRecord(DataSet ds, DataRow dr)
        {
            string rec = "{";
            foreach (DataColumn col in ds.Tables[0].Columns)
            {
                switch (col.DataType.ToString())
                {
                    case "System.Int64":
                    case "System.Int32":
                        //handle null
                        var intValue = dr[col.ColumnName];
                        if (intValue == DBNull.Value)
                        {
                            rec = rec + "\"" + col.ColumnName + "\":" + "null";
                        }
                        else
                        {
                            rec = rec + "\"" + col.ColumnName + "\":" + dr[col.ColumnName].ToString();
                        }
                        rec = rec + ",";
                        break;
                    case "System.String":
                        var strValue = dr[col.ColumnName];
                        if (strValue == DBNull.Value)
                        {
                            rec = rec + "\"" + col.ColumnName + "\":" + "null";
                        }
                        else
                        {
                            rec = rec + "\"" + col.ColumnName + "\":\"" + dr[col.ColumnName].ToString();
                            rec = rec + "\"" + ",";
                        }

                        break;
                    case "System.DateTime":
                        var dateValue = dr[col.ColumnName];
                        if (dateValue == DBNull.Value)
                        {
                            rec = rec + "\"" + col.ColumnName + "\":" + "null";
                        }
                        else
                        {
                            rec = rec + "\"" + col.ColumnName + "\":\"" + dr[col.ColumnName].ToString();
                            rec = rec + "\"" + ",";
                        }

                        break;
                    case "System.Boolean":
                        rec = rec + "\"" + col.ColumnName + "\":" + dr[col.ColumnName].ToString().ToLower() ?? "null";
                        rec = rec + ",";
                        break;
                    default:
                        rec = rec + "\"" + col.ColumnName + "\":" + dr[col.ColumnName].ToString() ?? "null";
                        rec = rec + ",";
                        break;
                }

                //jsonString = jsonString + col.ColumnName + ":{ key:true,create:false,edit:false,list:false},";
            }
            rec = rec.TrimEnd(',');
            return rec;
        }
        public static string GetPrimaryKey(string tableName)
        {
            string primaryKeyQuery = "SELECT KU.table_name as tablename,column_name as primarykeycolumn FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC " +
"INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU ON TC.CONSTRAINT_TYPE = 'PRIMARY KEY' AND TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME and ku.table_name='{0}' " +
"ORDER BY KU.TABLE_NAME, KU.ORDINAL_POSITION;";
            Database db;
            string sqlCommand;
            DbCommand dbCommand;
            DatabaseProviderFactory factory = new DatabaseProviderFactory();
            db = factory.Create("ExampleDatabase");
            sqlCommand = String.Format(primaryKeyQuery, tableName);
            dbCommand = db.GetSqlStringCommand(sqlCommand);

            string primaryKeyName = string.Empty;
            using (IDataReader dr = db.ExecuteReader(dbCommand))
            {
                dr.Read();
                primaryKeyName = dr[1].ToString();
            }
            return primaryKeyName;
        }

    }
}
