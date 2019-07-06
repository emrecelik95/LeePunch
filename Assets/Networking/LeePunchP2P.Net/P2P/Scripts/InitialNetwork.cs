using Lidgren.Network;
using LeePunchP2P.Net.MSCommon;
using System;
using System.Collections;
using System.Net;
using UnityEngine;

namespace LeePunchP2P.Net
{
    /// <summary>
    /// 'peer' must be assigned either from inspector or from code.
    /// The most useful api' s ----> (HostAndRegisterToMaster, ConnectUsingMaster, Disconnect).
    /// </summary>
    /// 
    public class InitialNetwork : MonoBehaviour
    {
        #region Singleton

        public static InitialNetwork Instance
        {
            get;
            private set;
        }

        #endregion

        #region INSPECTOR STUFF

        [Header("Network Settings")]

        [Tooltip("Must not be null, used for creating initial connection.")]
        public Peer peer;

        [SerializeField]
        private ushort HostPort = 23345;

        public bool usePortForwarding = true;

        #endregion

        #region PRIVATE VARS
        
        private string localIP;

        #endregion

        #region PUBLIC VARS

        public IPEndPoint internalEP { get; private set; }
        public IPEndPoint externalEP { get; private set; }

        #endregion

        #region UNITY EVENTS

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(transform.root.gameObject);

            this.localIP = LocalNetworkUtils.GetLocalIPAddress();
            Debug.Log("My Local IP : " + this.localIP);

        }

        private void Start()
        {
            ConnectionToMaster.Instance.OnRequestHolePunch += HandleNatHolePunch;
        }

        private void OnDestroy()
        {
            Disconnect(true);
        }

        private void OnDisable()
        {
            Disconnect();
        }

        #endregion

        #region PUBLIC METHODS

        // *************** The Most Important API' s *************** //

        public void HostAndRegisterToMaster(string password)
        {
            if (!ConnectionToMaster.initMasterConn || peer == null)
                return;

            Host();

            PortForwarder.ReleaseInstance();

            this.localIP = LocalNetworkUtils.GetLocalIPAddress();
            this.internalEP = new IPEndPoint(IPAddress.Parse(this.localIP), peer.GetPort());

            Debug.Log("Internal EP : " + this.internalEP);

            ConnectionToMaster.Instance.StartClient(() =>
            {
                RequestExternalEP(OnRequestExternalEP: (ep) =>
               {
                   this.externalEP = ep;
                   Debug.Log("Network Peer External : " + this.externalEP);

                   ConnectionToMaster.Instance.RegisterToMaster(this.internalEP, this.externalEP, password,
                       (hostID) =>
                       {
                           peer.OnHosted();
                           // try to forward port in case hole punching does not work
                           if (usePortForwarding)
                                   PortForwarder.Instance.ForwardPort(internalEP.Port, externalEP.Port);
                       });
               });
            });
        }

        public void ConnectUsingMaster(int hostID, string password)
        {
            if (!ConnectionToMaster.initMasterConn || peer == null)
                return;

            ConnectionToMaster.Instance.StartClient(() =>
            {
                ConnectionToMaster.Instance.RequestHost(hostID, password,
                (internalEP, externalEP) =>
                {
                    if (ConnectionToMaster.Instance.externalEP.Address.Equals(externalEP.Address))
                    {
                        Connect(internalEP.Address.ToString(), (ushort)internalEP.Port);
                        StartCoroutine(ConnectIFInternalFails(hostID, password, internalEP, externalEP)); 
                }
                    else
                    {
                        ConnectUsingGlobalEP(hostID, password, externalEP);
                    }
                });
            });
        }

        public void Disconnect(bool onDestroy = false)
        {
            peer?.Disconnect();

            if (!onDestroy)
                PortForwarder.ReleaseInstance();
        }

        // ********************************************************* //

