using jTable.AdoNet.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace jTable.Controllers
{
    public class DefaultController : Controller
    {
        // GET: Default

        //public ActionResult Index() //PHANI
        //{
        //    const string listUrl = "/default/List?tablename={0}";
        //    ViewBag.MasterTable = String.Format(listUrl, "MasterTable");

            
        //    return View();
        //}


        public ActionResult Index(string tableName) //PHANI
        {
            if (!string.IsNullOrEmpty(tableName))
            {
                const string listUrl = "/default/List?tablename={0}";
                const string addUrl = "/default/add?tablename={0}";
                const string updateUrl = "/default/update?tablename={0}";
                const string deleteUrl = "/default/delete?tablename={0}";
                //string tableName = "Projects";

                ViewBag.FieldData = MasterTableDAL.GetJsonFields(tableName);
                ViewBag.ListUrl = String.Format(listUrl, tableName);
                ViewBag.AddUrl = String.Format(addUrl, tableName);
                ViewBag.UpdateUrl = String.Format(updateUrl, tableName);
                ViewBag.DeleteUrl = String.Format(deleteUrl, tableName);
            }
            
            //PHANI
            const string listUrl1 = "/default/List?tablename={0}";
            ViewBag.MasterTable = String.Format(listUrl1, "MasterTable");
            //
            return View();
        }
        public ContentResult List()
        {
            ///default/List?tablename=mstr_country&jtStartIndex=0&jtPageSize=10&jtSorting=Description%20ASC

            string pageSize = "10", startIndex = "", tableName = "", sortCriteria = "";

            if (!String.IsNullOrEmpty(Request.QueryString["jtPageSize"]))
            {
                pageSize = Request.QueryString["jtPageSize"];
            }
            if (!String.IsNullOrEmpty(Request.QueryString["jtStartIndex"]))
            {
                startIndex = Request.QueryString["jtStartIndex"];
            }
            if (!String.IsNullOrEmpty(Request.QueryString["tableName"]))
            {
                tableName = Request.QueryString["tableName"];
            }
            if (!String.IsNullOrEmpty(Request.QueryString["jtSorting"]))
            {
                sortCriteria = Request.QueryString["jtSorting"];
            }
            string records = MasterTableDAL.GetListOfRecords(tableName, startIndex, pageSize, sortCriteria);
            return this.Content(records, "application/json");
            //return null;
        }
        [HttpPost]
        public ContentResult Add()
        {
            string tableName = "";
            if (!String.IsNullOrEmpty(Request.QueryString["tableName"]))
            {
                tableName = Request.QueryString["tableName"];
            }
            //we will be receiving all values in form variables.
            Dictionary<string, string> ColumnList = MasterTableDAL.GetColumns(tableName);
            List<ColumnFieldValue> columnFieldValueList = new List<ColumnFieldValue>();

            foreach (var item in ColumnList)
            {
                foreach (string key in Request.Form.AllKeys)
                {
                    if (key.ToLower() == item.Key.ToLower())
                    {
                        var columnFieldValue = new ColumnFieldValue();
                        columnFieldValue.ColumnName = item.Key;
                        columnFieldValue.ColumnValue = Request.Form[key];
                        columnFieldValue.ColumnType = item.Value;
                        columnFieldValueList.Add(columnFieldValue);
                    }
                }
            }

            //create column string
            string strColumns = String.Join(",", columnFieldValueList.Select(x => x.ColumnName));
            string strValues = "";

            foreach (var column in columnFieldValueList)
            {
                switch (column.ColumnType)
                {
                    case "System.Int64":
                    case "System.Int32":
                    case "System.Boolean":
                        strValues = strValues + column.ColumnValue + ",";
                        break;
                    case "System.String":
                    case "System.DateTime":
                        strValues = strValues + "'" + column.ColumnValue + "'" + ",";
                        break;
                }
            }
            strValues = strValues.TrimEnd(',');

            //audit fields
            //assuming that there will be some audit fields, if not then this code can be removed.
            string userid= "1";
            strColumns = strColumns;//+ ",CreatedOn,LastUpdatedOn,CreatedByID,LastUpdatedByID,IsDeleted"; //PHANI
            strValues = strValues;// + ",'" + DateTime.Now.ToString() + "','" + DateTime.Now.ToString() + "'," + userid + "," + userid + "," + "0";
            string jsonResult =  MasterTableDAL.AddRecord(tableName, strColumns, strValues);
            return Content(jsonResult, "application/json");
        }
        [HttpPost]
        public ContentResult Update()
        {
            string tableName = "";
            if (!String.IsNullOrEmpty(Request.QueryString["tableName"]))
            {
                tableName = Request.QueryString["tableName"];
            }
            //we will be receiving all values in form variables.
            Dictionary<string, string> ColumnList = MasterTableDAL.GetColumns(tableName);
            List<ColumnFieldValue> columnFieldValueList = new List<ColumnFieldValue>();

            foreach (var item in ColumnList)
            {
                foreach (string key in Request.Form.AllKeys)
                {
                    if (key.ToLower() == item.Key.ToLower() && key.ToLower() != ColumnList.Keys.FirstOrDefault().ToLower())
                    {
                        var columnFieldValue = new ColumnFieldValue();
                        columnFieldValue.ColumnName = item.Key;
                        columnFieldValue.ColumnValue = Request.Form[key];
                        columnFieldValue.ColumnType = item.Value;
                        columnFieldValueList.Add(columnFieldValue);
                    }
                }
            }

            //create column string
            string strColumnUpdate = "";

            foreach (var column in columnFieldValueList)
            {
                switch (column.ColumnType)
                {
                    case "System.Int64":
                    case "System.Int32":
                    case "System.Boolean":
                        strColumnUpdate = strColumnUpdate + column.ColumnName + "=" + column.ColumnValue + ",";
                        break;
                    case "System.String":
                    case "System.DateTime":
                        strColumnUpdate = strColumnUpdate + column.ColumnName + "='" + column.ColumnValue + "',";
                        break;
                }
            }
            strColumnUpdate = strColumnUpdate.TrimEnd(',');

            //audit fields
            string userid = "1";
            //strColumnUpdate = strColumnUpdate + ",LastUpdatedOn = '" + DateTime.Now.ToString() + "'"; //PHANI
            //strColumnUpdate = strColumnUpdate + ",LastUpdatedByID = " + userid;
            strColumnUpdate = strColumnUpdate + " WHERE " + ColumnList.Keys.FirstOrDefault() + " = " + Request.Form[ColumnList.Keys.FirstOrDefault()]; 
            MasterTableDAL.UpdateRecord(tableName, strColumnUpdate);
            return Content("{\"Result\":\"OK\"}", "application/json");
        }

        [HttpPost]
        public ContentResult Delete()
        {
            string tableName = "";
            if (!String.IsNullOrEmpty(Request.QueryString["tableName"]))
            {
                tableName = Request.QueryString["tableName"];
            }

            string primaryKey = MasterTableDAL.GetPrimaryKey(tableName);

            MasterTableDAL.DeleteRecord(tableName, primaryKey, Request.Form[primaryKey]);

            return Content("{\"Result\":\"OK\"}", "application/json");

        }

    }

    public class ColumnFieldValue
    {
        public string ColumnName { get; set; }
        public string ColumnType { get; set; }
        public string ColumnValue { get; set; }
    }

}