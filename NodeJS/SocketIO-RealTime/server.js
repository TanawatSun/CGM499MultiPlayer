var io = require("socket.io")(5055);
var uid = require('uid');

console.log("socket listen at port 5055");

var playerIDList = [];

var roomIDList = [];
var roomDataList = {};


io.on("connection", (socket)=>{

    console.log("client connected : "+socket.id);

    //socket.join("lobby"+uid());

    ClientConnect(io, socket);

    ClientFetchPlayerList(socket);

    ClientCreateRoom(socket);

    ClientJoinRoom(socket);

    ClientLeaveRoom(socket);

    ClientFetchRoomList(socket);

    ClientUpdateMove(socket);


    PlayerFire(socket);
    
    WinCondition(socket);  
    

    socket.on("disconnect", ()=>{

        console.log("client disconnected : "+socket.id);

        ClientDisconnect(io, socket);
    });

   
    
});

setInterval(()=>{

    for(var i = 0; i < roomIDList.length; i++)
    {
        var roomName = roomIDList[i];
        for(var j = 0; j < roomDataList[roomName].sockets.length; j++)
        {
            var socket = roomDataList[roomName].sockets[j];

            if(socket != undefined)
            {
                socket.emit("OnClientUpdateMoveList", roomDataList[roomName].playerData);
                
            }
        }
    }

}, 100);

var ClientConnect = (io, socket)=>{

    var data = {
        "uid":socket.id
    };

    CountPlayer();

    socket.emit("OnOwnerClientConnect", data);
}

var ClientDisconnect = (io, socket)=>{

    var data = {
        "uid":socket.id
    };

    /*for(var i = 0; i < playerIDList.length; i++)
    {
        if(playerIDList[i] == data.uid)
        {
            playerIDList.splice(i, 1);
            console.log("delete player : " + data.uid);
        }
    }*/

    LeaveRoom(socket);

    CountPlayer();

    io.emit("OnClientDisconnect", data);
}

var ClientFetchPlayerList = (socket)=>{

    socket.on("OnClientFetchPlayerList", (data)=>{

        var isRoomFound = (element)=>{
            return element == data.roomName;
        }

        var roomName = data.roomName;

        if(roomIDList.find(isRoomFound) != undefined)
        {
            var playerIds = [];

            for(var i = 0; i < roomDataList[roomName].sockets.length; i++)
            {
                playerIds.push(roomDataList[roomName].sockets[i].id);
            }

            var data = {
                "playerIDList": playerIds,
            }
            socket.emit("OnClientFetchPlayerList", data);
        }
        else
        {
            var data = {};
            socket.emit("OnClientFetchPlayerList", data);
        }
    });
} 

var CountPlayer = ()=>{
    console.log("Player total : "+ playerIDList.length);
}


var ClientFetchRoomList = (socket)=>{
    
    socket.on("OnClientFetchRoomList", ()=>{

        data = {
            "roomIDList":roomIDList
        }

        socket.emit("OnClientFetchRoomList", data);
    });
}

var ClientCreateRoom = (socket)=>{
    //roomName : string
    socket.on("OnClientCreateRoom", data=>{
        console.log("ClientCreateRoom : "+socket.id +" : "+data.roomName);

        var isRoomFound = (element)=>{
            return element == data.roomName;
        }

        if(roomIDList.find(isRoomFound) != undefined || data.roomName == ""){
            console.log("ClientCreateRoom : fail");
            socket.emit("OnClientCreateRoomFail", data);
        }else{
            roomIDList.push(data.roomName);
            roomDataList[data.roomName] = {
                "sockets": [socket],
                "playerData": {},
            };

            var resultData = {
                uid: socket.id
            };


            socket.join(data.roomName, ()=>{
                console.log("CientCreatRoom : success");
                socket.emit("OnClientCreateRoomSuccess", resultData);
            });
        }
        
    });
}

var ClientJoinRoom = (socket)=>{
    socket.on("OnClientJoinRoom", (data)=>{
        console.log("ClientJoinRoom");

        var isRoomFound = (element)=>{
            return element == data.roomName;
        };

        if(roomIDList.find(isRoomFound) != undefined && data.roomName != ""){
            
           
            roomDataList[data.roomName].sockets.push(socket);

            var resultData = {
                uid: socket.id
            };

            socket.join(data.roomName, ()=>{
                console.log("ClientJoinRoom : success");
                socket.emit("OnOwnerClientJoinRoomSuccess", resultData);
                socket.broadcast.to(data.roomName).emit("OnClientJoinRoomSuccess", resultData);
            });

        }else{
            console.log("ClientJoinRoom : fail");
            socket.emit("OnClientJoinRoomFail", data);
        }
    });
}

var ClientLeaveRoom = (socket)=>{

    socket.on("OnClientLeaveRoom", ()=>{
        console.log("client leave room");
        LeaveRoom(socket);
    });
}

var LeaveRoom = (socket)=>{

    for(var i = 0; i < roomIDList.length;i++)
    {
        var roomName = roomIDList[i];

        for(var j = 0; j < roomDataList[roomName].sockets.length; j++)
        {
            if(roomDataList[roomName].sockets[j].id == socket.id)
            {
                roomDataList[roomName].sockets.splice(j,1);
                
                delete roomDataList[roomName].playerData[socket.id];

                var data = {
                    uid: socket.id
                };

                socket.broadcast.to(roomName).emit("OnClientLeaveRoom", data);
                socket.emit("OnClientLeaveRoom", data);
                socket.leave(roomName);

                break;
            }
        }

        if(roomDataList[roomName].sockets.length <= 0)
        {
            roomIDList.splice(i, 1);
            delete roomDataList[roomName];
            break;
        }
    }
}

var ClientUpdateMove = (socket)=>{

    socket.on("OnClientUpdateMove", (data)=>{

        if(roomDataList[data.roomName] != undefined)
        {
            roomDataList[data.roomName].playerData[data.uid] = {
                x: data.x,
                y: data.y,
                z: data.z,
                R_x: data.R_x,
                R_y: data.R_y,
                R_z: data.R_z,

                B_x: data.B_x,
                B_y: data.B_y,
                B_z: data.B_z,
            };
            //console.log(data);
        }
        
        
    });
}
var PlayerFire = (socket) =>{

    socket.on("Fire",(data)=>{
        
        socket.emit("Fire",data);
        socket.broadcast.emit("Fire",data);
    });
}

var WinCondition = (socket) =>{
    socket.on("WinCheck",()=>{
        //console.log("WinCheck");
        socket.emit("Lose");
        socket.broadcast.emit("Win");
    });
}

