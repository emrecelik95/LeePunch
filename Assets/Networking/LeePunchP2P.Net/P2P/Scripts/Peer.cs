using System.Net;
using UnityEngine;

public abstract class Peer : MonoBehaviour, IPeer
{
    public abstract void Connect(string ip, ushort port);
    public abstract void Disconnect();
    public abstract ushort GetPort();
    public abstract void Host(ushort port);
    public abstract bool IsConnected();
    public abstract void OnHosted();
    public abstract void Send(byte[] dgram, int bytes, IPEndPoint endpoint);
}
