
'use strict';

//////////////////////////////////////////////////////////////////
//
//////////////////////////////////////////////////////////////////

const roomname = 'prevent';

const socket = io.connect(); // 서버로 소켓 연결

socket.on('connect-ok', function(id) {
	const socketId = id;
	console.log("connected: " + id);
});

socket.on('created', function(room) {
	isInitiator = true;
});
socket.on('join', function(room) {
	console.log("Another peer made a request to join room");
	isInitiator = false;
	isChannelReady = true;
});
socket.on('joined', function(room) {
	console.log("joined: " + room);
	isInitiator = false;
	isChannelReady = true;
});
socket.on('full', function(room) {
	console.log("Room " + room + " is full");
	$.toast('connection full, try again later..', {duration: 3000});
});

socket.on('userlist', function(data) {
	console.log(data);
	// 유저 리스트를 관리할 일이 있으면 여기서 처리
});

socket.on('noti-message', function(data) {
	console.log(data);
	// 텍스트 지시사항 -> Toast message display
	$.toast(data, {duration: 5000});
	$('#chat-section').html('<p class="noti-msg"><span class="tbox">' + data + '</span></p>');
});

var msgOffer = null;

// signaling message
socket.on('message', (message) => {
	var msg = JSON.parse(message);
	
	switch(msg.type) {
		// Signaling messages
		case "video-offer": // 영상통화 요청을 받았을 때
			handleVideoOfferMsg(msg);
			break;
		case "video-answer": // 영상통화 응답을 받았을 때
			handleVideoAnswerMsg(msg);
			break;
		case "new-ice-candidate": // ice candidate 받았을 때
			handleNewICECandidateMsg(msg);
			break;
		case "hang-up": // 영상통화 종료 신호를 받았을 때
			handleHangUpMsg(msg);
			break;
		default:
			log_error("Unknown message received:");
			log_error(msg);
	}
});

socket.on('response_calling', (data) => {
	if(data.answer == 'accept') {
		startCall();
	} else if(data.answer == 'reject') {
		// hide call, show main	// 통화 대기 상태로
		$('#callModal').hide();
		$('#main-content').show();
		makePortrait();
	}
});

//////////////////////////////////////////////////////////////////

function sendToServer(msg) {
	var msgJSON = JSON.stringify(msg);
	socket.emit('message', msgJSON);
}

//////////////////////////////////////////////////////////////////

// 클라이언트에서 로그인은 패스
// user login / connection
var loginModal = document.getElementById("loginModal");
var loginBtn = document.getElementById("login-btn");
var user = document.getElementById("username");
//loginModal.style.display = "block"; // loginModal 을 활성화 한다
var nameTmp = localStorage.getItem('userName');
if(nameTmp !== '') {
	user.value = nameTmp;
}
$('#loginModal').show();
loginBtn.onclick = function() {
	doLogin();

	$('#loginModal').hide();
	$('#chat-section').html('');
	$('#main-content').show();
}
function doLogin() {
    const userType = "worker";
    const username = user.value;
    console.log(username);

	socket.emit('login', {type: userType, name: username});

	if(roomname !== '') {
		socket.emit('create-or-join', roomname); // 방만들기 요청 -> TODO : 화상통화 걸 때로 바꾸기
	}
}

//////////////////////////////////////////////////////////////////
//
//////////////////////////////////////////////////////////////////

var configModal = document.getElementById("configModal");
var configOkBtn = document.getElementById("config-ok-btn");
var audioSelectEl = document.getElementById("audio-sel");
//var speakerSelectEl = document.getElementById("speaker-sel");
var cameraSelectEl = document.getElementById("camera-sel");
var previewEl = document.getElementById("preview");
configOkBtn.onclick = function() {
    if(window.stream) {
        window.stream.getTracks().forEach(track => {
            track.stop();
        });
    }
    window.stream = null;
    previewEl.srcObject = null;

	$('#configModal').hide();
	$('#main-content').show();
}

const selectors = [audioSelectEl, cameraSelectEl];

