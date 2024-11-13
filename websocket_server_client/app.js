
'use strict';

var express = require('express');
var app = express();
var server = require('http').createServer(app);
var io = require('socket.io')(server);

const PORT = 5000;

////
// web service 
////

//
app.use(express.static(__dirname + '/public'));

// operater page
app.get('/', (req, res) => {
    res.sendFile('./index.html');
});

server.listen(PORT, () => console.log(`### Server Started on ${PORT}###`));

//////
// socket.io 
//////

//// variables
var operater = null; // socket
var players = []; // socket

var gEpisodeList = null;

//// controls
io.on('connection', socket => {
    console.log('user connected...' + socket.id);

    // client callback test code
    socket.on('nova', (data, fn) => {
        console.log(data);
        fn('tech');
    });

    // client가 각자 information을 보내줌
    socket.on('login', data => { 
        console.log('user login ' + data.type + ' ' + data.name);
        socket.type = data.type; // player, operater
        socket.name = data.name; // Quest, simpc, operater

        socket.state = 'ready'; // ready, playing
        socket.syncState = false; // true when client is ready to begin timeline

        socket.epNum = 0; // ?

        // 역활별로 세팅
        if(data.type === 'player') {
            players.push(socket);
        } else if(data.type === 'operater') {
            operater = socket;
            sendUserList();
        }
    });

    socket.on('change_lang', (data) => {
        if(data.lang === 'kor') {
            players.forEach(session => { // 각 플레이어에게 커멘드 전달
                session.emit('change_lang', {langType: 1}); 
            });
        } else if(data.lang === 'eng') {
            players.forEach(session => { // 각 플레이어에게 커멘드 전달
                session.emit('change_lang', {langType: 2}); 
            });
        }
    });

    socket.on('cmd_control_multi', (eplist) => {
        console.log(eplist);

        // TODO 에피소드 리스트를 순차적으로 실행해라.
        // gEpisodeList에서 하나씩 EpNum을 가져와서 실행시킴
        // TODO 연결된 플레이어가 없으면 실행하지 않기 처리
        if(players.length>0) {
            gEpisodeList = eplist;
            playMultiEpisode();
        } else {
            operater.emit('message', {msg: '연결된 Player가 없습니다.'});
        }
    });

    // operater 커멘드 메시지 -> to players
    // msg : {cmdType, episodeNumber} // cmdType=1 : start
    socket.on('cmd_control', (msg) => {
        console.log('command message ' + msg.cmdType + ' ' + msg.episodeNumber);
        players.forEach(session => { // 각 플레이어에게 커멘드 전달
            session.emit('cmd_control', msg); 
        });
    });

    // player의 알림 메시지(컨텐츠 플레이 시작-종료, 각자가 알림)
    // msg : {notiType, episodeNumber}
    // notiType : 1=episode finish, 2=episode start ok
    socket.on('noti_control', (msg) => { 
        console.log('noti message ' + socket.name + ' ' + msg.notiType);
        // TODO :playerState 갱신
        if(msg.notiType === 1) { // finish noti
            socket.state = 'finish';
            socket.epNum = 0;
            // 모든 player 체크해서 state가 모두 finish이면 다음 에피소드 실행
            let epok = false;
            for(var i=0; i<players.length; i++) {
                if(players[i].state === 'finish') {
                    epok = true;
                } else {
                    epok = false;
                    break;
                }
            }
            if(epok === true) { // 모두 ok 이니 다음 에피소드 실행하자
                for(var i=0; i<players.length; i++) {
                    players[i].state = 'ready'; // 초기화
                }
                sleep(4000).then(playMultiEpisode());
            }
        } else if(msg.notiType === 2) { // start ok noti
            socket.state = 'playing';
            socket.epNum = msg.episodeNumber;
        } else if(msg.notiType === 3) { // sync ready
            let syncok = false;
            let i = 0;
            socket.syncState = 1; // set ready

            // 모든 player 체크해서 syncState가 모두 1이면 begin signal을 전송, 전송 후 0으로 초기화
            for(i=0; i<players.length; i++) {
                if(players[i].syncState === 1) {
                    syncok = true;
                } else {
                    syncok = false;
                    break;
                }
            }
            if(syncok === true) { // 모두 ok 이니 begin 을 모두에게 보내주자
                sleep(2000).then( () => {
                    for(i=0; i<players.length; i++) {
                        players[i].emit('cmd_control', {cmdType: 2, episodeNumber: 0}); //send begin timeline
                        players[i].syncState = 0; // 초기화
                        console.log('send sync begin');
                    }
                });
            }
        }
        // userlist를 보내면서 상태까지 같이 보내자
        sendUserList(); // operater에게 보낸다
    });

    // operater에게서 갱신 요청이 오면 userlist + state 를 보내주자
    socket.on('refresh', () => {
        console.log('request refresh');
        sendUserList();
    });

    // operator에게서 view calibration 요청이 오면 각 Player에게 보낸다
    socket.on('calibration', () => {
        console.log('request calibration');

        players.forEach(session => { // 각 플레이어에게 커멘드 전달
            session.emit('calibration', {calibType: 1}); 
        });
});

    // players state 알림 메시지 -> operater
    // msg : {currentState} // 1=ready, 2=playing
    socket.on('player_state', (msg) => {
        console.log('player state message ' + socket.name + ' ' + msg);
        // TODO :playerState 갱신
        if(msg.currentState === 1) {
            socket.state = 'ready';
        } else if(msg.currentState === 2) {
            socket.state = 'playing';
        } else {
            socket.state = 'unknown';
        }
        sendUserList();
    });

    socket.on('disconnect', () => {
        console.log('user disconnected ' + socket.name + ' ' + socket.id);
        if(socket.type === 'player') {
            players.splice(players.indexOf(socket), 1);
        } else if(socket.type === 'operater') {
            operater = null;
        }
    });
});

