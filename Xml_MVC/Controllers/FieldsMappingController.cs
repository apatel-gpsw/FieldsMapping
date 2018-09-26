using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IO;
using System.Web.Mvc;
using System.Xml;
using Xml_MVC.Models;

namespace Xml_MVC.Controllers
{
	public class FieldsMappingController : Controller
	{
		HashSet<string> internalFields = new HashSet<string>();
		// GET: FieldsMapping
		public ActionResult Index()
		{
			if(TempData["userData"] == null)
			{
				ViewBag.ShowList = false;
				return View();
			}
			else
			{
				List<MappingFieldsModel> lst = (List<MappingFieldsModel>)TempData["userData"];
				ViewBag.ShowList = true;
				return View(lst);
			}
		}

		[HttpPost]
		public ActionResult Upload()
		{
			try
			{
				ReadSystemFields();

				List<MappingFieldsModel> fieldsList = new List<MappingFieldsModel>();
				var file = Request.Files[0];
				if(file != null && file.ContentLength > 0)
				{
					XmlDocument xmldoc = new XmlDocument();
					xmldoc.Load(file.InputStream);
					MappingFieldsModel outputFields;
					XmlNode firstProductNode = xmldoc.GetElementsByTagName("Product")[0];

					foreach(XmlNode sectionNode in firstProductNode.ChildNodes)
					{
						foreach(XmlNode childNode in sectionNode.ChildNodes)
						{
							string nodeName = childNode.LocalName;
							List<string> list = new List<string>();
							list.Add("");

							outputFields = new MappingFieldsModel();
							outputFields.IncomingFieldName = nodeName;

							list.AddRange(internalFields);
							if(internalFields.Contains(nodeName))
							{
								list.Remove("");
								list.Remove(nodeName);
								list.Insert(0, nodeName);
							}
							outputFields.SystemFields = list;
							fieldsList.Add(outputFields);
						}
					}

					TempData["userData"] = fieldsList;
				}
				return RedirectToAction("Index");
			}
			catch(Exception ex)
			{
				var error = ex;
				throw;
			}
		}

		[HttpPost]
		public ActionResult Export(string[][] exportData)
		{
			List<ExportDataModel> data = new List<ExportDataModel>();
			foreach(string[] row in exportData)
			{
				data.Add(new ExportDataModel
				{
					IncomingField = row[0],
					OutgoingField = row[1]
				});
			}

			string fileName = $"{ Guid.NewGuid().ToString() }.csv";
			// string file = $@"C:\Drops\{fileName}";
			string file = Path.Combine(Server.MapPath("~/Content"), fileName);
			WriteTsv(data, file);

			return new JsonResult()
			{
				Data = new { FileName = fileName }
			};
		}

		/// <summary>
		/// Simple get request to receive the file.
		/// </summary>
		/// <param name="file">The Guid</param>
		/// <returns></returns>
		[HttpGet]
		public virtual ActionResult Download(string file)
		{
			// string fullPath = Path.Combine($@"C:\Drops", file);
			string fullPath = Path.Combine(Server.MapPath("~/Content"), file);
			return File(fullPath, "application/vnd.ms-excel", file);
		}

		/// <summary>
		/// Converts data into a physical csv file.
		/// </summary>
		/// <param name="data">In form of <see cref="ExportDataModel"/> list</param>
		/// <param name="file">Location</param>
		public void WriteTsv<T>(IEnumerable<T> data, string file)
		{
			using(StreamWriter output = new StreamWriter(file))
			{
				PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(T));
				foreach(PropertyDescriptor prop in props)
				{
					output.Write(prop.DisplayName); // header
					output.Write(",");
				}
				output.WriteLine();
				foreach(T item in data)
				{
					foreach(PropertyDescriptor prop in props)
					{
						output.Write(prop.Converter.ConvertToString(
							 prop.GetValue(item)));
						output.Write(",");
					}
					output.WriteLine();
				}
			}
		}

		/// <summary>
		/// Basically looks at the DB table and saves all the columns in <see cref="internalFields"/>
		/// </summary>
		private void ReadSystemFields()
		{
			string connetionString = null;
			SqlConnection myConnection;
			connetionString = "Data Source=localhost;Initial Catalog=;Integrated Security=SSPI;Application Name=Monster";

			myConnection = new SqlConnection(connetionString);
			SqlDataReader myReader = null;
			string query = @"
SELECT c.Name
FROM sys.columns c
JOIN sys.objects o ON o.object_id = c.object_id
WHERE o.object_id = OBJECT_ID('')
ORDER BY c.Name";
			SqlCommand myCommand = new SqlCommand(query, myConnection);

			try
			{
				myConnection.Open();
				myReader = myCommand.ExecuteReader();
				while(myReader.Read())
				{
					// orderFields.Add(myReader["Name"].ToString());
					internalFields.Add(myReader["Name"].ToString());
				}
			}
			catch(Exception ex)
			{
				throw ex;
			}
			finally
			{
				myConnection.Close();
			}
		}
	}
}