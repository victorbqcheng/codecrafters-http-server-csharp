using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

while (true)
{
    var client = await server.AcceptTcpClientAsync(); // wait for client
    _ = Task.Run(() =>
    {
        HandleClient(client);
    });
}

static Task HandleClient(TcpClient client)
{
    using (NetworkStream stream = client.GetStream())
    {
        byte[] buffer = new byte[1024];
        int bytesRead;
        bytesRead = stream.Read(buffer, 0, buffer.Length);
        {
            string receiveData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            HttpRequest request = ParseHttpRequest(receiveData);
            byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n");
            if (request.Path == "/")
            {
                Console.WriteLine("Root endpoint");
                response = RootEndPoint(request);
                Console.WriteLine("response:" + response);
            }
            else if (Echo(request.Path))
            {
                response = EcohEndPoint(request);
            }
            else if (request.Path == "/user-agent")
            {
                response = UserAgentEndPoint(request);
            }
            else if (request.Path.StartsWith("/files/") && request.Method == "POST")
            {
                response = PostFilesEndPoint(request);
            }
            else if (request.Path.StartsWith("/files/"))
            {
                response = FilesEndPoint(request);
            }
            stream.Write(response, 0, response.Length);
        }
    }
    client.Close();
    return Task.CompletedTask;
}
// Parse the HTTP request
static HttpRequest ParseHttpRequest(string requestText)
{
    HttpRequest request = new HttpRequest { };
    string[] lines = requestText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
    if (lines.Length == 0)
    {
        return request;
    }
    string[] parts = lines[0].Split(" ");
    if (parts.Length != 3)
    {
        return request;
    }
    request.Method = parts[0].ToUpper();
    request.Path = parts[1];
    request.Version = parts[2];
    for (int i = 1; i < lines.Length; i++)
    {
        string[] headerParts = lines[i].Split(": ");
        if (headerParts.Length == 2)
        {
            request.Headers[headerParts[0]] = headerParts[1];
        }
    }
    if (request.Headers.ContainsKey("Content-Length"))
    {
        int contentLength = int.Parse(request.Headers["Content-Length"]);
        request.Body = lines[lines.Length - 1];
    }

    return request;
}
// Check if the path starts with "/echo/"
static bool Echo(string path)
{
    return path.StartsWith("/echo/");
}
// Compress the data using GZip
static byte[] CompressData(byte[] data)
{
    using (var memoryStream = new MemoryStream())
    {
        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
        {
            gzipStream.Write(data, 0, data.Length);
        }
        return memoryStream.ToArray();
    }
}
// Implement the root(/) endpoint
static byte[] RootEndPoint(HttpRequest request)
{
    string status = "HTTP/1.1 200 OK\r\n";
    string body = "{\"msg\": \"Daumen hoch!\"}";
    string response = status +
        "Content-Type: text/json\r\n" +
        $"Content-Length: {body.Length}\r\n" +
        "\r\n" +
        body;
    return Encoding.UTF8.GetBytes(response);
}
// Implement the /echo endpoint
static byte[] EcohEndPoint(HttpRequest request)
{
    string status = "HTTP/1.1 200 OK\r\n";
    string body = request.Path.Substring(6);

    string contentEncoding = "";
    byte[] data = Encoding.UTF8.GetBytes(body);

    if (request.Headers.ContainsKey("Accept-Encoding") && request.Headers["Accept-Encoding"].Contains("gzip"))
    {
        contentEncoding = "Content-Encoding: gzip\r\n";
        data = CompressData(data);
    }

    string contentType = "text/plain";
    string contentLength = Encoding.UTF8.GetByteCount(body).ToString();
    string headers = status +
        contentEncoding +
        $"Content-Type: {contentType}\r\n" +
        $"Content-Length: {data.Length}\r\n" +
        "\r\n";
    byte[] headerBytes = Encoding.UTF8.GetBytes(headers);
    byte[] response = new byte[headerBytes.Length + data.Length];
    headerBytes.CopyTo(response, 0);
    data.CopyTo(response, headerBytes.Length);

    return response;
}

// Implement the /user-agent endpoint
static byte[] UserAgentEndPoint(HttpRequest request)
{
    string userAgent = request.Headers["User-Agent"];
    string status = "HTTP/1.1 200 OK\r\n";
    string contentType = "text/plain";
    string contentLength = Encoding.UTF8.GetByteCount(userAgent).ToString();
    string response = status +
        $"Content-Type: {contentType}\r\n" +
        $"Content-Length: {contentLength}\r\n" +
        "\r\n" +
        userAgent;

    return Encoding.UTF8.GetBytes(response);
}
static byte[] FilesEndPoint(HttpRequest request)
{
    var args = Environment.GetCommandLineArgs();
    string dir = args[2];
    string path = request.Path.Substring(7);
    string filePath = dir + path;
    if (File.Exists(filePath))
    {
        string status = "HTTP/1.1 200 OK\r\n";
        string contentType = "application/octet-stream";
        string contentLength = new FileInfo(filePath).Length.ToString();
        string body = File.ReadAllText(filePath);
        string response = status +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {contentLength}\r\n" +
            "\r\n" +
            body;
        return Encoding.UTF8.GetBytes(response);
    }
    else
    {
        return Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n");
    }
}

static byte[] PostFilesEndPoint(HttpRequest request)
{
    var args = Environment.GetCommandLineArgs();
    string dir = args[2];
    string path = request.Path.Substring(7);
    string filePath = dir + path;

    string body = request.Body;
    File.WriteAllText(filePath, body);

    return Encoding.UTF8.GetBytes("HTTP/1.1 201 Created\r\n\r\n");
}

class HttpRequest
{
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public string Version { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public string Body { get; set; } = "";

}

