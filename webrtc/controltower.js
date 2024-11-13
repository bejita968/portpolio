
'use strict';

//////////////////////////////////////////////////////////////////
//
//////////////////////////////////////////////////////////////////

const roomname = 'prevent';

const socket = io.connect(); // 서버 socket 접속

socket.on('connect-ok', function(id) { // 서버 접속 ok 응답 받음
	const socketId = id;
	console.log("connected: " + id);
});

socket.on('created', function(room) { // 방 생성/입장 응답 받음
	isInitiator = true;
});
socket.on('join', function(room) { // 방 입장한다는 응답 받음
	console.log("Another peer made a request to join room");
	isChannelReady = true;
	isInitiator = false;
});
socket.on('joined', function(room) { // 방 입장했다는 응답 받음
	console.log("joined: " + room);
	isChannelReady = true;
	isInitiator = false;
});
socket.on('full', function(room) { // 방 인원이 찼다는 응답 받음(입장 실패)
	console.log("Room " + room + " is full");
});

socket.on('userlist', function(data) { // 서버 유저 리스트 받음
	console.log(data);
	// 유저 리스트를 관리할 일 이 있으면 여기서 처리
});

socket.on('noti-message', function(data) { // 지시문 받음 - 센터에서 보내는 메시지라 여기서는 받지 않음
	console.log(data);
	// 텍스트 -> Toast message display
//	$.toast(data, {duration: 5000});
});

// signaling message variable
var msgOffer = null;

// signaling message
socket.on('message', (message) => {
	var msg = JSON.parse(message);
	
	switch(msg.type) {
		// Signaling messages
		case "video-offer": // 영상통화 요청을 받았을 때
			handleVideoOfferMsg(msg);
			$('#main-body').css({'display':'table'});
			break;
		case "video-answer": // 상대방 응답 신호를 받았을 때 (지금은 사용 않음 = 관제실에서 통화를 걸지 않는다)
			handleVideoAnswerMsg(msg); 
			break;
		case "new-ice-candidate":
			handleNewICECandidateMsg(msg); // ice candidate 신호를 받았을 때
			break;
		case "hang-up":
			handleHangUpMsg(msg); // 통화 종료 신호를 받았을 때
			break;
		default:
			log_error("Unknown message received:");
			log_error(msg);
	}
});

// 화상통화 요청 옴 -> callingModal 띄우기
socket.on('request_calling', (data) => {
	$('#callingModal').show();
});

//////////////////////////////////////////////////////////////////

function sendToServer(msg) {
	var msgJSON = JSON.stringify(msg);
	socket.emit('message', msgJSON);
}

//////////////////////////////////////////////////////////////////


//////////////////////////////////////////////////////////////////
//
//////////////////////////////////////////////////////////////////

// user login / connection
var loginModal = document.getElementById("loginModal");
var loginBtn = document.getElementById("login-btn");
var user = document.getElementById("username");
//loginModal.style.display = "block"; // 시작 시 로그인 모달 출력
$('#loginModal').show();
$('#main-body').hide();
loginBtn.onclick = function() {
    const username = user.value;
	const userType = "expert";
    console.log(username);
	socket.emit('login', {name: username, type: userType});

	if(roomname !== '') {
		socket.emit('create-or-join', roomname);
	}

	$('#loginModal').hide();
	$('#main-body').show();

	// 메인화면 버튼은 비활성화 - 통화가 이루어졌을때만 활성화
	hangupBtn.disabled = true;
	recordBtn.disabled = true;
	saveBtn.disabled = true;
}

//////////////////////////////////////////////////////////////////
//
//////////////////////////////////////////////////////////////////

// 통화 요청이 오면 받기, 취소 
var callingModal = document.getElementById("callingModal");
var callingAcceptBtn = document.getElementById("calling-accept-btn");
var callingCancelBtn = document.getElementById("calling-cancel-btn");
callingAcceptBtn.onclick = function() {

	// answer
	socket.emit('response_calling', {answer: 'accept'});

	$('#callingModal').hide();

	$('#main-body').css({'display':'table'});
	$('#chat-section').html('');

	// 메인화면의 버튼들 활성화
	hangupBtn.disabled = false;
	recordBtn.disabled = false;
	saveBtn.disabled = false;
}
callingCancelBtn.onclick = function() {
	// answer
	socket.emit('response_calling', {answer: 'reject'});

	// refuse calling
	hangupCall();
	
	$('#callingModal').hide();

	$('#main-body').css({'display':'table'});
	$('#chat-section').html('');

	// 메인화면의 버튼들 비활성화
	hangupBtn.disabled = true;
	recordBtn.disabled = true;
	saveBtn.disabled = true;
}

//////////////////////////////////////////////////////////////////
//
//////////////////////////////////////////////////////////////////

