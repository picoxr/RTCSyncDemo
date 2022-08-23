using System;
using System.Collections.Generic;
using System.Text; 
using Pico.Platform.Models; 
using UnityEngine; 
using UnityEngine.UI;


namespace Pico.Platform.Samples.RtcDemo
{
    public class RTCScripts : MonoBehaviour
    {
        // Output text
        public Text Info;

        // Get pico account user message
        public static User MyUser = null;

        // Input room id
        private InputField inputRoomId;

        // The timestamp of print room stats. Used for control frequency.
        private float lastRoomStatsTime = 0;


        private void Start()
        {
            var muteOthers = GameObject.Find("MuteOthers").GetComponent<Toggle>();
            var muteSelf = GameObject.Find("AudioMute").GetComponent<Toggle>(); 
            inputRoomId = GameObject.Find("InputRoomId").GetComponent<InputField>(); 

            // Callback function of RtcService.
            RtcService.SetOnJoinRoomResultCallback(OnJoinRoom);
            RtcService.SetOnLeaveRoomResultCallback(OnLeaveRoom);
            RtcService.SetOnUserLeaveRoomResultCallback(OnUserLeaveRoom);
            RtcService.SetOnUserJoinRoomResultCallback(OnUserJoinRoom);
            RtcService.SetOnRoomStatsCallback(OnRoomStats);
            RtcService.SetOnWarnCallback(OnWarn);
            RtcService.SetOnErrorCallback(OnError);
            RtcService.SetOnRoomWarnCallback(OnRoomWarn);
            RtcService.SetOnRoomErrorCallback(OnRoomError);
            RtcService.SetOnConnectionStateChangeCallback(OnConnectionStateChange);
            RtcService.SetOnUserMuteAudio(OnUserMuteAudio);
            RtcService.SetOnUserStartAudioCapture(OnUserStartAudioCapture);
            RtcService.SetOnUserStopAudioCapture(OnUserStopAudioCapture);
            RtcService.SetOnLocalAudioPropertiesReport(OnLocalAudioPropertiesReport);
            RtcService.SetOnRemoteAudioPropertiesReport(OnRemoteAudioPropertiesReport);

            AddInfo("Hello World");

            // Turn on or off the others' audio in the room.
            muteOthers.onValueChanged.AddListener((v) =>
            {
                if (v)
                {
                    AddInfo($"Mute other player voice");
                    RtcService.RoomPauseAllSubscribedStream(inputRoomId.text);
                    AddInfo($"StartMuteOthers Done");
                }
                else
                {
                    AddInfo($"Before ResumeVoiceCapture");
                    RtcService.RoomResumeAllSubscribedStream(inputRoomId.text);
                    AddInfo($"ResumeAudio Done");
                }
            });

            // Turn on or off user's own audio.
            muteSelf.onValueChanged.AddListener(mute =>
            {
                AddInfo($"MuteLocalAudio {mute}");
                if (mute)
                {
                    RtcService.MuteLocalAudio(RtcMuteState.On);
                }
                else
                {
                    RtcService.MuteLocalAudio(RtcMuteState.Off);
                }
                AddInfo($"MuteLocalAudio {mute} Done");
            }); 
        }


        /// <summary>
        /// Output text on canvas.
        /// </summary> 
        public void AddInfo(string info)
        {
            Info.text += $"{info}\n";
        }


        /// <summary>
        /// Get pico account user message.
        /// </summary>
        public void Login()
        { 
            UserService.GetLoggedInUser().OnComplete
            (
                msg =>
                {
                    if (msg.IsError)
                    {
                        AddInfo($"LoginFailed:code={msg.GetError().Code} message={msg.GetError().Message}");
                        return;
                    }
                    User me = msg.Data;
                    if (me == null)
                    { 
                        AddInfo($"Get User Failed:{me}");
                    }
                    else
                    {
                        MyUser = me;
                        AddInfo("Login Succeed!!");
                        AddInfo($"name:{me.DisplayName} \nId:{me.ID}");
                    }
                }
            );
        }


        /// <summary>
        /// Init RTC.
        /// </summary>
        public void InitRtc()
        {
            var res = RtcService.InitRtcEngine();
            if (res != RtcEngineInitResult.Success)
            {
                AddInfo($"Init RTC Engine Failed:{res}");
                throw new UnityException($"Init RTC Engine Failed:{res}");
            }
            RtcService.EnableAudioPropertiesReport(2000);
            Login();
        }


        /// <summary>
        /// Initialize the plateformSDK.
        /// </summary>
        public void Init()
        { 
            CoreService.AsyncInitialize("81e6b29509fad6ee4cb9edb6b4e49d22").OnComplete(m =>
            {
                if (m.IsError)
                {
                    AddInfo($"Init PlatformSdk failed:code={m.Error.Code},message={m.Error.Message}");
                    return;
                }

                if (m.Data == PlatformInitializeResult.Success || m.Data == PlatformInitializeResult.AlreadyInitialized)
                {
                    AddInfo($"Init PlatformSdk successfully");
                    // After init success, init the RTC.
                    InitRtc();
                }
                else
                {
                    AddInfo($"Init PlatformSdk failed:{m.Data}");
                }
            });
        }


