
(function () {

	"use strict";

	///////////////////////////////////////////////////
	// 시작 시 myModal(로그인) 다이얼로그 띄우기
	var modal = document.getElementById("myModal");
	modal.style.display = "block";

	var user = document.getElementById("username");
	var modalBtn = document.getElementById("user-btn");
	// 버튼 클릭 시 입력된 유저이름 가져오고 다이얼로그 닫음
	modalBtn.onclick = function() {
		var username = user.value;
		console.log(username);
		modal.style.display = "none";
	}
	
	///////////////////////////////////////////////////
	// configBtn 클릭 시 카메라 선택하기 위한 config 다이얼로그 띄움
	var configBtn = document.getElementById("config-btn");
	var configModal = document.getElementById("myConfigModal");
	var configOkBtn = document.getElementById("configok-btn");
	configBtn.onclick = function() {
		configModal.style.display = "block";
		doConfigureModalOpen();
	}
	// ok 버튼 클릭 시 디바이스 ID 선택, preview close하고 다이얼로그 닫음
	configOkBtn.onclick = function() {
		if(window.stream) {
			window.stream.getTracks().forEach(track => {
				track.stop();
			});
		}
		window.stream = null;
		configPreviewVideo.srcObject = null;
		configModal.style.display = "none";
	}

	const configAudioSel = document.getElementById("microphone");
	const configVideoSel = document.getElementById("camera");
	const configPreviewVideo = document.getElementById("preview-video");
	const selectors = [configAudioSel, configVideoSel];

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
				configAudioSel.appendChild(option);
			} else if(deviceInfo.kind === 'videoinput') {
				option.text = deviceInfo.label;
				configVideoSel.appendChild(option);
			}
		}
		selectors.forEach((select, selectorIndex) => {
			if(Array.prototype.slice.call(select.childNodes).some(n => n.value === values[selectorIndex])) {
				select.value = values[selectorIndex];
			}
		});
	}
	function handleError(err) {
		console.log(err);
	}
	function startConfig() {
		if(window.stream) {
			window.stream.getTracks().forEach(track => {
				track.stop();
			});
		}
		const audioSource = configAudioSel.value;
		const videoSource = configVideoSel.value;
		const constraints = {
			audio: true,
			video: {deviceId: videoSource ? {exact: videoSource} : undefined}
		};
		navigator.mediaDevices.getUserMedia(constraints).then(gotStream).then(gotDevices).catch(handleError);
	}
	function gotStream(stream) {
		window.stream = stream;
		configPreviewVideo.srcObject = stream;
		return navigator.mediaDevices.enumerateDevices();
	}
	configAudioSel.onchange = startConfig;
	configVideoSel.onchange = startConfig;

	///////////////////////////////////////////////////

	const startBtn = document.getElementById('start-btn');
	startBtn.onclick = function () {
		startChat();
	}

	function startChat() {
    	try {
			const audioSource = configAudioSel.value;
			const videoSource = configVideoSel.value;
			const constraints = {
				audio: true,
				video: {deviceId: videoSource ? {exact: videoSource} : undefined}
			};
      		navigator.mediaDevices.getUserMedia(constraints).then( stream => {
				showChatRoom();

				window.stream = stream;
				document.getElementById('self-view').srcObject = stream;
			});
		} catch (err) {
			console.error(err);
		}
	}

	function showChatRoom() {
		document.getElementById('start-btn').style.display = 'none';
		document.getElementById('chat-room').style.display = 'block';
	}

	const hangupBtn = document.getElementById("hangup-btn");
	hangupBtn.onclick = function() {
		console.log("hangup...");
		if(window.stream) {
			window.stream.getTracks().forEach(track => {
				track.stop();
			});
		}
		window.stream = null;
		document.getElementById('self-view').srcObject = null;
	}
	function hangup() {
		if(window.stream) {
			window.stream.getTracks().forEach(track => {
				track.stop();
			});
		}
		window.stream = null;
		document.getElementById('self-view').srcObject = null;
	}

	const recordBtn = document.getElementById("record-btn");
	recordBtn.onclick = function() {
		console.log("recording...");
	}

	///////////////////////////////////////////////////
	///////////////////////////////////////////////////

	const MESSAGE_TYPE = {
		SDP: 'SDP',
		CANDIDATE: 'CANDIDATE',
  	}
	
	const socket = io();
	
	var localStream;
	var remoteStream;
	
	var pc;
	var pcConfig = {
		'iceServers': [{
			'urls': 'stun:stun.l.google.com:19302'
		}]
	}
	
	var mediaConstraints;
	var displayConstraints;

	////////////////
	const inboxPeople = document.querySelector(".inbox_people");
	
	function addToUserBox(username) {
		if(!!document.querySelector(`.${user}-userlist`)) {
			return;
		}
		const userBox = `
			<div class="chat_ib ${user}-userlist">
				<h5>${username}</h5>
			</div>
		`;
		inboxPeople.innerHTML += userBox;
	};
	
	socket.on('userlist', function(data) {
		data.users.map((username) => addToUserBox(username));
	});
	
	socket.on('noti-message', function(data) {
		console.log(data);
	})

})();