var mainContent = document.getElementById("main-content");
var remoteView = document.getElementById("remote-view");
var chatLogElement = document.getElementById("chat-section");
var hangupBtn = document.getElementById("hangup-btn");
var recordBtn = document.getElementById("record-btn");
var saveBtn = document.getElementById("save-btn");
var inputText = document.getElementById("noti-message-input");
var sendBtn = document.getElementById("noti-message-send-btn");
hangupBtn.onclick = function() {
	hangupCall();

	hangupBtn.disabled = true;
	recordBtn.disabled = true;
	saveBtn.disabled = true;

	// 공유화면 닫기 처리 todo
}
recordBtn.addEventListener('click', () => {
	if(recordBtn.textContent === "녹화") {
		startRecording();
		recordBtn.textContent = "녹화중지";
	} else {
		stopRecording();
		recordBtn.textContent = "녹화";
		saveBtn.disabled = false;
	}
});
saveBtn.onclick = function() {
    const blob = new Blob(recordedBlobs, {type: 'video/webm'});
	const url = window.URL.createObjectURL(blob);
	const a = document.createElement('a');
	let timestamp = new Date().toLocaleDateString() + new Date().toLocaleTimeString();
	a.style.display = "none";
	a.href = url;
//	a.download = `nova-${Date.now()}.webm`;
	a.download = `nova-${timestamp}.webm`;
	document.body.appendChild(a);
	a.click();
	setTimeout(() => {
		document.body.removeChild(a);
		window.URL.revokeObjectURL(url);
	}, 100);
saveTextAsFile();
}
sendBtn.onclick = function() {
	var noti_text = inputText.value;
	socket.emit('noti-message', noti_text);
	inputText.value = "";
	
	// textarea 에 appendChild
	chatViewMsg(noti_text);
}
function handleKey(evt) {
    if(evt.keyCode === 13 || evt.keyCode === 14) {
        if(!sendBtn.disabled) {
			var noti_text = inputText.value;
			socket.emit('noti-message', noti_text);
			inputText.value = "";

			// textarea 에 appendChild
			chatViewMsg(noti_text);
        }
    }
}
function chatViewMsg(msg) {
	var str;
	var time = new Date();
	var timeStr = time.toLocaleTimeString();
	
	str = "[" + timeStr + "]" + " : " + msg + "<br>";

	// chatLogElement.innerHTML += str;
	$('#chat-section').append('<p class="noti-msg"><span class="tbox">' + str + '</span></p>');
	$('#chat-section').scrollTop($('#chat-section').prop('scrollHeight'));

	// append text log to file
	let log_str = "[" + timeStr + "]" + " : " + msg + "\n";
	chatlogdata.innerHTML += log_str;
}

//////////////////////////////////////////////////////////////////
//
//////////////////////////////////////////////////////////////////

var isChannelReady = false;
var isInitiator = false;
var isStarted = false;

var audioTrack, videoTrack;
var combineStream = null;

var localStream = null;
var remoteStream = null;
var transceiver;

var pc = null;

var pcConfig = {
	'iceServers': [
		{
			'urls': 'stun:stun.l.google.com:19302'
		},
		{ urls: 'turn:turn.example.com1', credential: 'user', password: 'pass' },
		{
			"urls": "turn:13.250.13.83:3478?transport=udp",
			"username": "YzYNCouZM1mhqhmseWk6",
			"credential": "YzYNCouZM1mhqhmseWk6"		
		}
	]
};

var targetUser = "worker";

var mediaConstraints = {
	audio: true,
	video: true
};
var displayConstraints = {
	audio: false,
	video: true
};

// globals MediaRecorder
var mediaRecorder;
var recordedBlobs;

// data channel
var dataChannel;
var dataConstraints;

//////////////////////////////////////////////////////////////////

