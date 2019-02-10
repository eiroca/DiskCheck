using System;
using System.IO;
using System.Net;
using System.Text;

namespace HttpUtils {

  public enum HttpVerb {
    GET,
    POST,
    PUT,
    DELETE
  }

  public class RestClient {

    public string EndPoint { get; set; } = null;
    public HttpVerb Method { get; set; } = HttpVerb.GET;
    public string ContentType { get; set; } = "text/json";
    public string PostData { get; set; } = "";

    public bool useProxy = true;
    public int timeOut = 5000;

    public RestClient() {
    }

    public RestClient(string endpoint) {
      EndPoint = endpoint;
    }

    public RestClient(string endpoint, HttpVerb method) {
      EndPoint = endpoint;
      Method = method;
    }

    public RestClient(string endpoint, HttpVerb method, string postData) {
      EndPoint = endpoint;
      Method = method;
      PostData = postData;
    }

    public RestClient(string endpoint, HttpVerb method, string contentType, string postData) {
      EndPoint = endpoint;
      Method = method;
      ContentType = contentType;
      PostData = postData;
    }


    public string MakeRequest() {
      return MakeRequest("");
    }

    public string MakeRequest(params string[] parameters) {
      if (EndPoint == null) return null;
      var request = (HttpWebRequest)WebRequest.Create(String.Format(EndPoint, parameters));
      request.Timeout = timeOut;
      if (!useProxy) {
        request.Proxy = null;
      }
      request.Method = Method.ToString();
      request.ContentLength = 0;
      request.ContentType = ContentType;
      if (!string.IsNullOrEmpty(PostData) && Method == HttpVerb.POST) {
        var encoding = new UTF8Encoding();
        var bytes = Encoding.GetEncoding("iso-8859-1").GetBytes(PostData);
        request.ContentLength = bytes.Length;
        var writeStream = request.GetRequestStream();
        writeStream.Write(bytes, 0, bytes.Length);
      }
      var response = (HttpWebResponse)request.GetResponse();
      var responseValue = string.Empty;
      if (response.StatusCode != HttpStatusCode.OK) {
        var message = String.Format("Request failed. Received HTTP {0}", response.StatusCode);
        throw new ApplicationException(message);
      }
      // grab the response
      var responseStream = response.GetResponseStream();
      if (responseStream != null) {
        var reader = new StreamReader(responseStream);
        responseValue = reader.ReadToEnd();
      }
      return responseValue;
    }

  }

}