function doConfigureModalOpen() {
    navigator.mediaDevices.enumerateDevices().then(gotDevices).catch(handleError);
    startConfig();
}
function gotDevices(deviceInfos) {
    const values = selectors.map(select => select.value);
    selectors.forEach(select => {
        while(select.firstChild) {
            select.removeChild(select.firstChild);
        }
    });
    for(let i=0; i!==deviceInfos.length; ++i) {
        const deviceInfo = deviceInfos[i];
        const option = document.createElement('option');
        option.value = deviceInfo.deviceId;
        if(deviceInfo.kind === 'audioinput') {
            option.text = deviceInfo.label;
            audioSelectEl.appendChild(option);
        } else if(deviceInfo.kind === 'videoinput') {
            option.text = deviceInfo.label;
            cameraSelectEl.appendChild(option);
        }
    }
    selectors.forEach((select, selectorIndex) => {
        if(Array.prototype.slice.call(select.childNodes).some(n => n.value === values[selectorIndex])) {
            select.value = values[selectorIndex];
        }
    });
}
function handleError(err) {
    console.error(err);
}

var audioSource;
var videoSource;
var mediaConstraints = { // default constraints
	audio: true,
	video: { facingMode: 'environment'}
};

function startConfig() {
    if(window.stream) {
        window.stream.getTracks().forEach(track => {
            track.stop();
        });
    }
    audioSource = audioSelectEl.value;
    videoSource = cameraSelectEl.value;
    mediaConstraints = {
        audio: true,
        video: {deviceId: videoSource ? {exact: videoSource} : undefined}
    };
   	navigator.mediaDevices.getUserMedia(mediaConstraints).then(gotStreamPrev).then(gotDevices).catch(handleError);
}
function gotStreamPrev(stream) {
    window.stream = stream;
    previewEl.srcObject = stream;
    return navigator.mediaDevices.enumerateDevices();
}

audioSelectEl.onchange = startConfig;
cameraSelectEl.onchange = startConfig;


//////////////////////////////////////////////////////////////////
//
//////////////////////////////////////////////////////////////////

// configure media device / select device
var configBtn = document.getElementById("config-btn");
configBtn.onclick = function() {
	$('#main-content').hide();
//    configModal.style.display = "block";
	$('#configModal').show();
    doConfigureModalOpen();
}

//////////////////////////////////////////////////////////////////
//
//////////////////////////////////////////////////////////////////

var mainContent = document.getElementById("main-content");

//////////////////////////////////////////////////////////////////
//
//////////////////////////////////////////////////////////////////

var callState = 0; // 0:ready, 1:calling, 2:connect

// webrtc call request
var callBtn1 = document.getElementById("call-btn1");
var callBtn2 = document.getElementById("call-btn2");
var callBtn3 = document.getElementById("call-btn3");
var callBtnH = document.getElementById("call-btn_h");
callBtn1.onclick = function() {
	$('#main-content').hide();

	openFullscreen(); // documentElement fullscreen and landscape mode

	$('#callModal').show();
	$('#fullscreen-exit-btn').hide();
	$('#fullscreen-btn').show();

	// => 요청을 소켓으로 보내고 응답 받은 후 rtc 요청 보내는 걸로 바꾸자
	socket.emit('request_calling', {req:'vcall'});
}
callBtn2.onclick = function() {
	$('#main-content').hide();

	openFullscreen();

	$('#callModal').show();
	$('#fullscreen-exit-btn').hide();
	$('#fullscreen-btn').show();

	// => 요청을 소켓으로 보내고 응답 받은 후 rtc 요청 보내는 걸로 바꾸자
	socket.emit('request_calling', {req:'vcall'});
}
callBtn3.onclick = function() {
	alert('not setting part on server');
}
callBtnH.onclick = function() {
	alert('not support yet');
}

//////////////////////////////////////////////////////////////////
//
//////////////////////////////////////////////////////////////////

var magnifyVal = 0;
// connected webrtc
var callModal = document.getElementById("callModal");
var remoteView = document.getElementById("remote-view");
var hangupBtn = document.getElementById("call-cancel-btn");
$('#fullscreen-btn').on('click', function(){
	$('#callModal').show();
	openElementFullscreen('video-player');
	$('#fullscreen-exit-btn').show();
	$('#fullscreen-btn').hide();
});
$('#fullscreen-exit-btn').on('click', function(){
	closeElementFullscreen('video-player');
	$('#callModal').show();
	$('#fullscreen-exit-btn').hide();
	$('#fullscreen-btn').show();
});
hangupBtn.onclick = function() {
	hangupCall();
	$('#callModal').hide();
	$('#main-content').show();
}

