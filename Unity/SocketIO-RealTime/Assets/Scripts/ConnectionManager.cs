using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketIO;
using UnityEngine.UI;

[RequireComponent(typeof(SocketIOComponent))]
public class ConnectionManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerIDGroup
    {
        public List<string> playerIDList = new List<string>();
    }

    [System.Serializable]
    public class RoomIDGroup
    {
        public List<string> roomIDList = new List<string>();
    }

    public class PlayerData
    {
        public string uid;
        public Player playerObj;
        public Bullet playerBullet;
        public Vector3 correctPos;
        public Vector3 correctRo;
        public Vector3 currentBulletPos;
    }
    [System.Serializable]
    public class ShootJson
    {
        public string name;

        public static ShootJson CreateFromJson(string data)
        {
            return JsonUtility.FromJson<ShootJson>(data);
        }
    }

    [System.Serializable]
    public class PlayerUpdateData
    {
        public float x, y, z;
        public float R_x,R_y,R_z;
        public float B_x, B_y, B_z;
    }

    public enum ConnectionState
    {
        Disconnected,
        Connected,
        RoleCreate,
        RoleJoin,
        InRoom,
    }



    public ConnectionState connectionState;

    public Player playerObjPref;

    public string ownerID;

    public PlayerIDGroup playerIDGroup;

    public PlayerIDGroup cachePlayerIDGroup;

    public RoomIDGroup roomIDGroup;

    private List<PlayerData> characterList = new List<PlayerData>();

    private PlayerData playerDataOwner;

    private SocketIOComponent socket;

    public string roomName;

    private bool isRoom;

    [SerializeField] Camera cam;
    [SerializeField] Vector3 offset;
    [SerializeField] float smoothSpeed = 0.125f;
    [SerializeField] Bullet bulletpref;
    [SerializeField] GameObject winText;
    [SerializeField] GameObject loseText;
    //[SerializeField] GameObject gunPoint;
    Vector3 velocity = Vector3.zero;
    //public BulletData playerBulletData;
    public Bullet playerBulletPref;
    public bool hit = false;
    private void OnGUI()
    {
        switch(connectionState)
        {
            case ConnectionState.Disconnected: 
            {
                if (GUILayout.Button("Connect"))
                {
                    socket.Connect();
                }
                   
                if(socket.IsConnected)
                {
                    connectionState = ConnectionState.Connected;
                }
            
                break;
            }

            case ConnectionState.Connected:
            {
                if(GUILayout.Button("CreateRoom"))
                {
                    connectionState = ConnectionState.RoleCreate;
                }

                if(GUILayout.Button("JoinRoom"))
                {
                    connectionState = ConnectionState.RoleJoin;
                    socket.Emit("OnClientFetchRoomList");
                }
                break;
            }

            case ConnectionState.RoleCreate:
            {
                roomName = GUILayout.TextField(roomName);
                if(GUILayout.Button("CreateRoom"))
                {
                    CreateRoom(roomName);
                }
                break;
            }

            case ConnectionState.RoleJoin:
            {
                foreach(var _roomName in roomIDGroup.roomIDList)
                {
                    if(GUILayout.Button(_roomName))
                    {
                        roomName = _roomName;
                        JoinRoom(_roomName);
                    }
                }
                break;
            }

            case ConnectionState.InRoom:
            {
                GUILayout.TextField(ownerID);
                if(GUILayout.Button("LeaveRoom"))
                {
                    LeaveRoom();
                }
                break;
            }
        }
    }

   

    // Start is called before the first frame update
    void Start()
    {
        

        socket = GetComponent<SocketIOComponent>();

        socket.On("OnOwnerClientConnect", OnOwnerClientConnect);
        socket.On("OnClientConnect", OnClientConnect);
        socket.On("OnClientFetchPlayerList", OnClientFetchPlayerList);
        socket.On("OnClientDisconnect", OnClientDisconnect);

        socket.On("OnClientCreateRoomSuccess", OnClientCreateRoomSuccess);
        socket.On("OnClientCreateRoomFail", OnClientCreateRoomFail);
        socket.On("OnOwnerClientJoinRoomSuccess", OnOwnerClientJoinRoomSuccess);
        socket.On("OnClientJoinRoomSuccess", OnClientJoinRoomSuccess);
        socket.On("OnClientJoinRoomFail", OnClientJoinRoomFail);

        socket.On("OnClientLeaveRoom", OnClientLeaveRoom);

        socket.On("OnClientFetchRoomList", OnClientFetchRoomList);

        socket.On("OnClientUpdateMoveList", OnClientUpdateMoveList);
        socket.On("Fire", Fire);
        socket.On("Win", Win);
        socket.On("Lose", Lose);

        cachePlayerIDGroup = new PlayerIDGroup();
    }

    void Fire(SocketIOEvent evt)
    {
        string data = evt.data.ToString();
        Debug.Log("data "+data);

        

        ShootJson shootJson = ShootJson.CreateFromJson(data);
        GameObject p = GameObject.Find(shootJson.name);
        Debug.Log(shootJson.name);
        Transform spawn = p.gameObject.transform.GetChild(0);
        //Debug.Log(spawn.transform.position);
        
        Instantiate(playerDataOwner.playerBullet, spawn.transform.position, spawn.transform.rotation);
    }

    // Update is called once per frame
    void Update()
    {
        
        DetectPlayerConnect();
        UpdateAllCharacter();
        WinCheck();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("name", ownerID);
            JSONObject jsonObj = new JSONObject(data);
            socket.Emit("Fire",jsonObj);
           
        }
        if(hit)
        {
            if (FindObjectOfType<Bullet>().hitName == ownerID)
            {
                FindObjectOfType<Player>().TakeDamage(1);
            }
            hit = false;
        }
       


    }
    

    private void FixedUpdate()
    {
        if (playerDataOwner.uid == ownerID)
        {
            Vector3 desPos = playerDataOwner.correctPos + offset;
            Vector3 smoothedPos = Vector3.SmoothDamp(cam.transform.position, desPos, ref velocity, smoothSpeed);
            cam.transform.position = smoothedPos;

           // cam.transform.position = new Vector3(playerDataOwner.correctPos.x, playerDataOwner.correctPos.y,-10f);
        }
    }

   

    void UpdateAllCharacter()
    {
        for(int i = 0; i < characterList.Count; i++)
        {
            if (characterList[i].uid == ownerID)
                continue;

            Vector3 currentPos = characterList[i].playerObj.transform.position;
            currentPos = Vector3.Lerp(currentPos, characterList[i].correctPos, 5.0f * Time.deltaTime);

            Vector3 currentRotation = characterList[i].playerObj.transform.rotation.eulerAngles;
            currentRotation = Vector3.Lerp(currentRotation, characterList[i].correctRo, 5.0f * Time.deltaTime);

            /* Vector3 currentBulletPos = playerBulletData.playerBulletOBJ.transform.position;
             currentBulletPos = Vector3.Lerp(currentBulletPos, playerBulletData.bulletCurrectPos, 5.0f * Time.deltaTime);*/

            //Vector3 currentBullet = characterList[i].playerBullet.transform.position;
            //currentBullet = Vector3.Lerp(currentBullet, characterList[i].currentBulletPos, 5.0f * Time.deltaTime);
           

            characterList[i].playerObj.transform.position = currentPos;
            characterList[i].playerObj.transform.rotation = Quaternion.Euler(currentRotation);
            //characterList[i].playerBullet.transform.position = currentBullet;
            

            //playerBulletData.playerBulletOBJ.transform.position = currentBulletPos;

        }
    }

    IEnumerator UpdateOwnerPlayerData()
    {
        while(connectionState == ConnectionState.InRoom)
        {
            if(playerDataOwner != null && playerDataOwner.playerObj != null)
            {
                Dictionary<string, string> data = new Dictionary<string, string>();

                Vector3 playerPos = playerDataOwner.playerObj.transform.position;
                Vector3 playerRotation = playerDataOwner.playerObj.transform.rotation.eulerAngles;
               // Vector3 bulletPos = playerDataOwner.playerBullet.transform.position;

                //float playerRotation = playerDataOwner.playerObj.transform.rotation.z;
                data.Add("roomName", roomName);
                data.Add("uid", ownerID);
                data.Add("x", playerPos.x.ToString());
                data.Add("y", playerPos.y.ToString());
                data.Add("z", playerPos.z.ToString());
                data.Add("R_x", playerRotation.x.ToString());
                data.Add("R_y", playerRotation.y.ToString());
                data.Add("R_z", playerRotation.z.ToString());

                
                // data.Add("B_x", bulletPos.x.ToString());
                //data.Add("B_y", bulletPos.y.ToString());
                // data.Add("B_z", bulletPos.z.ToString());


                JSONObject jsonObj = new JSONObject(data);

                socket.Emit("OnClientUpdateMove", jsonObj);

                yield return new WaitForSeconds(0.1f);
            }

            yield return null;
        }
    }

    private void DetectPlayerConnect()
    {
        if(cachePlayerIDGroup.playerIDList.Count != playerIDGroup.playerIDList.Count)
        {
            bool checkConnect;
            List<string> firstList;
            List<string> secondList;

            if(playerIDGroup.playerIDList.Count > cachePlayerIDGroup.playerIDList.Count)
            {
                firstList = playerIDGroup.playerIDList;
                secondList = cachePlayerIDGroup.playerIDList;
                checkConnect = true;
            }
            else
            {
                firstList = cachePlayerIDGroup.playerIDList;
                secondList = playerIDGroup.playerIDList;
                checkConnect = false;
            }

            foreach(var fID in firstList)
            {
                bool isFound = false;
                foreach(var sID in secondList)
                {
                    if(fID == sID)
                    {
                        isFound = true;
                        break;
                    }
                }

                if(!isFound)
                {
                    if(checkConnect)//Check player connect
                    {
                        //Debug.Log("Player connected : " + fID);
                        CreateCharacter(fID);
                    }
                    else//Check player disconnect
                    {
                        //Debug.Log("Player disconnected : " + fID);
                        DestroyCharacter(fID);
                    }
                }
            }
        }

        cachePlayerIDGroup.playerIDList = playerIDGroup.playerIDList;
    }

  

    private void CreateCharacter(string uid)
    {
        
        PlayerData newPlayerData = new PlayerData();

        newPlayerData.uid = uid;
        newPlayerData.playerObj = Instantiate(playerObjPref, Vector3.zero, Quaternion.identity);
        newPlayerData.playerBullet = Instantiate(playerBulletPref, Vector3.zero, Quaternion.identity);

        //newPlayerData.playerObj.name = "Player : " + uid;
        newPlayerData.playerObj.name = uid;
        if (uid == ownerID)
        {
            newPlayerData.playerObj.canControl = true;
            playerDataOwner = newPlayerData;
            
            //cam.transform.position = new Vector3(newPlayerData.playerObj.transform.position.x, newPlayerData.playerObj.transform.position.y+10f, newPlayerData.playerObj.transform.position.z);

        }

        characterList.Add(newPlayerData);
    }

    private void DestroyCharacter(string uid)
    {
        
        for (int i = 0; i < characterList.Count; i++)
        {
            if(characterList[i].uid == uid)
            {
                Destroy(characterList[i].playerObj.gameObject);
                characterList.RemoveRange(i, 1);
                break;
            }
        }
    }

    public void CreateRoom(string newRoomName)
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add("roomName", newRoomName);
        JSONObject jsonObj = new JSONObject(data);
 
        socket.Emit("OnClientCreateRoom", jsonObj);
    }

    public void JoinRoom(string newRoomName)
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add("roomName", newRoomName);
        JSONObject jsonObj = new JSONObject(data);

        socket.Emit("OnClientJoinRoom", jsonObj);
        
    }

    public void LeaveRoom()
    {
        connectionState = ConnectionState.Connected;
        roomName = "";
        socket.Emit("OnClientLeaveRoom");
    }

    private void FetchPlayerList()
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add("roomName", roomName);
        JSONObject jsonObj = new JSONObject(data);
        socket.Emit("OnClientFetchPlayerList", jsonObj);
    }

    public void WinCheck()
    {
        if(FindObjectOfType<Player>().die)
        {
            Debug.Log("Die");
            socket.Emit("WinCheck");
        }
        
       
    }

    IEnumerator WaitForDisconnect()
    {
        Time.timeScale = 0;
        yield return new WaitForSeconds(5f);
        Time.timeScale = 1;
        LeaveRoom();
    }




    #region Callback Group
    void OnClientConnect(SocketIOEvent evt)
    {
        Debug.Log("OnClientConnect : "+ evt.data.ToString());
        //socket.Emit("OnClientFetchPlayerList");
    }

    void OnClientDisconnect(SocketIOEvent evt)
    {
        Debug.Log("OnClientDisconnect : " + evt.data.ToString());
        //socket.Emit("OnClientFetchPlayerList");
    }

    void OnOwnerClientConnect(SocketIOEvent evt)
    {
        Debug.Log("OnOwnerClientConnect : " + evt.data.ToString());
    }

    void OnClientFetchPlayerList(SocketIOEvent evt)
    {
        Debug.Log("OnClientFetchPlayerList : "+ evt.data.ToString());

        playerIDGroup = JsonUtility.FromJson <PlayerIDGroup> (evt.data.ToString());
    }

    //======================== Room ===========================
    void OnClientCreateRoomSuccess(SocketIOEvent evt)
    {
        Debug.Log("OnClientCreateRoomSuccess : " + evt.data.ToString());

        connectionState = ConnectionState.InRoom;

        var dictData = evt.data.ToDictionary();

        ownerID = dictData["uid"];

        StartCoroutine(UpdateOwnerPlayerData());

        FetchPlayerList();
    }

    void OnClientCreateRoomFail(SocketIOEvent evt)
    {
        Debug.Log("OnClientCreateRoomFail : " + evt.data.ToString());
    }

    void OnOwnerClientJoinRoomSuccess(SocketIOEvent evt)
    {
        Debug.Log("OnOwnerClientJoinRoomSuccess : " + evt.data.ToString());

        connectionState = ConnectionState.InRoom;

        var dictData = evt.data.ToDictionary();
        
        ownerID = dictData["uid"];

        StartCoroutine(UpdateOwnerPlayerData());

        FetchPlayerList();
    }

    void OnClientJoinRoomSuccess(SocketIOEvent evt)
    {
        Debug.Log("OnClientJoinRoomSuccess : " + evt.data.ToString());

        FetchPlayerList();
    }

    void OnClientJoinRoomFail(SocketIOEvent evt)
    {
        Debug.Log("OnClientJoinRoomFail : " + evt.data.ToString());
    }

    void OnClientLeaveRoom(SocketIOEvent evt)
    {
        Debug.Log("OnClientLeaveRoom : " + evt.data.ToString());

        FetchPlayerList();
    }

    void OnClientFetchRoomList(SocketIOEvent evt)
    {
        Debug.Log("OnClientFetchRoomList : " + evt.data.ToString());

        roomIDGroup = JsonUtility.FromJson<RoomIDGroup>(evt.data.ToString());
    }

    void OnClientUpdateMoveList(SocketIOEvent evt)
    {
        var dataDict = evt.data.ToDictionary();

        for(int i = 0; i < characterList.Count; i++)
        {
            var newPlayerUpdateData = JsonUtility.FromJson<PlayerUpdateData>(dataDict[characterList[i].uid]);
            Vector3 newPos = new Vector3(newPlayerUpdateData.x, newPlayerUpdateData.y, newPlayerUpdateData.z);
            Vector3 newRotation = new Vector3(newPlayerUpdateData.R_x, newPlayerUpdateData.R_y, newPlayerUpdateData.R_z);
            //Vector3 newBulletPos = new Vector3(newPlayerUpdateData.B_x, newPlayerUpdateData.B_y, newPlayerUpdateData.B_z);

            if(characterList[i].playerObj.transform.position == Vector3.zero)
            {
                characterList[i].playerObj.transform.position = newPos;
                
            }
            if (characterList[i].playerObj.transform.rotation.eulerAngles == Vector3.zero)
            {
                characterList[i].playerObj.transform.rotation = Quaternion.Euler(newRotation);
            }
            //if(characterList[i].playerBullet.transform.position==Vector3.zero)
            //{
             //   characterList[i].playerBullet.transform.position = newBulletPos;
           // }
            

            characterList[i].correctPos = newPos;
            characterList[i].correctRo = newRotation;
           // characterList[i].currentBulletPos = newBulletPos;
            //playerBulletData.bulletCurrectPos = newBulletPos;
        }
    }

    void Win(SocketIOEvent evt)
    {
        winText.SetActive(true);
        StartCoroutine(WaitForDisconnect());
    }

    void Lose(SocketIOEvent evt)
    {
        loseText.SetActive(true);
        StartCoroutine(WaitForDisconnect());
    }




    #endregion
}
