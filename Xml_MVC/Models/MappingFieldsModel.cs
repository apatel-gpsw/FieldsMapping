using System.Collections.Generic;

namespace Xml_MVC.Models
{
	public class MappingFieldsModel
	{
		public string IncomingFieldName { get; set; }
		public IEnumerable<string> SystemFields { get; set; }
	}
}