//////////////////////////////////////////////////////////////////
//
//////////////////////////////////////////////////////////////////

var isChannelReady = false;
var isInitiator = false;
var isStarted = false;

var localStream;
var remoteStream;
var transceiver;

var pc = null;
var pcConfig = {
	'iceServers': [
		{
			'urls': 'stun:stun.l.google.com:19302'
		},
		{
			"urls": [
				"turn:13.250.13.83:3478?transport=udp"
			],
			"username": "YzYNCouZM1mhqhmseWk6",
			"credential": "YzYNCouZM1mhqhmseWk6"		
		}
	]
};

var targetUser = "expert";

// data channel
var dataChannel;
var dataConstraints;

var sdpConstraints = {
	offerToReceiveAudio: true,
	offerToReceiveVideo: true
};

//////////////////////////////////////////////////////////////////

function startCall() { // 통화 요청 시작
	if(pc) {
		alert("you already have one open");
	} else {
		targetUser = "expert";

		createPeerConnection();
		
		navigator.mediaDevices.getUserMedia(mediaConstraints).then(gotStream).catch(handleError);
	}
}
async function createPeerConnection() { // WebRTC Peer 생성
	try {
		pc = new RTCPeerConnection(pcConfig);

		pc.onicecandidate = handleIceCandidate;
		pc.onremovestream = handleRemoveRemoteStream;
		pc.ontrack = handleTrackEvent;
		pc.oniceconnectionstatechange = handleIceConnectionStateChange;
		pc.onicegatheringstatechange = handleIceGatheringStateChange;
		pc.onsignalingstatechange = handleSignalingStateChange;
		pc.onnegotiationneeded = handleNegotiationNeeded;
	} catch(e) {
		return;
	}
}
function gotStream(stream) {
    localStream = stream;
	localStream.getTracks().forEach(track => {
		pc.addTrack(track, localStream);
	});

	isStarted = true;

	doCall(); // offer 요청 보내기
}
function doCall() {
	pc.createOffer(setLocalAndSendOfferMessage, handleCreateOfferError);
}
function handleCreateOfferError(e) {
	console.log("create offer error" + e);
}
function setLocalAndSendOfferMessage(sdp) {
	pc.setLocalDescription(sdp);
	var msg = {
		type: "video-offer",
		target: "expert",
		sdp: pc.localDescription
	}
	sendToServer(msg);
}

function stopCall() {
	hangupCall();
}

function handleIceCandidate(event) {
	if(event.candidate) {
		sendToServer({
			type: "new-ice-candidate",
			target: "expert",
 			label: event.candidate.sdpMLineIndex,
 			id: event.candidate.sdpMid,
			candidate: event.candidate
		});
	}
}

function handleAddRemoteStream(event) {
	remoteStream = event.stream;
	remoteView.srcObject = remoteStream;
}
function handleRemoveRemoteStream(event) {
	remoteView.srcObject = null;
	remoteStream = null;
}
function handleTrackEvent(event) {
 	remoteStream = event.streams[0];
 	remoteView.srcObject = remoteStream;
}

async function handleNegotiationNeeded() {
	try{
		const offer = await pc.createOffer();
		if(pc.signalingState !== "stable") return;
		await pc.setLocalDescription(offer);
		sendToServer({
			type: "video-offer",
			target: "expert",
			sdp: pc.localDescription
		});
	} catch(e) {
		console.log(e);
	}
}
function handleIceConnectionStateChange(event) {
	switch(pc.iceConnectionState) {
		case "closed":
		case "failed":
		case "disconnected":
			closeVideoCall();
			break;
	}
}
function handleIceGatheringStateChange(event) {
	console.log("Ice Gathering State changed");
}
function handleSignalingStateChange(event) {
	switch(pc.signalingState) {
		case "closed":
			closeVideoCall();
			break;
	}
}

function closeVideoCall() {
	if(pc) {
		pc.ontrack = null;
		pc.onicecandidate = null;
		pc.oniceconnectionstatechange = null;
		pc.onicegatheringstatechange = null;
		pc.onsignalingstatechange = null;
		pc.onnegotiationneeded = null;
		pc.onnotificationneeded = null;
		
		pc.close();
		pc = null;

		localStream = null;
	}
	if(remoteStream) {
		remoteStream.getTracks().forEach(track => {
			track.stop();
		});
		remoteStream = null;
	}
}

