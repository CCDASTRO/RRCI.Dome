using System;
using System.Threading;

public class RoofSimulator
{
    public event Action<string> Message;

    public void Send(string cmd)
    {
        Message?.Invoke("ACK");

        if (cmd == "open")
            Simulate("OPENING", "OPEN");
        else if (cmd == "close")
            Simulate("CLOSING", "CLOSED");
        else if (cmd == "ping")
            Message?.Invoke("PONG");
    }

    private void Simulate(string start, string end)
    {
        Message?.Invoke(start);

        new Thread(() =>
        {
            System.Threading.Thread.Sleep(3000);
            Message?.Invoke(end);
        }).Start();
    }
}