async function createPeerConnection() {
	try {
		pc = new RTCPeerConnection(pcConfig);

		pc.onicecandidate = handleIceCandidate;
//		pc.onaddstream = handleAddRemoteStream;
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

function handleIceCandidate(event) {
	if(event.candidate) {
		sendToServer({
			type: "new-ice-candidate",
			target: "worker",
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
	try {
		const offer = await pc.createOffer();
		if(pc.signalingState !== "stable") return;
		await pc.setLocalDescription(offer);
		sendToServer({
			type: "video-offer",
			target: "worker",
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

async function handleVideoOfferMsg(msg) {
	targetUser = "worker";

	await createPeerConnection();
	
	var desc = new RTCSessionDescription(msg.sdp);
	
	pc.setRemoteDescription(desc).then(function () {
		return navigator.mediaDevices.getDisplayMedia({
			video:true
		}).then(async displayStream => {
			[videoTrack] = displayStream.getVideoTracks();
			const audioStream = await navigator.mediaDevices.getUserMedia({audio:true}).catch(e => {throw e});
			[audioTrack] = audioStream.getAudioTracks();
			displayStream.addTrack(audioTrack);
			localStream = displayStream;

			// 공유종료하면 통신종료하는 이벤트 todo : 공유만 종료하고 다시 공유하도록 할 수 있다(공유버튼 만들면)
			displayStream.getVideoTracks()[0].addEventListener('ended', () => {
				hangupCall();
				hangupBtn.disabled = true;
				recordBtn.disabled = true;
				saveBtn.disabled = true;
			});
		});
	}).then(function(stream) {
		localStream.getTracks().forEach(track => {
			pc.addTrack(track, localStream);
		});
	}).then(function() {
		return pc.createAnswer();
	}).then(function(answer) {
		return pc.setLocalDescription(answer);
	}).then(function() {
		sendToServer({
			type: "video-answer",
			target: "worker",
			sdp: pc.localDescription
		});
	}).catch(handleGetUserMediaError);
}

async function handleVideoAnswerMsg(msg) { // 지금은 쓰지 않음. 응답을 받지 않으므로
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

function handleHangUpMsg(msg) { // 통화 종료 신호를 받았을 때 
	// text show : 영상통화를 종료하였슴.
	$.toast(" ***** 통화 종료 ***** ", {duration: 5000});

	closeVideoCall();
}

function hangupCall() {
	sendToServer({
		type: "hang-up",
		target: "worker"
	});

	// text show : 영상통화를 종료하였슴.
	$.toast(" ***** 통화 종료 ***** ", {duration: 5000});

	closeVideoCall();
}

function handleGetUserMediaError(err) {
	closeVideoCall();
}

//////////////////////////////////////////////////////////////////

function handleDataAvailable(event) {
	if(event.data && event.data.size > 0) {
		recordedBlobs.push(event.data);
	}
}
// 녹화 시작
function startRecording() {
	recordedBlobs = [];
	let options = {mimeType: 'video/webm;codecs=vp9,opus'};
	if(!MediaRecorder.isTypeSupported(options.mimeType)) {
		console.error(`${options.mimeType} is not supported`);
		options = {mimeType: 'video/webm;codecs=vp8,opus'};
		if(!MediaRecorder.isTypeSupported(options.mimeType)) {
			console.error(`${options.mimeType} is not supported`);
			options = {mimeType: 'video/webm;'};
			if(!MediaRecorder.isTypeSupported(options.mimeType)) {
				console.error(`${options.mimeType} is not supported`);
				options = {mimeType: ''};
			}
		}
	}
	try {
		mediaRecorder = new MediaRecorder(remoteStream, options);
	} catch(e) {
		console.error('Exception while creating MediaRecorder:', e);
		return;
	}
	
	console.log('Created MediaRecorder', mediaRecorder, 'with options', options);
	recordBtn.textContent = "Stop Recording";
	saveBtn.disabled = true;
	mediaRecorder.onstop = (event) => {
		console.log('Recorder stopped: ', event);
		console.log('Recorded Blobs: ', recordedBlobs);
	};
	mediaRecorder.ondataavailable = handleDataAvailable;
	mediaRecorder.start();
	console.log('MediaRecorder started', mediaRecorder);
}
// 녹화 종료
function stopRecording() {
	mediaRecorder.stop();
}


//////////////////////////////////////////////////////////////////

var chatlogdata = document.createElement("textarea");
function saveTextAsFile() {
	var textToWrite = chatlogdata.value;
	var textFileAsBlob = new Blob([textToWrite], {type:'text/plain'});
	let timestamp = new Date().toLocaleDateString() + new Date().toLocaleTimeString();
//	var fileNameToSaveAs = `nova-${Date.now()}.txt`;
	var fileNameToSaveAs = `nova-${timestamp}.txt`;

	var downloadLink = document.createElement("a");
	downloadLink.download = fileNameToSaveAs;
	downloadLink.innerHTML = "Download File";

	if (window.webkitURL != null) {
		// Chrome allows the link to be clicked
		// without actually adding it to the DOM.
		downloadLink.href = window.webkitURL.createObjectURL(textFileAsBlob);
	}
	else
	{
		// Firefox requires the link to be added to the DOM
		// before it can be clicked.
		downloadLink.href = window.URL.createObjectURL(textFileAsBlob);
		downloadLink.onclick = destroyClickedElement;
		downloadLink.style.display = "none";
		document.body.appendChild(downloadLink);
	}
	downloadLink.click();
}
//////////////////////////////////////////////////////////////////

window.onunload = window.onbeforeunload = () => {
	var msg = {
		type: "bye"
	}
	sendToServer(msg);
	socket.close();
};
