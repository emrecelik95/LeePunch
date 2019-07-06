using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lidgren.Network;
using System;
using System.Net;
using LeePunchP2P.Net.MSCommon;

namespace LeePunchP2P.Net
{
    /// <summary>
    /// Used to communicate master server for registering and requesting host.
    /// </summary>
    public class ConnectionToMaster : MonoBehaviour
    {
        #region Singleton
        public static ConnectionToMaster Instance
        {
            get;
            private set;
        }
        #endregion

        #region PUBLIC STATIC VARS

        public static bool initMasterConn;
        public static IPEndPoint masterServer { get; private set; }

        #endregion

        #region PUBLIC VARS

        /// <summary>
        /// Possibly available master server ip adresses.
        /// Used in case master adress is unreachable.
        /// </summary>
        public List<string> masterIPs = new List<string>();

        [Space(5)]
        /// <summary>
        /// Master Server Port
        /// </summary>
        public ushort masterPort = 47856;

        public bool logAll = true;

        /// <summary>
        /// The last requested or registered host id.
        /// Initializes when using 'RegisterToMaster' or 'RequestHost' or 'RequestHostNatHolePunch' methods.
        /// </summary>
        public int HostID { get; private set; } = -1;


        public IPEndPoint externalEP { get; private set; }

        #endregion

        #region PRIVATE VARS

        private NetPeer client;

        private Coroutine initCoroutine;

        private Action onInit;
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

            onInit = null;
            if(!initMasterConn)
                StartCoroutine(InitMasterEndPoint());

            StartCoroutine(ReadIncomingsForClient());
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        #endregion

        #region PUBLIC NETWORK EVENTS

        /// <summary>
        /// Invoked when a client hosted and returns host ID.
        /// Parameter : Host ID, my external ip end point.
        /// </summary>
        public Action<int> OnRegisterHost;

        /// <summary>
        /// Invoked when a client request is completed and returns host's internal and external endpoints.
        /// Parameters : Internal and External IPEndPoints of the host.
        /// </summary>
        public Action<IPEndPoint, IPEndPoint> OnRequestHost;

        /// <summary>
        /// Invoked when nat hole request is received.
        /// </summary>
        public Action<IPEndPoint, IPEndPoint, string> OnRequestHolePunch;

        /// <summary>
        /// Invoked when external ep recevied as 'RequestExternalEP' callback.
        /// </summary>
        public Action<IPEndPoint> OnRequestExternalEP;

        #endregion

        #region PRIVATE METHODS

        private void InitClient()
        {
            NetPeerConfiguration config;

            config = new NetPeerConfiguration("MSClient");
            config.EnableMessageType(NetIncomingMessageType.UnconnectedData);
            config.ConnectionTimeout = 36000000.0f;
            
            client?.Shutdown("restart");

            client = new NetClient(config);
            client.Start();
        }

        /// <summary>
        /// Initialize some basic configurations.
        /// Create client.
        /// </summary>
        private IEnumerator InitAll()
        {
            InitClient();

            if (!initMasterConn)
                yield return StartCoroutine(InitMasterEndPoint());

            RequestExternalEP(OnRequestExternalEP: (IPEndPoint ep) =>
            {
                this.externalEP = ep;
                Debug.Log("ConnectionToMaster Client External : " + this.externalEP);

                onInit?.Invoke();
            });
        }

        private IEnumerator InitMasterEndPoint()
        {
            float tStart;
            float tPass;
            float timeOut = 1.0f;
            while (masterServer == null)
            {
                Ping ping;
                foreach (var ip in masterIPs)
                {
                    tStart = Time.time;
                    tPass = 0;

                    ping = new Ping(ip);

                    while (!ping.isDone && tPass < timeOut)
                    {
                        yield return new WaitForSeconds(0.15f);
                        tPass = Time.time - tStart;
                    }

                    if (tPass >= timeOut || ping.time == -1)
                    {
                        Debug.Log("Ping timed out (" + tPass + "s)" + " for master : " + ip);
                        // timedout
                    }
                    else
                    {
                        masterServer = NetUtility.Resolve(ip, masterPort);
                        Debug.Log("Resolved Master : " + ip + " , Ping : " + ping.time + "ms");
                        initMasterConn = true;
                        yield break;
                    }
                    yield return null;
                }
            }

        }

        private IEnumerator GetExternalEP(IPEndPoint sendEP = null)
        {
            while (masterServer == null)
                yield return new WaitForSeconds(0.1f);

            NetOutgoingMessage msg = client.CreateMessage();
            msg.Write((byte)MasterServerMessageType.RequestExternalEP);
            msg.Write(sendEP != null); // return end point
            if (sendEP != null)
                msg.Write(sendEP);

            client.SendUnconnectedMessage(msg, masterServer);
        }