        public void ConnectUsingGlobalEP(int hostID, string password, IPEndPoint externalEP)
        {
            Debug.Log("Connecting using global ip end");
            Connect(externalEP);

            // NAT HOLE PUNCHING
            this.internalEP = new IPEndPoint(IPAddress.Parse(this.localIP), peer.GetPort());
            Debug.Log("Internal EP : " + this.internalEP);

            RequestExternalEP(OnRequestExternalEP: (ep) =>
            {
                this.externalEP = ep;
                Debug.Log("Network Peer External : " + this.externalEP);

                ConnectionToMaster.Instance.RequestHostNatHolePunch(hostID, password, "token", this.internalEP, this.externalEP);
            });
        }

        #endregion

        #region PRIVATE METHODS

        private void Connect(IPEndPoint ep)
        {
            Connect(ep.Address.ToString(), (ushort)ep.Port);
        }

        private void Connect(string ip, ushort port)
        {
            peer?.Disconnect();
            peer.Connect(ip, port);
        }

        private void Host()
        {
            bool bind = false;
            ushort trial = 0; 
            while (!bind)
            {
                trial++;

                if (trial == 65535)
                {
                    Debug.Log("Cannot host...");
                    return;
                }

                try
                {
                    peer?.Disconnect();
                    Host(HostPort);
                    bind = true;
                }
                catch
                {
                    HostPort++;
                    if (HostPort > 65535)
                        HostPort = 0;
                }
            }
        }

        private void Host(ushort port)
        {
            peer.Host(port);
        }

        private void RequestExternalEP(Action<IPEndPoint> OnRequestExternalEP)
        {
            ConnectionToMaster.Instance.OnRequestExternalEP = OnRequestExternalEP;

            NetOutgoingMessage msg = new NetOutgoingMessage();
            msg.Write((byte)MasterServerMessageType.RequestExternalEP);
            msg.Write(true); // send ep to someone
            msg.Write(ConnectionToMaster.Instance.externalEP); // (masterconnection client in this case)

            Debug.Log("Requesting networker external. | masterclient external : " + ConnectionToMaster.Instance.externalEP);

            SendOutMessage(msg, ConnectionToMaster.masterServer);
        }

        private bool SendOutMessage(NetOutgoingMessage msg, IPEndPoint endPoint)
        {
            byte[] data = new byte[msg.GetEncodedSize()];
            int len = msg.Encode(data, 0, 0);

            try
            {
                peer.Send(data, data.Length, endPoint);
            }
            catch (Exception e)
            {
                Debug.LogError("Cannot send message to MS. \n" + e.ToString());
                // if udp client has a connection socket, than forget it
                return false;
            }

            return true;
        }

        private void HandleNatHolePunch(IPEndPoint peerInternal, IPEndPoint peerExternal, string token)
        {
            // send some packets to internal and external end point for punching nat

            peer.Send(new byte[] { 0 }, 1, peerInternal);
            peer.Send(new byte[] { 0 }, 1, peerExternal);

            peer.Send(new byte[] { 0 }, 1, peerInternal);
            peer.Send(new byte[] { 0 }, 1, peerExternal);

            peer.Send(new byte[] { 0 }, 1, peerInternal);
            peer.Send(new byte[] { 0 }, 1, peerExternal);

            Debug.Log("Punching a hole in peer nat, peerInternal : " + peerInternal + " , peerExternal : " + peerExternal);
        }

        private IEnumerator ConnectIFInternalFails(int hostID, string password, IPEndPoint internalEP, IPEndPoint externalEP)
        {
            // internal nat punchthrough
            for (float i = 0; i < 4.0f; i += 0.1f)
            {
                yield return new WaitForSeconds(0.1f);
                if (peer.IsConnected())
                {
                    yield break;
                }
            }

            peer.Disconnect();

            Connect(internalEP);
            RequestExternalEP(OnRequestExternalEP: (ep) =>
            {
                this.internalEP = new IPEndPoint(IPAddress.Parse(this.localIP), peer.GetPort());
                this.externalEP = ep;
                Debug.Log("Network Peer External : " + this.externalEP);

                ConnectionToMaster.Instance.RequestHostNatHolePunch(hostID, password, "token", this.internalEP, this.externalEP);
            });
        }

        #endregion
    }
}