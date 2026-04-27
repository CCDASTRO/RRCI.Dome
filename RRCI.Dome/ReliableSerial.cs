using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

public class ReliableSerial : IDisposable
{
    private SerialPort port;
    private StringBuilder buffer = new StringBuilder();

    private AutoResetEvent ackEvent = new AutoResetEvent(false);
    private AutoResetEvent pongEvent = new AutoResetEvent(false);

    public event Action<string> MessageReceived;

    private Timer heartbeat;

    public void Connect(string com)
    {
        port = new SerialPort(com, 9600);
        port.DataReceived += DataReceived;
        port.Open();

        heartbeat = new Timer(_ => Ping(), null, 2000, 2000);
    }

    private void Ping()
    {
        try
        {
            pongEvent.Reset();
            port.Write("ping#");

            if (!pongEvent.WaitOne(1000))
                MessageReceived?.Invoke("DISCONNECTED");
        }
        catch
        {
            MessageReceived?.Invoke("DISCONNECTED");
        }
    }

    private void DataReceived(object s, SerialDataReceivedEventArgs e)
    {
        buffer.Append(port.ReadExisting());

        while (buffer.ToString().Contains("#"))
        {
            int i = buffer.ToString().IndexOf("#");
            var msg = buffer.ToString().Substring(0, i).Trim().ToUpper();
            buffer.Remove(0, i + 1);

            if (msg == "ACK") ackEvent.Set();
            else if (msg == "PONG") pongEvent.Set();
            else MessageReceived?.Invoke(msg);
        }
    }

    public void SendWithAck(string cmd)
    {
        ackEvent.Reset();
        port.Write(cmd + "#");

        if (!ackEvent.WaitOne(2000))
            throw new Exception("ACK timeout");
    }

    public void Dispose()
    {
        heartbeat?.Dispose();
        if (port != null)
        {
            port.DataReceived -= DataReceived;
            port.Close();
        }
    }
}