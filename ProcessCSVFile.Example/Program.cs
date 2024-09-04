using Microsoft.VisualBasic.FileIO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProcessCSVFile.Example
{
    /// <summary>Program class</summary>
    public class Program
    {
        /// <summary>Main function</summary>
        /// <param name="args">The arguments</param>
        public static void Main(string[] args)
        {
            // Get the file content
            TextFieldParser parser = GetFileContent();

            // Create a list of lines
            var lines = new List<string>();

            // Add header
            lines.Add("Date;Url;CustomerId;MaxResult;FirstResult;Status;PartnerCode;DateFrom;DateTo;OrderField;OrderType");

            // Read the file
            if (parser != null)
            {
                while (!parser.EndOfData)
                {
                    // Get the fields
                    string[] fields = parser.ReadFields();
                    var date = fields[0];
                    var fullLog = fields[1];

                    // Clean the full log data
                    var processedLog = CleanFullLog(fullLog);

                    // Gets the list of logs from the full log
                    var listData = GetListObjectLog(processedLog);

                    // Extract the url from the list of logs
                    var url = ExtractUrl(listData);

                    // Extract the params from the url
                    var param = ToParam(url);

                    // Create line
                    var line = CreateLine(date, url, param);

                    // Add line to list
                    lines.Add(line);
                }
            }

            // Generate the new file
            GenerateFile(lines);
        }

        /// <summary>Gets the file content</summary>
        /// <returns>The parser with the file information</returns>
        private static TextFieldParser GetFileContent()
        {
            var filePath = "C:\\Files\\file.csv";
            var parser = new TextFieldParser(filePath)
            {
                TextFieldType = FieldType.Delimited
            };
            parser.SetDelimiters(new string[] { ";" });
            return parser;
        }

        /// <summary>Creates new line with the params</summary>
        /// <param name="date">The date of the request</param>
        /// <param name="url">The url of the request</param>
        /// <param name="param">The param object</param>
        /// <returns>The new line</returns>
        public static string CreateLine(string date, string url, Param param)
        {
            var str = new StringBuilder();
            str.Append(date.ToString() + ";");
            str.Append(url + ";");
            str.Append(param.CustomerId + ";");
            str.Append(param.MaxResult + ";");
            str.Append(param.FirstResult + ";");
            str.Append(param.Status + ";");
            str.Append(param.PartnerCode + ";");
            str.Append(param.DateFrom + ";");
            str.Append(param.DateTo + ";");
            str.Append(param.OrderField + ";");
            str.Append(param.OrderType + ";");

            return str.ToString();
        }

        /// <summary>Extract the params of a url</summary>
        /// <param name="url">The url of a param</param>
        /// <returns>The param object</returns>
        public static Param ToParam(string url)
        {
            var param = new Param
            {
                CustomerId = GetParamCustomerId(url),
                MaxResult = int.Parse(GetParamValue(url, "maxResults=")),
                FirstResult = int.Parse(GetParamValue(url, "firstResult=")),
                Status = GetParamValue(url, "status="),
                PartnerCode = GetParamValue(url, "partnerCode="),
                DateFrom = GetParamValue(url, "dateFrom="),
                DateTo = GetParamValue(url, "dateTo="),
                OrderField = GetParamValue(url, "orderField="),
                OrderType = GetParamValue(url, "orderType=")
            };
            return param;
        }

        /// <summary>Gets the customer id param from the url</summary>
        /// <param name="url">The url</param>
        /// <returns>The customer id</returns>
        public static int GetParamCustomerId(string url)
        {
            var str = url.Substring(0, url.IndexOf("/transactions?"));
            str = str.Replace("/transaction/customers/", "");
            return int.Parse(str);
        }

        /// <summary>Gets a param value from his name and url</summary>
        /// <param name="url">The url</param>
        /// <param name="paramName">The param name</param>
        /// <returns>The param value</returns>
        public static string GetParamValue(string url, string paramName)
        {
            var str = "";
            if (url != null)
            {
                var pos = url.IndexOf(paramName);
                if (pos >= 0)
                {
                    str = url[pos..];
                    str = str[..str.IndexOf("&")];
                    str = str.Replace(paramName, "");
                }
            }
            return str;
        }

        /// <summary>Creates a new file</summary>
        /// <param name="lines">The lines</param>
        public static void GenerateFile(List<string> lines)
        {
            var date = DateTime.Now;
            var strDate = $"{date:yyyy-MM-dd.HH.mm.ss}";
            var filePath = $"C:\\Files\\file_generated_{strDate}.csv";
            using var sw = new StreamWriter(filePath, true);
            foreach (var line in lines)
            {
                sw.Write(line);
                sw.WriteLine();
            }
        }

        /// <summary>Extracts the url from a list of logs</summary>
        /// <returns>The url</returns>
        public static string? ExtractUrl(List<Log> logs)
        {
            foreach (var log in logs)
            {
                if (log.Msg.Contains("Call to GET /transaction/customers/")
                    && log.Msg.Contains("/transactions?maxResults")
                )
                {
                    return CleanUrl(log.Msg);
                }
            }

            return null;
        }

        /// <summary>Cleans the log text informed</summary>
        /// <param name="log">The log text</param>
        /// <returns>The url cleaned</returns>
        public static string CleanUrl(string log)
        {
            // Cuts the beginning of the text from the index found
            var str = log[log.IndexOf("/transaction/customers/")..];

            // Cuts the end of the text from the index found
            str = str[..str.IndexOf(" took")];

            // Replaces the text
            str = str.Replace("u0026", "&");

            // Add the simbol in the end
            str += "&";

            return str;
        }

        /// <summary>Cleans the full log text</summary>
        /// <param name="fullLog">The full log text</param>
        /// <returns>The cleaned full log</returns>
        public static string CleanFullLog(string fullLog)
        {
            var str = $@"{fullLog.Replace(@"\", @"")}";

            // Cuts the beginning of the text from the value 1
            str = str[1..];

            // Cuts the end of the text from the value 1
            str = str[..^1];
            return str;
        }

        /// <summary>Gets the list of logs</summary>
        /// <param name="strJson">The json string</param>
        /// <returns>The list of objects</returns>
        public static List<Log> GetListObjectLog(string strJson)
        {
            var list = new List<Log>();
            if (strJson != null)
            {
                list = JsonSerializer.Deserialize<List<Log>>(strJson);
            }
            return list;
        }

        #region Classes

        /// <summary>Contains information about the log</summary>
        public class Log
        {
            /// <summary>The log message</summary>
            [JsonPropertyName("msg")]
            public string? Msg { get; set; }

            /// <summary>The log datetime</summary>
            [JsonPropertyName("dateTime")]
            public string? DateTime { get; set; }
        }

        /// <summary>Contains the params of the request</summary>
        public class Param
        {
            /// <summary>The customer id</summary>
            public int CustomerId { get; set; }

            /// <summary>The max result request</summary>
            public int MaxResult { get; set; }

            /// <summary>The number of the first item request</summary>
            public int FirstResult { get; set; }

            /// <summary>The status</summary>
            public string? Status { get; set; }

            /// <summary>The partner code</summary>
            public string? PartnerCode { get; set; }

            /// <summary>The date from</summary>
            public string? DateFrom { get; set; }

            /// <summary>The date to</summary>
            public string? DateTo { get; set; }

            /// <summary>The order field</summary>
            public string? OrderField { get; set; }

            /// <summary>The order type</summary>
            public string? OrderType { get; set; }
        }

        #endregion Classes
    }
}