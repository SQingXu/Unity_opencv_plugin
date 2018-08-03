#if UNITY_WSA_10_0 && !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#else
using System.Net.Sockets;
#endif

using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;

enum CommandType
{
    Stable = 0, Track = 1, SyncTrans = 2, Follow = 3
}

enum TransformType
{
    Camera = 0, Origin = 1
}

[Serializable]
public class Command
{
    public int command;
    public Command(int val)
    {
        command = val;
    }
}

[Serializable]
public class TransformNet
{
    public int type;
    public Vector3 position;
    public Quaternion rotation;
    public TransformNet(int t)
    {
        type = t;
        position = new Vector3();
        rotation = new Quaternion();
    }
}

public class CommandManager : MonoBehaviour {
    public bool syncTrans = true;
    public int holo_port = 8500;
    public int pc_port = 8500;
    private HoloPCClient client;
    private TransformNet cameraTransform;
    private TransformNet markerTransform;
    public GameObject marker;
    // Use this for initialization
    void Start () {
        cameraTransform = new TransformNet(0);
        markerTransform = new TransformNet(1);
        marker = GameObject.Find("marker");
        client = new HoloPCClient();
#if !UNITY_EDITOR
        client.Init(pc_port, holo_port);
        Debug.Log("Command client started");
#else
        client.Init(holo_port, pc_port);
#endif
        

    }
	
	// Update is called once per frame
	void LateUpdate () {
#if !UNITY_EDITOR
        //if (syncTrans)
        //{
        //    //Send Transformation of Camera and marker
        //    cameraTransform.position = Camera.main.transform.position;
        //    cameraTransform.rotation = Camera.main.transform.rotation;
        //    markerTransform.position = marker.transform.position;
        //    markerTransform.rotation = marker.transform.rotation;
        //    string camera_str = JsonUtility.ToJson(cameraTransform);
        //    string marker_str = JsonUtility.ToJson(markerTransform);
        //    client.sendMsg(camera_str);
        //    client.sendMsg(marker_str);
        //}

#else
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("stable the object");
            Command cmd = new Command((int)CommandType.Stable);
            client.sendMsg(cmd);
        }else if(Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("track the object");
            Command cmd = new Command((int)CommandType.Track);
            client.sendMsg(cmd);
        }else if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("sync the object");
            Command cmd = new Command((int)CommandType.SyncTrans);
            client.sendMsg(cmd);
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("follow the object");
            Command cmd = new Command((int)CommandType.Follow);
            client.sendMsg(cmd);
        }

        if (client.ReceiveCameraTrans)
        {
            Camera.main.transform.position = client.CameraTrans.position;
            Camera.main.transform.rotation = client.CameraTrans.rotation;
            client.ReceiveCameraTrans = false;
        }
        if (client.ReceiveMarkerTrans)
        {
            marker.transform.position = client.MarkerTrans.position;
            marker.transform.rotation = client.MarkerTrans.rotation;
            client.ReceiveMarkerTrans = false;
        }
#endif
    }


}

class HoloPCClient : MonoBehaviour
{
    GameObject origin = GameObject.Find("Origin");
    GameObject marker = GameObject.Find("marker");
    GameObject managers = GameObject.Find("Managers");
    TestDLL MarkerController;
    CommandManager cmdManager;
    OriginPosition origPos;


    public string holoip = "152.23.17.145";
    public string pcip = "152.2.142.24";
    public int send_port = 8500;
    public int receive_port = 8500;

    private bool _ReceiveCameraTrans = false;
    private bool _ReceiveMarkerTrans = false;
    private object lock_receive_camera = new object();
    private object lock_receive_marker = new object();
    private object lock_camera_trans = new object();
    private object lock_marker_trans = new object();
    private TransformNet _CameraTrans = new TransformNet(0);
    private TransformNet _MarkerTrans = new TransformNet(1);

    public bool ReceiveCameraTrans
    {
        get
        {
            lock (lock_receive_camera)
            {
                return _ReceiveCameraTrans;
            }
        }
        set
        {
            lock (lock_receive_camera)
            {
                _ReceiveCameraTrans = value;
            }
        }
    }

    public bool ReceiveMarkerTrans
    {
        get
        {
            lock (lock_receive_marker)
            {
                return _ReceiveMarkerTrans;
            }
        }
        set
        {
            lock (lock_receive_marker)
            {
                _ReceiveMarkerTrans = value;
            }
        }
    }

    public TransformNet CameraTrans
    {
        get
        {
            lock (lock_camera_trans)
            {
                return _CameraTrans;
            }
        }
        set
        {
            lock (lock_camera_trans)
            {
                _CameraTrans = value;
            }

        }
    }

