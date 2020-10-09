using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour
{

    [SerializeField]
    public GameObject cube;

    GameObject cube1;

    public UdpClient udp;

    List<Player> playersInGame = new List<Player>();

    bool populated = false;

    public string clientID;

    // Start is called before the first frame update
    void Start()
    {

        

        udp = new UdpClient();
        // 34.229.252.30
        udp.Connect("54.91.131.245", 12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
      
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);

        Byte[] sendBytes2 = Encoding.ASCII.GetBytes("spawn");
        udp.Send(sendBytes2, sendBytes2.Length);

        //cube1 = Instantiate(cube, new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0));

    }

    void OnDestroy(){
        udp.Dispose();
    }


    public enum commands{
        NEW_CLIENT,
        UPDATE,
        POPULATE,
        CLIENTID,
        DISCONNECT

    };
    
    [Serializable]
    public class Message{
        public commands cmd;
    }
    
    public class idMessage
    {
        public string id;
    }

    [Serializable]
    public class Player{
        [Serializable]
        public struct receivedColor{
            public float R;
            public float G;
            public float B;
        }

        [Serializable]
        public struct receivedPosition
        {
            public float x;
            public float y;
            public float z;
        }

        public string id;
        public float xCoord;
        public receivedColor color;
        public receivedPosition position;
        public GameObject gameCube = null;
        public int spawned;

        public int disconnected;

    }

    [Serializable]
    public class NewPlayer{
        
    }

    [Serializable]
    public class GameState{
        public Player[] players;
    }

    public Message latestMessage;
    public GameState latestGameState;
    public idMessage idm;

    void OnReceived(IAsyncResult result){
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        
        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        //Debug.Log("Got this: " + returnData);
        

        latestMessage = JsonUtility.FromJson<Message>(returnData);
        
        
      
        //float R  = message["color"]["R"]
        
        try{
            switch(latestMessage.cmd){
                case commands.NEW_CLIENT:

                    Player np = new Player();

                    np = JsonUtility.FromJson<Player>(returnData);

                    //playersInGame.Add(np);

                    break;
                case commands.UPDATE:
                    latestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.POPULATE:
                    if (populated == false)
                    {
                        Debug.Log("POPULATING");

                        latestGameState = JsonUtility.FromJson<GameState>(returnData);

                        foreach (Player p in latestGameState.players)
                        {
                            Player n = new Player();

                            n.id = p.id;
                            n.position = p.position;
                            n.spawned = 0;
                            n.color = p.color;

                            
                            
                            //playersInGame.Add(n);

                        }

                        populated = true;
                    }

                    break;
                case commands.CLIENTID:
                    Debug.Log("CLIENT ID ACQUIRED");

                    idm = JsonUtility.FromJson<idMessage>(returnData);

                    clientID = idm.id;

                    Debug.Log(clientID);


                    break;
                case commands.DISCONNECT:
                    Debug.Log("Client disconnected");

                    idm = JsonUtility.FromJson<idMessage>(returnData);

                    foreach (Player p in playersInGame)
                    {
                        Debug.Log(p.id);
                        Debug.Log(idm.id);

                        if (p.id == idm.id)
                        {
                            Debug.Log("SETTING DISCONNECT");
                            p.disconnected = 1;
                            
                        }
                    }

                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e){
            Debug.Log(e.ToString());
        }
        
        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers()
    {

        foreach (Player g in playersInGame)
        {
            if(g.spawned == 0)
            {
                g.position.x = transform.position.x;
                g.position.y = transform.position.y;
                g.position.z = transform.position.z;

                g.gameCube = Instantiate(cube, new Vector3(g.position.x, g.position.y, g.position.z), new Quaternion(0, 0, 0, 0));
                //g.gameCube.GetComponent<CubeControls>().id = g.id;
                g.spawned = 1;
            }
        }



    }

    void UpdatePlayers()
    {
        

        foreach (Player p in latestGameState.players)
        {
            bool inList = false;

            foreach (Player g in playersInGame)
            {
                if (g.id == p.id)
                {
                    g.gameCube.GetComponent<Renderer>().material.SetColor("_Color", new Color(p.color.R, p.color.G, p.color.B));
                    //g.gameCube.transform.position.Set(p.position.x + transform.position.x, p.position.y + transform.position.y, p.position.z + transform.position.z);
                    //g.gameCube.transform.position.Set(p.position.x, p.position.y, p.position.z);

                    if (g.id != clientID)
                    {
                        g.gameCube.transform.SetPositionAndRotation(new Vector3(p.position.x, p.position.y, p.position.z), new Quaternion(0, 0, 0, 0));
                    }
                    inList = true;
                }
                
            }

            if (inList == false)
            {
                Player n = new Player();

                n.id = p.id;
                n.position = p.position;
                n.spawned = 0;
                n.color = p.color;

                playersInGame.Add(n);

            }

        }
        
        //cube1.GetComponent<Renderer>().material.SetColor("_Color", new Color(latestGameState.players[0].color.R, latestGameState.players[0].color.G, latestGameState.players[0].color.B));
    }

    void DestroyPlayers()
    {
        foreach (Player g in playersInGame)
        {
            if (g.disconnected == 1)
            {
                DestroyImmediate(g.gameCube);
                playersInGame.Remove(g);
            }
        }
    }

    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

   [Serializable] 
    public struct move
    {
        public string moveName;
        public float x;
        public float y;
        public float z;
    }


    move left;
    move right;

    move newPos;

    void MovementInput()
    {
        foreach (Player g in playersInGame)
        {
            if (g.id == clientID)
            {
                if (g.gameCube)
                {
                    Vector3 movementVector = new Vector3(0, 0, 0);

                    if (Input.GetAxis("Horizontal") > 0)
                    {
                        movementVector.x = 0.1f;
                        //Debug.Log("Moving RIGHT");
                    }
                    else if (Input.GetAxis("Horizontal") < 0)
                    {
                        movementVector.x = -0.1f;
                        //Debug.Log("Moving LEFT");
                    }

                    if (Input.GetAxis("Vertical") > 0)
                    {
                        movementVector.y = 0.1f;
                        //Debug.Log("Moving UP");
                    }
                    else if (Input.GetAxis("Vertical") < 0)
                    {
                        movementVector.y = -0.1f;
                        //Debug.Log("Moving DOWN");
                    }

                    g.gameCube.transform.Translate(movementVector);

                    newPos.x = g.gameCube.transform.position.x;
                    newPos.y = g.gameCube.transform.position.y;
                    newPos.z = g.gameCube.transform.position.z;
                    newPos.moveName = "move";

                    string _pos = JsonUtility.ToJson(newPos);
                    Byte[] sendBytes = Encoding.ASCII.GetBytes(_pos);
                    udp.Send(sendBytes, sendBytes.Length);

                }
            }

        }
    

    }

    void Update()
    {
        MovementInput();
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();

    }
    
}
