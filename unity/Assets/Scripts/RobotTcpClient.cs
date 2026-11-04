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
    public bool IsConnected { get; private set; }
    public event Action<RobotMessage> MessageReceived;
    public event Action<string> ConnectionChanged;
    readonly ConcurrentQueue<Action> callbacks = new ConcurrentQueue<Action>();
    readonly object writeLock = new object();
    TcpClient client;
    StreamWriter writer;
    int generation;
    bool connecting;
    float heartbeatAt;

    bool retryInitialConnection;
    float nextAttempt;
    void Start() { retryInitialConnection = connectOnStart; }
    public void Connect()
    {
        if (IsConnected || connecting) return;
        connecting = true;
        int session = Interlocked.Increment(ref generation);
        ConnectionChanged?.Invoke("CONNECTING");
        string address = host;
        int endpoint = port;
        new Thread(() => ReadSession(session, address, endpoint)) { IsBackground = true }.Start();
    }
    void Enqueue(int session, Action action)
    {
        callbacks.Enqueue(() => { if (session == generation) action(); });
    }
    void ReadSession(int session, string address, int endpoint)
    {
        using (var socket = new TcpClient())
        {
            try
            {
                var task = socket.ConnectAsync(address, endpoint);
                if (!task.Wait(3000)) throw new TimeoutException("connection_timeout");
                task.GetAwaiter().GetResult();
                socket.ReceiveTimeout = 10000;
                socket.SendTimeout = 2000;
                var output = new StreamWriter(socket.GetStream(), new UTF8Encoding(false)) { AutoFlush = true };
                lock (writeLock)
                {
                    if (session != generation) return;
                    client = socket; writer = output;
                }
                Enqueue(session, () => {
                    retryInitialConnection = false;
                    connecting = false; IsConnected = true;
                    ConnectionChanged?.Invoke("CONNECTED");
                    Send("HELLO"); heartbeatAt = Time.unscaledTime;
                });
                using (var reader = new StreamReader(socket.GetStream(), Encoding.UTF8))
                {
                    while (session == generation)
                    {
                        string line = reader.ReadLine();
                        if (line == null) break;
                        Enqueue(session, () => {
                            try { MessageReceived?.Invoke(JsonUtility.FromJson<RobotMessage>(line)); }
                            catch (Exception ex) { Debug.LogError("Robot message: " + ex.Message); }
                        });
                    }
                }
            }
            catch (Exception ex) { Enqueue(session, () => ConnectionChanged?.Invoke("ERROR: " + ex.Message)); }
            finally
            {
                lock (writeLock) { if (session == generation) { writer = null; client = null; } }
                Enqueue(session, () => { connecting = false; IsConnected = false; nextAttempt = Time.unscaledTime + 3f; ConnectionChanged?.Invoke("DISCONNECTED"); });
            }
        }
    }
    public string Send(string command)
    {
        string id = Guid.NewGuid().ToString("N");
        try
        {
            lock (writeLock)
            {
                if (!IsConnected || writer == null) return null;
                writer.WriteLine(JsonUtility.ToJson(new RobotCommand { request_id = id, command = command }));
            }
            return id;
        }
        catch (Exception ex)
        {
            Disconnect();
            ConnectionChanged?.Invoke("SEND_ERROR: " + ex.Message);
            return null;
        }
    }
    void Update()
    {
        while (callbacks.TryDequeue(out var action)) action();
        if (retryInitialConnection && !connecting && !IsConnected && Time.unscaledTime >= nextAttempt) Connect();
        if (IsConnected && heartbeatIntervalSeconds > 0 && Time.unscaledTime - heartbeatAt >= heartbeatIntervalSeconds)
        { heartbeatAt = Time.unscaledTime; Send("HEARTBEAT"); }
    }
    public void Disconnect()
    {
        retryInitialConnection = false;
        Interlocked.Increment(ref generation);
        lock (writeLock) { client?.Close(); client = null; writer = null; }
        connecting = false; IsConnected = false;
        ConnectionChanged?.Invoke("DISCONNECTED");
    }
    void OnDestroy() { Disconnect(); }
}

[Serializable]
public class RobotCommand
{
    public string version = "1.0";
    public string type = "command";
    public string request_id;
    public string command;
}