        public IEnumerator ReadIncomingsForClient()
        {
            NetIncomingMessage inc;
            while (true)
            {
                while (client != null && (inc = client.ReadMessage()) != null)
                {
                    try
                    {
                        switch (inc.MessageType)
                        {
                            case NetIncomingMessageType.VerboseDebugMessage:
                            case NetIncomingMessageType.DebugMessage:
                            case NetIncomingMessageType.WarningMessage:
                            case NetIncomingMessageType.ErrorMessage:
                                // Debug.Log(inc.ReadString());
                                break;
                            case NetIncomingMessageType.UnconnectedData:
                                if (inc.SenderEndPoint.Equals(masterServer))
                                {
                                    switch ((MasterServerMessageType)(inc.ReadByte()))
                                    {
                                        case MasterServerMessageType.RequestExternalEP:
                                            {
                                                OnRequestExternalEP?.Invoke(inc.ReadIPEndPoint());
                                                break;
                                            }
                                            
                                        case MasterServerMessageType.RegisterHost:
                                            {
                                                int id = inc.ReadInt32(); // my unique host id
                                                this.HostID = id;
                                                if (logAll)
                                                    Debug.Log("Received from MS ::: " + MasterServerMessageType.RegisterHost.ToString()
                                                            + "\nRegistered to MS , Host ID : " + id);

                                                OnRegisterHost?.Invoke(id);
                                                break;
                                            }

                                        case MasterServerMessageType.RequestHost:
                                            {
                                                IPEndPoint internalIP = inc.ReadIPEndPoint(); // internal
                                                IPEndPoint externalIP = inc.ReadIPEndPoint(); // external

                                                if (logAll)
                                                    Debug.Log("Received from MS ::: " + MasterServerMessageType.RequestHost.ToString()
                                                            + "\nGot requested host \ninternal : " + internalIP + "\nexternal : " + externalIP);

                                                OnRequestHost?.Invoke(internalIP, externalIP);
                                                break;
                                            }
                                            
                                        case MasterServerMessageType.RequestNatHolePunch:
                                            {
                                                IPEndPoint peerInternal = inc.ReadIPEndPoint();
                                                IPEndPoint peerExternal = inc.ReadIPEndPoint();
                                                string token = inc.ReadString();

                                                Debug.Log("Received from MS ::: " + MasterServerMessageType.RequestNatHolePunch.ToString()
                                                        + "\n peerInternal : " + peerInternal
                                                        + "\n peerExternal : " + peerExternal
                                                        + "\n token : " + token);

                                                OnRequestHolePunch?.Invoke(peerInternal, peerExternal, token);
                                                break;
                                            }
                                            
                                    }
                                }
                                break;
                            case NetIncomingMessageType.NatIntroductionSuccess:
                                {
                                    string token = inc.ReadString();
                                    Debug.Log("Nat introduction success to " + inc.SenderEndPoint + " token is: " + token);

                                    break;
                                }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Log("Exception(on master server response) : " + e.ToString());
                    }
                }

                yield return new WaitForSeconds(0.2f);
            }
        }

        #endregion

        #region PUBLIC INTERFACE

        public void Shutdown()
        {
            client?.Shutdown("bye");

            onInit = null;

            OnRegisterHost = null;
            OnRequestExternalEP = null;
            OnRequestHolePunch = null;
            OnRequestHost = null;
        }

        public void StartClient(Action onStarted)
        {
            if (initCoroutine != null)
                StopCoroutine(initCoroutine);

            onInit = onStarted;
            initCoroutine = StartCoroutine(InitAll());
        }

        public void RequestExternalEP(IPEndPoint sendEP = null, Action<IPEndPoint> OnRequestExternalEP = null)
        {
            if(OnRequestExternalEP != null)
                this.OnRequestExternalEP = OnRequestExternalEP;

            StartCoroutine(GetExternalEP(sendEP));
        }

        public void RegisterToMaster(IPEndPoint internalHost, IPEndPoint externalHost, string password, Action<int> OnRegisterHost = null)
        {
            if (OnRegisterHost != null)
                this.OnRegisterHost = OnRegisterHost;

            NetOutgoingMessage msg = client.CreateMessage();
            msg.Write((byte)MasterServerMessageType.RegisterHost);
            msg.Write(password);
            msg.Write(internalHost);
            msg.Write(externalHost);

            if (logAll)
            {
                Debug.Log("Sending to MS ::: " + MasterServerMessageType.RegisterHost.ToString()
                        + "\n password : " + (password.Equals(string.Empty)? "(no)" : password) 
                        + " , internal : " + internalHost
                        + " , external : " + externalHost);
            }
            client.SendUnconnectedMessage(msg, masterServer);
        }

        public void RequestHost(int hostID, string password, Action<IPEndPoint, IPEndPoint> OnRequestHost = null)
        {
            if (OnRequestHost != null)
                this.OnRequestHost = OnRequestHost;

            this.HostID = hostID;

            NetOutgoingMessage msg = client.CreateMessage();
            msg.Write((byte)MasterServerMessageType.RequestHost);
            msg.Write(hostID);
            msg.Write(password);

            

            if (logAll)
            {
                Debug.Log("Sending to MS ::: " + MasterServerMessageType.RequestHost.ToString()
                        + "\nhostID : " + hostID + " , password : " + password);
            }

            client.SendUnconnectedMessage(msg, masterServer);
        }

        public void RequestHostNatHolePunch(int hostID, string password, string token, IPEndPoint clientInternal, 
            IPEndPoint clientExternal, Action<IPEndPoint, IPEndPoint, string> OnRequestHolePunch = null)
        {
            if (OnRequestHolePunch != null)
                this.OnRequestHolePunch = OnRequestHolePunch;

            this.HostID = hostID;

            NetOutgoingMessage msg = client.CreateMessage();
            msg.Write((byte)MasterServerMessageType.RequestNatHolePunch);
            msg.Write(hostID);
            msg.Write(password);
            msg.Write(token);
            msg.Write(clientInternal);
            msg.Write(clientExternal);

            Debug.Log("Sending to MS ::: " + MasterServerMessageType.RequestNatHolePunch.ToString()
                        + "\nhostID : " + hostID + " , password : " + password);

            client.SendUnconnectedMessage(msg, masterServer);
        }

        #endregion
    }
}