        /// <summary>
        /// Judge the roomID is valid or not.
        /// </summary> 
        private bool CheckRoomId()
        {
            if(String.IsNullOrWhiteSpace(inputRoomId.text))
            {
                AddInfo("Please input room id");
                return false;
            }
            return true;
        }


        /// <summary>
        /// Click the "JoinRoom" button to join room.
        /// </summary>
        public void OnClickJoinRoom()
        {
            if (!CheckRoomId())
                return;
            var userId = MyUser.ID;
            var roomId = inputRoomId.text;
            AddInfo($"userId={userId} roomId={roomId} ");
            var privilege = new Dictionary<RtcPrivilege, int>();
            privilege.Add(RtcPrivilege.PublishStream, 3600 * 2);
            privilege.Add(RtcPrivilege.SubscribeStream, 3600 * 2);
            RtcService.GetToken(roomId, userId, 3600 * 2, privilege).OnComplete(msg =>
            {
                if (msg.IsError)
                {
                    AddInfo($"Get rtc token failed: code={msg.GetError().Code} message={msg.GetError().Message}");
                    return;
                }
                var token = msg.Data;
                int result = RtcService.JoinRoom(roomId, userId, token, RtcRoomProfileType.Communication, true);
                AddInfo($"Join Room Result={result} RoomId={roomId}");
                AddInfo("Join Room Success");
            }
            );
        }


        /// <summary>
        /// Callback of JoinRoom to get RtcJoinRoomResult.
        /// </summary> 
        private void OnJoinRoom(Message<RtcJoinRoomResult> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                var joinRoomResult = msg.Data;
                var roomId = "";
                if (joinRoomResult != null)
                {
                    roomId = joinRoomResult.RoomId;
                }

                AddInfo($"[JoinRoomError] code={err.Code} message={err.Message} roomId={roomId}");
                return;
            }
            // Open audio capture.
            RtcService.StartAudioCapture();
            // Open audio publish.
            RtcService.PublishRoom(inputRoomId.text);
            var rtcJoinRoomResult = msg.Data;
            if (rtcJoinRoomResult.ErrorCode != 0)
            {
                AddInfo($"[JoinRoomError] code={rtcJoinRoomResult.ErrorCode} RoomId={rtcJoinRoomResult.RoomId} UserId={rtcJoinRoomResult.UserId}");
                return;
            }
            
            AddInfo($"[JoinRoomOk] Elapsed:{rtcJoinRoomResult.Elapsed} JoinType:{rtcJoinRoomResult.JoinType} RoomId:{rtcJoinRoomResult.RoomId} UserName:{rtcJoinRoomResult.UserId}");
        }


        /// <summary>
        /// Click the "LeaveRoom" button to leave room.
        /// </summary>
        public void OnClickLeaveRoom()
        {
            int result = RtcService.LeaveRoom(inputRoomId.text);
            AddInfo($"[LeaveRoomResult]={result},RoomId={inputRoomId.text}");
        }


        /// <summary>
        /// Clear the text.
        /// </summary>
        public void ClearText()
        {
            Info.text = "";
        }


        /// <summary>
        /// Callback function When user leave room.
        /// </summary> 
        private void OnLeaveRoom(Message<RtcLeaveRoomResult> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                AddInfo($"[LeaveRoomResult]code={err.Code} message={err.Message}");
                return;
            }

