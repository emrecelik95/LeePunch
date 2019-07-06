# LeePunch-P2P.Net
A light-weight tool to allow peer to peer connections using NAT traversal techniques in Unity.

### Features
- UPnP/PMP Port Forwarding (UDP/TCP)
- NAT Hole Punching (UDP)
- Master Server

### Dependencies 
- <a href = "https://github.com/lidgren/lidgren-network-gen3">Lidgren Network Library</a>
- <a href = "https://github.com/lontivero/Open.NAT">Open.NAT - UPnP/PMP Port Forwarding Library</a>

### Notes
- IPeer or Peer must be implemented using any network library.
- That Peer object must be assigned to <a href = "https://github.com/emrecelik95/LeePunch-P2P.Net/blob/master/Assets/Networking/LeePunchP2P.Net/P2P/Scripts/InitialNetwork.cs#L32">InitialNetwork.peer</a>. (assign with script or prefab inspector)
- <a href = "https://github.com/emrecelik95/LeePunch-P2P.Net/blob/master/Assets/Networking/LeePunchP2P.Net/P2P/Scripts/InitialNetwork.cs">Master Server</a> assigns a unique ID for each host and other clients use this ID to connect the hosted client.

## License (MIT)
Copyright (c) 2019 Emre Çelik

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
