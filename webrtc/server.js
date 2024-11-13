'use strict';

var express = require('express');
var app = express();
var server = require('http').createServer(app);
var io = require('socket.io')(server);
var fs = require('fs');

const PORT = 3000;

//
//app.use('/public', express.static(__dirname + '/public'));
app.use(express.static(__dirname + '/public'));

//
app.get('/ct', (req, res) => {
	res.sendFile(__dirname + '/public/controltower.html');
});
app.get('/ct2', (req, res) => {
	res.sendFile(__dirname + '/public/controltower2.html');
});
app.get('/ct3', (req, res) => {
	res.sendFile(__dirname + '/public/controltower3.html');
});
app.get('/client', (req, res) => {
	res.sendFile(__dirname + '/public/client.html');
});
app.get('/client2', (req, res) => {
	res.sendFile(__dirname + '/public/client2.html');
});
app.get('/client3', (req, res) => {
	res.sendFile(__dirname + '/public/client3.html');
});

server.listen(PORT, () => {
	console.log("Socket IO server listening on port: " + PORT);
});

//////////////////////////////////////////////////////////////////

const roomname = 'prevent';

var expertUser = null;
var workerUser = null;

var socket_ids = [];

var logfilename = "prevent-log.txt";

//////////////////////////////////////////////////////////////////

io.on('connect', function(socket) {
	console.log('user connected');
	socket.emit('connect-ok', socket.id); // 소켓 접속되면 응답 보냄
	
	// 유저가 로그인 패킷을 보내면 타입(expert, worker), 이름, expertUser/workerUser 소켓id 설정, 유저리스트 등록
	socket.on('login', function(data) {
		socket.userType = data.type;
		socket.name = data.name;
		console.log("type: " + data.type + ", name: " + data.name);
		if(socket.userType === 'expert') {
			expertUser = socket.id;
		} else if(socket.userType === 'worker') {
			workerUser = socket.id;
		}
		registerUser(socket, data.name);
		writeLogFile(socket.name + ' login');
	});
	
	// 방 이름으로 생성 or 참가 신청
	socket.on('create-or-join', function(room) {
		socket.roomname = room;

		var clientsInRoom = io.sockets.adapter.rooms[room];
		var numClients = clientsInRoom ? Object.keys(clientsInRoom.sockets).length : 0;
		if(numClients === 0) {
			socket.join(room);
			socket.emit('created', room, socket.id); // 방을 만들었다고 알려준다
		} else if(numClients === 1) {
			io.sockets.in(room).emit('join', room);
			socket.join(room);
			socket.emit('joined', room, socket.id); // 방에 참여했다고 알려준다

			io.sockets.in(room).emit('ready');
		} else {
			socket.emit('full', room); // 방이 꽉 찼다고 알려준다
		}
	});

	// 관제실 공지 메시지를 작업자에게 전달
	socket.on('noti-message', function(data) {
		socket.broadcast.emit('noti-message', data);
	});

	// 클라이언트에서 서버로 소켓 종료 요청
	socket.on('forceDisconnect', function() {
		unregisterUser(socket, socket.name);
		socket.disconnect();
	});

	// 소켓 접속 종료 처리
	socket.on('disconnect', function() {
		console.log("user disconnected: " + socket.name);
		writeLogFile(socket.name + ' logout');
		unregisterUser(socket, socket.name);
	});
	
	// message 패킷 전달
	socket.on('message', function(message) {
		var msg = JSON.parse(message);
		
		// bye message이면 룸에서 나가기
		if(msg.type === "bye") {
			io.of('/').in(socket.roomname).clients((error, socketIds) => {
				if(error) throw error;
				socketIds.forEach(socketId => {
					io.sockets.sockets[socketId].leave(socket.roomname);
				});
			});
		}
		// bye가 아니고 타겟이 있으면
		else if(msg.target && msg.target !== undefined && msg.target.length !== 0) { 
			var msgString = JSON.stringify(msg);

			if(msg.target === "expert") { // 관제실로 보냄
				sendToExpert(msgString);
			} else if(msg.target === "worker") { // 작업자에게 보냄
				sendToWorker(msgString);
			} else { // 관제실이나 작업자가 아닐때는 상대방에게 전달 (1:1 이므로)
			 	socket.broadcast.emit('message', msgString);
			}
		}
	});
	
	// calling
	socket.on('request_calling', (data) => {
		io.to(expertUser).emit('request_calling', data);
	});
	
	// calling accept or reject
	socket.on('response_calling', (data) => { // data.answer == 'accept' or 'reject'
		io.to(workerUser).emit('response_calling', data);
	});
});

function registerUser(socket, name) {
	if(socket.name != undefined) {
		delete socket_ids[socket.name];
		socket_ids[name] = socket.id;
		io.sockets.emit('userlist', {users: Object.keys(socket_ids)});
	} 
}

function unregisterUser(socket, name) {
	if(socket.name != undefined) {
		if(socket.userType === 'expert') {
			expertUser = null;
		} else if(socket.userType === 'worker') {
			workerUser = null;
		}
		delete socket_ids[socket.name];
		io.sockets.emit('userlist', {users: Object.keys(socket_ids)});
	}
}

function sendToExpert(msg) {
	if(expertUser) {
		io.to(expertUser).emit('message', msg);
	}
}

function sendToWorker(msg) {
	if(workerUser) {
		io.to(workerUser).emit('message', msg);
	}
}

function writeLogFile(msg) {
	var str;
	var time = new Date();
	var timeStr = time.toUTCString();//.toLocaleString();
	
	let log_str = "[" + timeStr + "]" + " : " + msg + "\n";
	fs.appendFile(logfilename, log_str, () => console.log('write to file'));
}
