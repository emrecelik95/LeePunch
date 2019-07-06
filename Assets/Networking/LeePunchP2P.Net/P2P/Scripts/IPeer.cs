using System.Net;

public interface IPeer
{
    /// <summary>
    /// Send udp datagram to endpoint.
    /// </summary>
    /// <param name="dgram">Datagram to be sent</param>
    /// <param name="bytes">byte size</param>
    /// <param name="endpoint">end point to sent</param>
    void Send(byte[] dgram, int bytes, IPEndPoint endpoint);

    /// <summary>
    /// Connect to a host using UDP.
    /// </summary>
    /// <param name="ip">host ip</param>
    /// <param name="port">host port</param>
    void Connect(string ip, ushort port);

    /// <summary>
    /// Disconnect this peer.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Become host using UDP.
    /// </summary>
    /// <param name="port">port to be established</param>
    void Host(ushort port);

    /// <summary>
    /// Called automatically when hosted and registered completed.
    /// </summary>
    void OnHosted();

    /// <summary>
    /// Returns the port number of socket.
    /// </summary>
    /// <returns></returns>
    ushort GetPort();

    /// <summary>
    /// Returns whether socket is connected.
    /// </summary>
    /// <returns></returns>
    bool IsConnected();
}
