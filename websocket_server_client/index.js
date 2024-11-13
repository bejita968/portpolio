/*
var socket = io.connect('서버 주소');

socket.emit('서버로 보낼 이벤트명', 데이터);

socket.on('서버에서 받을 이벤트명', function(데이터) {
    // 받은 데이터 처리
    socket.emit('서버로 보낼 이벤트명', 데이터);
});
*/

'use strict';

var serverUrl = 'http://192.168.0.4:5000'

var socket = io(); //io(serverUrl); // connect to server

function sendToServer(msg) {
    var msgJSON = JSON.stringify(msg);
    socket.emit('message', msgJSON);
}

socket.on('connect', () => {
    console.log('socket connection is ' + socket.connected);

    doLogin(); // 커넥션 후 로그인으로 시작
});

socket.on('message', msg => {
    console.log(msg.msg);
    alert(msg.msg); // "연결된 player가 없습니다"
    document.getElementById('startBtn').disabled = false;
});

socket.on('user-list', msg => {
    var data = JSON.parse(msg);
    handleUserlistMsg(data);
});

socket.on('done', () => {
    console.log('play done');
    // TODO 버튼 enable 처리
    document.getElementById('startBtn').disabled = false;
});

socket.on('disconnect', () => {
    console.log('socket connection is ' + socket.connected);
})

//////////////////////////////////////////////////////////////

function doLogin() {
    var msg = {
        type: 'operater',
        name: 'operater'
    }
    socket.emit("login", msg);
}

function handleUserlistMsg(msg) {
    var listElem = document.querySelector(".userlistbox");
    while(listElem.firstChild) { // 리스트를 먼저 비운다
        listElem.removeChild(listElem.firstChild);
    }

    // sample
//     msg.users.forEach(function(username) {
//         var item = document.createElement("li");
//         item.appendChild(document.createTextNode(username));
// //        item.addEventListener("click", invite, false);
//         listElem.appendChild(item);
//     });

//     if(msg.users !== undefined) { // 리스트를 채운다
//         msg.users.forEach(function(username) {
//             var item = document.createElement("li");
//             item.appendChild(document.createTextNode(username));
// //            item.addEventListener("click", invite, false);
//             listElem.appendChild(item);
//         });
//     }

    if(msg.users !== undefined) { // 리스트를 채운다
        msg.users.forEach(function(userinfo) {
            var item = document.createElement("li");
            let infoStr = userinfo.name + ' : ' + userinfo.state;
            item.appendChild(document.createTextNode(infoStr));
            listElem.appendChild(item);
        });
    }
}

function stateRefresh() {
    socket.emit('refresh');
}

function onCalibration() {
    socket.emit('calibration');
}

// language set
function changeLang() {
    var lang = document.getElementById('chk_lang');
    if(lang.checked === true) { 
        socket.emit('change_lang', {'lang': 'eng'});
    } else {
        socket.emit('change_lang', {'lang': 'kor'});
    }
}

// command
function episodeStart(ep_num) {
    var msg = {
        cmdType: 1, // episode start
        episodeNumber: ep_num // ep Num
    }
    socket.emit('cmd_control', msg);

    // TODO 버튼 disble 처리
    document.getElementById('startBtn').disabled = true;
}

function multiEpisodeStart() {
    var ep1 = document.getElementById('chk_ep1');
    var ep2 = document.getElementById('chk_ep2');
    var ep3 = document.getElementById('chk_ep3');

    var ep_list = [];

    if(ep1.checked === true) { ep_list.push(Number(ep1.value)); }
    if(ep2.checked === true) { ep_list.push(Number(ep2.value)); }
    if(ep3.checked === true) { ep_list.push(Number(ep3.value)); }

    if(ep_list.length>0) {
        socket.emit('cmd_control_multi', ep_list);
        // TODO 버튼 disble 처리
        document.getElementById('startBtn').disabled = true;
    } else {
        alert('컨텐츠를 선택해 주세요.');
    }
}