// signaling functions
async function handleVideoOfferMsg(msg) {
	if(!pc) {
		createPeerConnection();
	}
	
	var desc = new RTCSessionDescription(msg.sdp);
	
	if(pc.signalingState !== "stable") {
		await Promise.all([
			pc.setLocalDescription({type: "rollback"}),
			pc.setRemoteDescription(desc)
		]);
		return;
	} else {
		await pc.setRemoteDescription(desc);
	}
	
	if(!localStream) {
		try {
			localStream = await navigator.mediaDevices.getUserMedia(mediaConstraints);
		} catch(err) {
			handleGetUserMediaError(err);
			return;
		}
		
		try {
			localStream.getTracks().forEach(track => {
				pc.addTrack(track, localStream);
			});
		} catch(err) {
			handleGetUserMediaError(err);
		}
	}
	
	await pc.setLocalDescription(await pc.createAnswer());
	
	sendToServer({
		type: "video-answer",
		target: "expert",
		sdp: pc.localDescription
	});
}

async function handleVideoAnswerMsg(msg) {
	var desc = new RTCSessionDescription(msg.sdp);
	await pc.setRemoteDescription(desc).catch( e => console.log(e));
}

async function handleNewICECandidateMsg(msg) {
	var candidate = new RTCIceCandidate(msg.candidate);
	try {
		await pc.addIceCandidate(candidate);
	} catch(err) {
		console.log(err);
	}
}

function handleHangUpMsg(msg) { // 영상통화 종료 신호 받았을 때
	// text show : 영상통화를 종료하였슴.
	$.toast(" ***** 통화 종료 ***** ", {duration: 3000});
//	alert(" ***** 통화 종료 ***** ");

	closeVideoCall(); // video stream (local, remote), peer 종료/초기화

	// 통화 대기 상태로
//	mainContent.style.display = "none";
	$('#callModal').hide();

//	callBtn.style.display = "block";
// 	configBtn.style.display = "block";
	$('#main-content').show();

	makePortrait();
}

function hangupCall() { // 영상통화 종료 눌렀을 때
	sendToServer({
		type: "hang-up",
		target: "expert"
	}); // 서버로 상대방에게 통화 종료를 알려준다

	// text show : 영상통화를 종료하였슴.
	$.toast(" ***** 통화 종료 ***** ", {duration: 3000});
//	alert(" ***** 통화 종료 ***** ");

	closeVideoCall(); // video stream (local, remote), peer 종료/초기화

	$('#callModal').hide();

	$('#main-content').show();

	makePortrait();
}

function handleGetUserMediaError(err) {
	console.log("handleGetUserMediaError : "+err);

	closeVideoCall();

	$('#callModal').hide();

	$('#main-content').show();
}

//////////////////////////////////////////////////////////////////

window.onunload = window.onbeforeunload = () => { // 창 닫거나 새로고침으로 종료될 때

	//screen.orientation.unlock(); // 스크린 잠금 풀기
	closeFullscreen();

	var msg = {
		type: "bye"
	}

	sendToServer(msg); // 창 닫히기 직전에 bye message를 보낸다.
	
	socket.close(); // 소켓 접속 종료
};


/* View in fullscreen */
function openFullscreen(id) {
	var elem = document.documentElement;
	if(id){
		elem = document.getElementById(id);
	}
	if (elem.requestFullscreen) {
		elem.requestFullscreen();
	}
	makeLandscape();	
}
/* Close fullscreen */
function closeFullscreen() {
	if (document.exitFullscreen) {
		document.exitFullscreen();
	}
	makePortrait();
}
/* Landscape mode */
function makeLandscape() {
	// this works on android, not iOS
	if (screen.orientation && screen.orientation.lock) {
		screen.orientation.lock('landscape');
	}
}
/* Portrait mode */
function makePortrait() {
	// this works on android, not iOS
	if (screen.orientation && screen.orientation.lock) {
		screen.orientation.lock('portrait');
	}
}

function openElementFullscreen(id) {
	var elem = document.documentElement;
	if(id) {
		elem = document.getElementById(id);
	}
	if (elem.requestFullscreen) {
		elem.requestFullscreen();
	}

	makeLandscape();
}
function closeElementFullscreen(id) {
	if(document.fullscreenElement) {
		document.exitFullscreen();
	}
}
