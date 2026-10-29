using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class RobotTcpClient : MonoBehaviour
{
    public string host = "127.0.0.1";
    public int port = 5000;
    public bool connectOnStart = true;
    public float heartbeatIntervalSeconds = 1f;

    public event Action<RobotMessage> MessageReceived;
    public event Action<string> ConnectionChanged;

    private TcpClient client;
    private StreamWriter writer;
    private Thread readerThread;
    private readonly ConcurrentQueue<string> incoming = new ConcurrentQueue<string>();
    private volatile bool running;
    private int requestCounter;

    private void Start()
    {
        if (connectOnStart) Connect();
    }

    public void Connect()
    {
        if (running) return;
        try
        {
            client = new TcpClient();
            client.Connect(host, port);
            writer = new StreamWriter(client.GetStream(), new UTF8Encoding(false)) { AutoFlush = true };
            running = true;
            readerThread = new Thread(ReadLoop) { IsBackground = true };
            readerThread.Start();
            ConnectionChanged?.Invoke("CONNECTED");
            Send("HELLO");
            if (heartbeatIntervalSeconds > 0f) InvokeRepeating(nameof(SendHeartbeat), heartbeatIntervalSeconds, heartbeatIntervalSeconds);
        }
        catch (Exception ex)
        {
            ConnectionChanged?.Invoke("ERROR: " + ex.Message);
        }
    }

    private void SendHeartbeat()
    {
        if (running) Send("HEARTBEAT");
    }

    public string Send(string command)
    {
        string requestId = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + Interlocked.Increment(ref requestCounter);
        string json = "{\"version\":\"1.0\",\"type\":\"command\",\"request_id\":\"" + requestId + "\",\"command\":\"" + command + "\"}";
        try
        {
            if (!running || writer == null) throw new InvalidOperationException("not_connected");
            writer.WriteLine(json);
        }
        catch (Exception ex)
        {
            ConnectionChanged?.Invoke("SEND_ERROR: " + ex.Message);
        }
        return requestId;
    }

    private void ReadLoop()
    {
        try
        {
            using (var reader = new StreamReader(client.GetStream(), Encoding.UTF8, false, 1024, true))
            {
                while (running)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    incoming.Enqueue(line);
                }
            }
        }
        catch (Exception ex)
        {
            if (running) ConnectionChanged?.Invoke("READ_ERROR: " + ex.Message);
        }
        finally
        {
            running = false;
            ConnectionChanged?.Invoke("DISCONNECTED");
        }
    }

    private void Update()
    {
        while (incoming.TryDequeue(out string json))
        {
            try
            {
                RobotMessage message = JsonUtility.FromJson<RobotMessage>(json);
                MessageReceived?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError("Robot message parse failed: " + ex.Message);
            }
        }
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(SendHeartbeat));
        running = false;
        try { client?.Close(); } catch { }
        try { readerThread?.Join(200); } catch { }
    }
}
