using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using socket.io;
using Newtonsoft.Json;

public class NetManager : MonoBehaviour
{
    public static NetManager Instance;

    public string serverURL = "http://192.168.0.4:5000";

    public string userType; // "player"
    public string userName; // "Quest(A)", "Quest(B)", "SimPC"

    public int sessionState = 0; // 0=disconnected, 1=connected ??
    public int playerState = 0; // 0=, 1=ready, 2=playing 

    public int syncState = 0;

    public int langType = 1; // default 'kor'

    private Socket socket; // client socket session

    public GameMainController_Temp gameLauncher;

    public class login {
        public string type; // "player"
        public string name; // "QuestA", "QuestB", "SimPC"
        public login(string usertype, string username) {
            this.type = usertype;
            this.name = username;
        }
    } // send

    public class cmd_control {
        public int cmdType; // 0=, 1=Start, 2=syncBegin
        public int episodeNumber; // 0=none, 1=episode1, 2=episode2, 3=eipsode3
        public cmd_control(int cmdtype, int epNum) {
            this.cmdType = cmdtype;
            this.episodeNumber = epNum;
		}
	} // recv

    public class noti_control {
        public int notiType; // 0=, 1=Finish, 2=Start OK, 3=syncReady
        public int episodeNumber; // 0=none, 1=episode1, 2=episode2, 3=episode3
        public noti_control(int notitype, int epNum) {
            this.notiType = notitype;
            this.episodeNumber = epNum;
		}
	} // send

    public class player_state {
        public int currentState; // 1=ready, 2=playing
        public player_state(int state) {
            this.currentState = state;
		}
	} // send

    public class change_language {
        public int langType; // 1=kor, 2=eng
        public change_language(int lang) {
            this.langType = lang;
		}
	}

    public class view_calibration {
        public int calibType; // 1=set
        public view_calibration(int calib) {
            this.calibType = calib;
		}
	}

	private void Awake()
	{
        if(Instance != null) {
            Destroy(gameObject);
            return;
		}
        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        this.socket = Socket.Connect(serverURL);

        // connect event
        socket.On(SystemEvents.connect, () => {
            Debug.LogFormat("connect ok");

            this.sessionState = 1; // connected
            this.playerState = 1; // ready

            var data = JsonConvert.SerializeObject(new login(this.userType, this.userName));
            Debug.LogFormat("data: {0}", data);
            socket.EmitJson("login", data);
        });

        // disconnect event
        socket.On(SystemEvents.disconnect, () => {
            Debug.LogFormat("disconnect");

            this.sessionState = 0; // disconnect
            this.playerState = 0; // none

            // reconnect ?
        });

        // episode 실행 커맨드 받음
        socket.On("cmd_control", (string data) => {
            var r = JsonConvert.DeserializeObject<cmd_control>(data); // recv
            Debug.LogFormat("Command Recv : {0}, {1}", r.cmdType, r.episodeNumber);
            if(r.cmdType == 1) { // Start command
                // TODO r.episodeNumber // Start Episode
                if(r.episodeNumber == 1) {
                    gameLauncher.StartCoroutine(gameLauncher.Co_LoadFirst());
                    SendEpisodeStartOk(2, r.episodeNumber); // start ok, epnum
                } else if(r.episodeNumber == 2) {
                    gameLauncher.StartCoroutine(gameLauncher.Co_LoadSecond());
                    SendEpisodeStartOk(2, r.episodeNumber); // start ok, epnum
                } else if(r.episodeNumber == 3) {
                    gameLauncher.StartCoroutine(gameLauncher.Co_LoadThird());
                    SendEpisodeStartOk(2, r.episodeNumber); // start ok, epnum
                }
            } else if(r.cmdType == 2) { // sync begin timeline
                syncState = 1; // sync flag on, then begin timeline each episodecontroller
			}
		});

        socket.On("change_lang", (string data) => {
            var l = JsonConvert.DeserializeObject<change_language>(data);
            if(l.langType == 1) {
                this.langType = 1; // kor
			} else if(l.langType == 2) {
                this.langType = 2; // eng
			}
		});

        socket.On("calibration", (string data) => {
            var c = JsonConvert.DeserializeObject<view_calibration>(data);
            if(c.calibType == 1) {
                // call calib func in gamemaincontroller
                gameLauncher.onCalibrationSet();
			}
		});

        // samples
        //socket.On("join_user", (string data) => { // event
        //    var a = JsonConvert.DeserializeObject<join_user>(data); // recv
        //    Debug.LogFormat("{0}, {1}", a.cmd, a.message);
        //    socket.Emit("emit_join_user", JsonConvert.SerializeObject(new emit_join_user(201, userName))); // send
        //});

        //// receive "news" event
        //socket.On("news", (string data) => {
        //    Debug.Log(data);
        //    // Emit raw string data
        //    socket.Emit("my other event", "{ my: data }");
        //    // Emit json-formatted string data
        //    socket.EmitJson("my other event", @"{ ""my"": ""data"" }");
        //});
    }

    public void SendEpisodeStartOk(int notiType, int contentNumber)
    {
        var data = JsonConvert.SerializeObject(new noti_control(notiType, contentNumber)); // episode start ok, epNum
        socket.EmitJson("noti_control", data);
    }
    public void SendEpisodeSyncReady(int notiType, int contentNumber) {
        var data = JsonConvert.SerializeObject(new noti_control(notiType, contentNumber)); // sync ready
        socket.EmitJson("noti_control", data);
	}
    public void SendEpisodeFinish(int notiType, int contentNumber)
    {
        var data = JsonConvert.SerializeObject(new noti_control(notiType, contentNumber)); // finish, epNum
        socket.EmitJson("noti_control", data);
    }
}
