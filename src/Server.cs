using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

if(args.Length<2)
{
    Console.WriteLine("Usage: --directory /tmp/");
    return;
}

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();

while (true)
{
    var client = await server.AcceptTcpClientAsync(); // wait for client
    await HandleClientAsync(client);

}

static async Task HandleClientAsync(TcpClient client)
{
    
    using (NetworkStream stream = client.GetStream())
    {
        byte[] buffer = new byte[1024];
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
        {
            string receiveData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            HttpRequest request = ParseHttpRequest(receiveData);
            string response = "HTTP/1.1 404 Not Found\r\n\r\n";
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
            else if (request.Path.StartsWith("/files/"))
            {
                response = FilesEndPoint(request);
            }
            byte[] data = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(data, 0, data.Length);
        }
    }
    client.Close();
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
    request.Method = parts[0];
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
// Implement the root(/) endpoint
static string RootEndPoint(HttpRequest request)
{
    string status = "HTTP/1.1 200 OK\r\n";
    string body = "{\"msg\": \"Daumen hoch!\"}";
    string response = status +
        "Content-Type: text/json\r\n" +
        $"Content-Length: {body.Length}\r\n" +
        "\r\n" +
        body;
    return response;
}
// Implement the /echo endpoint
static string EcohEndPoint(HttpRequest request)
{
    string status = "HTTP/1.1 200 OK\r\n";
    string body = request.Path.Substring(6);
    string contentType = "text/plain";
    string contentLength = Encoding.UTF8.GetByteCount(body).ToString();
    string response = status +
        $"Content-Type: {contentType}\r\n" +
        $"Content-Length: {contentLength}\r\n" +
        "\r\n" +
        body;
    return response;
}

// Implement the /user-agent endpoint
static string UserAgentEndPoint(HttpRequest request)
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

    return response;
}

static string FilesEndPoint(HttpRequest request)
{
    var args = Environment.GetCommandLineArgs();
    string dir = args[1];
    string path = request.Path.Substring(7);
    string filePath = dir + path;
    if (File.Exists(filePath))
    {
        string status = "HTTP/1.1 200 OK\r\n";
        string contentType = "text/plain";
        string contentLength = new FileInfo(filePath).Length.ToString();
        string body = File.ReadAllText(filePath);
        string response = status +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {contentLength}\r\n" +
            "\r\n" +
            body;
        return response;
    }
    else
    {
        return "HTTP/1.1 404 Not Found\r\n\r\n";
    }
}

class HttpRequest
{
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public string Version { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    public string Body { get; set; } = "";

}