    public TransformNet MarkerTrans
    {
        get
        {
            lock (lock_marker_trans)
            {
                return _MarkerTrans;
            }
        }
        set
        {
            lock (lock_marker_trans)
            {
                _MarkerTrans = value;
            }

        }
    }

#if !UNITY_EDITOR
    DatagramSocket socket;
    IOutputStream outstream;
    DataReader reader;
    DataWriter writer;
#else
    UdpClient udp;
#endif
    IPEndPoint ep;
    public void Init(int _port_s, int _port_r)
    {
        if(marker != null)
        {
            MarkerController = marker.GetComponent<TestDLL>();
        }
        if(origin != null)
        {
            origPos = origin.GetComponent<OriginPosition>();
        }
        
        cmdManager = managers.GetComponent<CommandManager>();

        this.send_port = _port_s;
        this.receive_port = _port_r;
#if UNITY_WSA_10_0 && !UNITY_EDITOR
        
        socket = new DatagramSocket();
        //receive from pc
        socket.MessageReceived += SocketOnMessageReceived;
        socket.BindServiceNameAsync(receive_port.ToString()).GetResults();
        //send to pc
        IPAddress ipAddress = IPAddress.Parse(pcip);
        ep = new IPEndPoint(ipAddress, send_port);
        outstream = socket.GetOutputStreamAsync(new HostName(ep.Address.ToString()), ep.Port.ToString()).GetResults();
        writer = new DataWriter(outstream);
#else
        //send to hololens
        IPAddress ipAddress = IPAddress.Parse(holoip);
        ep = new IPEndPoint(ipAddress, send_port);
        udp = new UdpClient(_port_r);
        //receive from hololens
        udp.BeginReceive(new AsyncCallback(receiveMsg), null);

#endif
    }

    //Send & Receive part
#if !UNITY_EDITOR

    public async void sendMsg(string message)
    {
        var data = Encoding.UTF8.GetBytes(message);
        if(writer != null)
        {
            writer.WriteBytes(data);
            await writer.StoreAsync();
            Debug.Log("sent " + data.Length);
        }


    }

    private async void SocketOnMessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
    {
        //Debug.Log("RECEIVED VOID");

        var result = args.GetDataStream();
        var resultStream = result.AsStreamForRead(1400);

        using (var reader = new StreamReader(resultStream))
        {
            var text = await reader.ReadToEndAsync();
            //var text = reader.ReadToEnd();
            Debug.Log("Command manager MESSAGE: " + text);

            handleMsg(text);

            
        }
    }
#else
    public void sendMsg(Command cmd)
    {
        string cmd_str = JsonUtility.ToJson(cmd);
        Byte[] sendBytes = Encoding.UTF8.GetBytes(cmd_str);
        try
        {
            int result = udp.Send(sendBytes, sendBytes.Length, ep);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }
    void receiveMsg(IAsyncResult result)
    {
        //Debug.Log("RECEIVING");
        IPEndPoint source = new IPEndPoint(0, 0);
        //byte[] message = udp.EndReceive(result, ref source);
        //Debug.Log("RECV " + Encoding.UTF8.GetString(message) + " from " + source);

        string message = Encoding.UTF8.GetString(udp.EndReceive(result, ref source));

        handleMsg(message);
        // schedule the next receive operation once reading is done:
        udp.BeginReceive(new AsyncCallback(receiveMsg), udp);
    }



#endif
    void handleMsg(string msg)
    {
#if !UNITY_EDITOR
        Command cmd = JsonUtility.FromJson<Command>(msg);
        if(cmd.command == (int)CommandType.Stable)
        {
            Debug.Log("receive stable command");
            //if(MarkerController != null)
            //{
            //    MarkerController.StableObject();
            //}
            if(origPos != null)
            {
                origPos.stable = true;
                origPos.anchor_change = true;
                //origPos.addAnchor();
            }
        }else if(cmd.command == (int)CommandType.Track)
        {
            Debug.Log("receive track command");
            if (MarkerController != null)
            {
                MarkerController.TrackObject();
            }
            
        }
        else if(cmd.command == (int)CommandType.SyncTrans)
        {
            Debug.Log("receive sync command");
            cmdManager.syncTrans = (cmdManager.syncTrans) ? false : true;
        }
        else if(cmd.command == (int)CommandType.Follow)
        {
            Debug.Log("receive follow command");
            if (origPos != null)
            {
                //origPos.removeAnchor();
                origPos.stable = false;
                origPos.anchor_change = true;
            }
        }
#else
        TransformNet tn = JsonUtility.FromJson<TransformNet>(msg);
        if(tn.type == (int)TransformType.Camera)
        {
            CameraTrans = tn;
            ReceiveCameraTrans = true;
        }else if(tn.type == (int)TransformType.Origin)
        {
            MarkerTrans = tn;
            ReceiveMarkerTrans = true;
        }
#endif
        return;
    }

    public void Stop()
    {
#if UNITY_WSA_10_0 && !UNITY_EDITOR

#else
            if (udp != null)
                udp.Close();
#endif
    }
}