//// functions
function registerSession(socket) {

}
function unregisterSession(socket) {

}

// unity client에 보냄
function sendToPlayer(ev, data) {

}
function sendToPlayerAll(ev, data) {
    for(i=0; i<players.length; i++) {
        players[i].emit(ev, data); //send
    }
}

// operater 관리자에게 보냄
function sendToOperater(ev, data) {
    operater.emit(ev, data);
}

// operater 관리자에게 접속된 unity client 리스트 보냄
function sendUserList() {
    var userListMsg = makeUserListMessage();
    var userListMsgStr = JSON.stringify(userListMsg);
    operater.emit('user-list', userListMsgStr); // send
}
function makeUserListMessage() {
    var userListMsg = {
        users: [] // {name: '', state: ''}
    };
    for(var i=0; i<players.length; i++) {
        let userinfo = {name: players[i].name, state: players[i].state};
        userListMsg.users.push(userinfo);
    }
    return userListMsg;
}

// Output logging information to console
function log(text) {
    var time = new Date();
    console.log("[" + time.toLocaleTimeString() + "] " + text);
}

async function playMultiEpisode() {
    let currentEpisodeNum = 0;
    if(gEpisodeList.length > 0) { 
        // gList에서 꺼내서 ep 실행 커맨드 보냄
        currentEpisodeNum = gEpisodeList.shift();
        var msg = { 'cmdType': 1, 'episodeNumber': currentEpisodeNum };
        players.forEach(session => { // 각 플레이어에게 커멘드 전달
            session.emit('cmd_control', msg); 
        });
        console.log(msg);
    } else {
        // 종료
        gEpisodeList = null;

        for(var i=0; i<players.length; i++) {
            players[i].state = 'ready'; // 초기화
        }

        operater.emit('done');
        sendUserList()
    }
}

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

function disableButtons() {

}
function enableButtons() {

}
