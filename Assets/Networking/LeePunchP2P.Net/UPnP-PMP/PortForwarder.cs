using UnityEngine;
using Open.Nat;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Collections;

namespace LeePunchP2P.Net
{
    /// <summary>
    /// UPnP - PMP Port Forwarding Class
    /// </summary>
    public class PortForwarder : MonoBehaviour
    {

        #region Singleton

        private static PortForwarder _instance;
        public static PortForwarder Instance
        {
            get
            {
                if(_instance == null)
                {
                    GameObject go = new GameObject("PortForwarder");
                    _instance = go.AddComponent<PortForwarder>();

                    init = true;
                }

                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        #endregion

        #region Inspector Stuff

        [Tooltip("Lifetime of the port in seconds.")]
        [SerializeField]
        private int lifeTime = 10800;

        public bool logAll = false;
        public bool logging = true;
        public bool destroyIfFailed = false;

        #endregion

        #region Public Static Vars

        public static bool init = false;

        #endregion

        #region Public Static Methods

        public static void ReleaseInstance()
        {
            if (PortForwarder.init)
            {
                PortForwarder.init = false;
                Destroy(PortForwarder.Instance.gameObject);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port">internal and external port</param>
        public void ForwardPort(int port)
        {
            ForwardPort(port, port);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="internalPort">internal port</param>
        /// <param name="externalPort">external port</param>
        /// <param name="protocol">internet protocol</param>
        public void ForwardPort(int internalPort, int externalPort, Protocol protocol = Protocol.Udp)
        {
            this.internalPort = internalPort;
            this.externalPort = externalPort;
            StartCoroutine(ForwardPortCoroutine(protocol));
        }

        #endregion

        #region Private Vars

        private bool Found { get; set; }
        private IPAddress ip;
        private Mapping mapping;
        private int internalPort = 22876;
        private int externalPort = 22876;
        private float startTime;

        #endregion

        #region Unity Events

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else if (_instance != this)
                Destroy(gameObject);

            init = true;
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationQuit()
        {
            NatDiscoverer.ReleaseAll();
        }

        #endregion

        #region Private Methods

        private IEnumerator ForwardPortCoroutine(Protocol protocol = Protocol.Udp)
        {
            if (logging)
                Debug.Log("Port Forwarding has been started.");

            Task nat = ForwardPortTask(protocol);

            while (!nat.IsCompleted)
                yield return new WaitForSeconds(0.5f);

            string log = "";
        
            if (Found)
                log = "Port Forwarded !" + "\nIP: " + ip + "\n Internal Port : " + this.internalPort + "\n External Port : " + this.externalPort;
            else
                log = "Couldn't forward port.";

            log += "\nCompleted in " + (Time.time - startTime) + "s";

            if (logging)
            {
                Debug.Log(log);
                Debug.Log("Port Forwarding is finished.");
            }

            if (!Found && destroyIfFailed)
                Destroy(gameObject);
        }

        private async Task ForwardPortTask(Protocol protocol = Protocol.Udp)
        {
            // delete previous port mappings
            NatDiscoverer.ReleaseAll();

            startTime = Time.time;
            for (int i = 0; i < 10 && !Found; i++)
            {
                await ForwardPort(this.internalPort, this.externalPort, 150, protocol);
                await Task.Delay(60);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancelTime">Cancellation time in milliseconds</param>
        /// <returns></returns>
        private async Task ForwardPort(int internalPort, int externalPort, int cancelTime, Protocol protocol = Protocol.Udp)
        {
            var nat = new NatDiscoverer();
            var cts = new CancellationTokenSource();
            cts.CancelAfter(cancelTime);

            NatDevice device = null;
            var sb = new StringBuilder();
            IPAddress ip = null;

            await nat.DiscoverDeviceAsync(PortMapper.Upnp | PortMapper.Pmp, cts)
                .ContinueWith(task =>
                {
                    device = task.Result;
                    return device.GetExternalIPAsync();

                })
                .Unwrap()
                .ContinueWith(task =>
                {
                    ip = task.Result;
                    this.ip = ip;

                    sb.AppendFormat("\nYour IP: {0}", ip);
                    this.mapping = new Mapping(Protocol.Udp, internalPort, externalPort, lifeTime, "Game Server (Udp)");

                    return device.CreatePortMapAsync(mapping);
                })
                .Unwrap()
                .ContinueWith(task =>
                {
                    sb.AppendFormat("\nAdded mapping: {0}:{1} -> 127.0.0.1:{2}\n", ip, externalPort, internalPort);
                    sb.AppendFormat("\n+------+-------------------------------+--------------------------------+------------------------------------+-------------------------+");
                    sb.AppendFormat("\n| PORT | PUBLIC (Reacheable)   | PRIVATE (Your computer)  | Description      |       |");
                    sb.AppendFormat("\n+------+----------------------+--------+-----------------------+--------+------------------------------------+-------------------------+");
                    sb.AppendFormat("\n|  | IP Address   | Port | IP Address   | Port |         | Expires     |");
                    sb.AppendFormat("\n+------+----------------------+--------+-----------------------+--------+------------------------------------+-------------------------+");
                    return device.GetAllMappingsAsync();
                })
                .Unwrap()
                .ContinueWith(task =>
                {

                    foreach (var mapping in task.Result)
                    {
                        sb.AppendFormat("\n| {5} | {0,-20} | {1,6} | {2,-21} | {3,6} | {4,-35}|{6,25}|",
                    ip, mapping.PublicPort, mapping.PrivateIP, mapping.PrivatePort, mapping.Description,
                    mapping.Protocol == Protocol.Tcp ? "TCP" : "UDP", mapping.Expiration.ToLocalTime());
                    }

                    sb.AppendFormat("\n+------+----------------------+--------+-----------------------+--------+------------------------------------+-------------------------+");

                    Found = true;

                    sb.AppendFormat("\n[Done]");

                    if (logAll)
                        Debug.Log(sb.ToString());

                    return device.GetAllMappingsAsync();
                })
                .Unwrap()
                .ContinueWith(task =>
                {

                });
        }

        #endregion
    }
}