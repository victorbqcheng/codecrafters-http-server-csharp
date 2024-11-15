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
    var client = server.AcceptTcpClient(); // wait for client
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
            (string method, string path) = ParseHttpRequest(receiveData);
            string response = "HTTP/1.1 404 Not Found\r\n\r\n";
            if (path == "/")
            {
                response = "HTTP/1.1 200 OK\r\n" + 
                "Content-Length: 0" +
                "\r\n" +
                "";
            }
            else if (Echo(path))
            {
                string status = "HTTP/1.1 200 OK\r\n";
                string body = path.Substring(6);    // remove "/echo/"
                string contentType = "text/plain";
                string contentLength = Encoding.UTF8.GetByteCount(body).ToString();
                response = status +
                    $"Content-Type: {contentType}\r\n" +
                    $"Content-Length: {contentLength}\r\n" +
                    "\r\n" +
                    body;
            }
            byte[] data = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(data, 0, data.Length);
        }
    }
    client.Close();
}

static (string Method, string Path) ParseHttpRequest(string requestText)
{
    string[] lines = requestText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
    if (lines.Length == 0)
    {
        return ("", "");
    }

    string[] parts = lines[0].Split(" ");
    if (parts.Length != 3)
    {
        return ("", "");
    }
    return (parts[0], parts[1]);
}

static bool Echo(string path)
{
    return path.StartsWith("/echo/");
}

// clientSocket.Send(Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n\r\n"));