            var res = msg.Data;
            // Stop the user audio capture.
            RtcService.StopAudioCapture();
            // Stop the audio publish.
            RtcService.UnPublishRoom(inputRoomId.text);
            AddInfo($"[LeaveRoomOk]RoomId={res.RoomId}");
        }


        /// <summary>
        /// Callback function for user leaving the room.
        /// </summary> 
        private void OnUserLeaveRoom(Message<RtcUserLeaveInfo> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                AddInfo($"[UserLeave] Error {err.Code} {err.Message}");
                return;
            }

            var res = msg.Data;
            AddInfo($"[UserLeave]User[{res.UserId}] left room[{res.RoomId}],offline reason£º{res.OfflineReason}");
        }


        /// <summary>
        /// Callback function for user joining the room.
        /// </summary> 
        private void OnUserJoinRoom(Message<RtcUserJoinInfo> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                AddInfo($"[UserJoin] Error {err.Code} {err.Message}");
                return;
            }

            var res = msg.Data;
            AddInfo($"[UserJoin] user={res.UserId} join room={res.RoomId},UserExtra={res.UserExtra},TimeElapsed{res.Elapsed}");
        }


        /// <summary>
        /// Callback function when room state is updated.
        /// </summary> 
        private void OnRoomStats(Message<RtcRoomStats> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                AddInfo($"[RoomStats] Error {err.Code} {err.Message}");
                return;
            }

            if (Time.realtimeSinceStartup - lastRoomStatsTime < 10)
            {
                return;
            }

            lastRoomStatsTime = Time.realtimeSinceStartup;
            var res = msg.Data;
            AddInfo($"[RoomStats] RoomId={res.RoomId} UserCount={res.UserCount} Duration={res.TotalDuration}");
        }


        /// <summary>
        /// Set the callback to get warning messages from the RTC engine.
        /// </summary> 
        private void OnWarn(Message<int> msg)
        {
            if(msg.IsError)
            {
                var err = msg.GetError();
                AddInfo($"[RtcWarn]  Error {err.Code} {err.Message}");
                return;
            }
            AddInfo($"[RtcWarn] {msg.Data}");
        }


        /// <summary>
        /// Set the callback to get error messages from the RTC engine.
        /// </summary> 
        private void OnError(Message<int> msg)
        {
            if(msg.IsError)
            {
                var err = msg.GetError();
                AddInfo($"[EtcError] Error {err.Code} {err.Message}");
                return;
            }
            AddInfo($"[RtcError] {msg.Data}");
        }


        /// <summary>
        /// Set the callback to get warning messages from the room.
        /// </summary> 
        private void OnRoomWarn(Message<RtcRoomWarn> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                AddInfo($"[RoomWarn] Error {err.Code} {err.Message}");
                return;
            }
            var e = msg.Data;
            AddInfo($"[RtcRoomWarn] RoomId={e.RoomId} Code={e.Code}");
        }


        /// <summary>
        /// Set the callback to get error messages from the room.
        /// </summary> 
        private void OnRoomError(Message<RtcRoomError> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                AddInfo($"[RoomError] Error {err.Code} {err.Message}");
                return;
            }
            var e = msg.Data;
            AddInfo($"[RtcRoomError] RoomId={e.RoomId} Code={e.Code}");
        }


        /// <summary>
        /// Set the callback to get notified when the state of the connection to the RTC server has changed.
        /// </summary> 
        private void OnConnectionStateChange(Message<RtcConnectionState> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                AddInfo($"[ConnectionStateChange] Error {err.Code} {err.Message}");
                return;
            }
            AddInfo($"[ConnectionStateChange] {msg.Data}");
        }


        /// <summary>
        /// Set the callback to get notified when the user has muted local audio.
        /// </summary> 
        private void OnUserMuteAudio(Message<RtcMuteInfo> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                AddInfo($"[UserMuteAudio] Error {err.Code} {err.Message}");
                return;
            }
            var d = msg.Data;
            AddInfo($"[UserMuteAudio] userId={d.UserId} muteState={d.MuteState}");
        }


        /// <summary>
        /// Callback function when user start audio capture.
        /// </summary> 
        private void OnUserStartAudioCapture(Message<string> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                AddInfo($"[UserStartAudioCapture] Error {err.Code} {err.Message}");
                return;
            }
            var d = msg.Data;
            AddInfo($"[UserStartAudioCapture] UserId={d}");
        }


        /// <summary>
        /// Callback function when user stop audio capture.
        /// </summary> 
        private void OnUserStopAudioCapture(Message<string> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                AddInfo($"[UserStopAudioCapture] Error {err.Code} {err.Message}");
                return;
            }
            var d = msg.Data;
            AddInfo($"[UserStopAudioCapture] UserId={d}");
        }


        /// <summary>
        /// Callback function to receive local audio report.
        /// </summary> 
        private void OnLocalAudioPropertiesReport(Message<RtcLocalAudioPropertiesReport> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                AddInfo($"[LocalAudioPropertiesReport] Error {err.Code} {err.Message}");
                return;
            }
            var d = msg.Data;
            StringBuilder builder = new StringBuilder();
            foreach (var i in d.AudioPropertiesInfos)
            {
                builder.Append(i.AudioPropertyInfo.Volume).Append(",");
            }

            AddInfo($@"[LocalAudioPropertiesReport] {d.AudioPropertiesInfos.Length} LocalVolume={builder}");
        }


        /// <summary>
        /// Callback function to receive remote audio report.
        /// </summary> 
        private void OnRemoteAudioPropertiesReport(Message<RtcRemoteAudioPropertiesReport> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                AddInfo($"[RemoteAudioPropertiesReport] Error {err.Code} {err.Message}");
                return;
            }
            var d = msg.Data;
            StringBuilder builder = new StringBuilder();
            foreach (var usr in d.AudioPropertiesInfos)
            {
                if (usr.AudioPropertiesInfo.Volume > 5)
                {
                    builder.Append($"user={usr.StreamKey.UserId} roomId={usr.StreamKey.RoomId} volume={usr.AudioPropertiesInfo.Volume} ");
                }
            }

            var report = builder.ToString();
            AddInfo($@"[RemoteAudioPropertiesReport]totalVolume={d.TotalRemoteVolume} {d.AudioPropertiesInfos.Length}{report}");
        }
    }
}


