using System;
using System.Collections.Generic;
using System.Text;
using Pico.Platform;
using Pico.Platform.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace Pico.Platform.Samples.RtcDemo
{
    public class RTCScripts : MonoBehaviour
    {
        public Text Info;//³õÊ¼ÎÄ±¾output text
        public static User myUser = null;//get pico account user message
        private InputField InputRoomId;//input room id 
        float _lastRoomStatsTime = 0;//The timestamp of print room stats.Used for control frequency.

        private void Start()
        { 
            var Mute = GameObject.Find("AudioMute").GetComponent<Toggle>(); 
            var MuteOthers = GameObject.Find("MuteOthers").GetComponent<Toggle>(); 

            InputRoomId = GameObject.Find("InputRoomId").GetComponent<InputField>(); 

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
              

            MuteOthers.onValueChanged.AddListener((v) =>
            {
                if (v)
                {
                    //AddInfo($"Mute other player voice");
                    RtcService.RoomPauseAllSubscribedStream(InputRoomId.text);
                    //AddInfo($"StartMuteOthers Done");
                }
                else
                {
                    //AddInfo($"Before ResumeVoiceCapture");
                    RtcService.RoomResumeAllSubscribedStream(InputRoomId.text);
                    //AddInfo($"ResumeAudio Done");
                }
            });

            Mute.onValueChanged.AddListener(mute =>
            {
                //AddInfo($"MuteLocalAudio {mute}");
                if (mute)
                {
                    RtcService.MuteLocalAudio(RtcMuteState.On);
                }
                else
                {
                    RtcService.MuteLocalAudio(RtcMuteState.Off);
                }

                //AddInfo($"MuteLocalAudio {mute} Done");
            }); 
        } 


        //output text on canvas
        public void AddInfo(string info)
        {
            Info.text += $"{info}\n";
        }


        //get pico account user message
        public void Login()
        {
            //oculus is always logged in!
            UserService.GetLoggedInUser().OnComplete
            (
                msg =>
                {
                    if (msg.IsError)
                    {
                        AddInfo("LoginFailed");
                        return;
                    }

                    User me = msg.Data;
                    if (me == null)
                    {
                        string errorMsg = "Failed To Get Logged In User. User is null";
                        AddInfo(errorMsg);
                    }
                    else
                    {
                        myUser = me;
                        AddInfo("Login Succeed£¡£¡");
                        AddInfo($"name:{me.DisplayName} \nId:{me.ID}");
                    }
                }
            );
        }


        //clear the text
        public void ClearText()
        {
            Info.text = "";
        }


        //init RTC
        public void initRtc()
        {
            var res = RtcService.InitRtcEngine();
            if (res != RtcEngineInitResult.Success)
            {
                AddInfo($"Init RTC Engine Failed{res}");
                throw new UnityException($"Init RTC Engine Failed:{res}");
            }
            RtcService.EnableAudioPropertiesReport(2000);
            Login();
        }


        //initialize the plateformSDK
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
                    initRtc();//after init success, init the RTC
                }
                else
                {
                    AddInfo($"Init PlatformSdk failed:{m.Data}");
                }
            });
        }


        //judge the roomID is valid or not
        bool CheckRoomId()
        {
            if(String.IsNullOrWhiteSpace(InputRoomId.text))
            {
                AddInfo("please input room id");
                return false;
            }
            return true;
        }


        //click the "JoinRoom" button to join room
        public void OnClickJoinRoom()
        {
            if (!CheckRoomId())
                return;
            var userId = myUser.ID;
            var roomId = InputRoomId.text;
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


        //when two user match together, call this function
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

                //AddInfo($"[JoinRoomError]code={err.Code} message={err.Message} roomId={roomId}");
                return;
            }
            RtcService.StartAudioCapture();//open audio capture
            RtcService.PublishRoom(InputRoomId.text);//open audio publish 
            var rtcJoinRoomResult = msg.Data;
            if (rtcJoinRoomResult.ErrorCode != 0)
            {
                //AddInfo($"[JoinRoomError]code={rtcJoinRoomResult.ErrorCode} RoomId={rtcJoinRoomResult.RoomId} UserId={rtcJoinRoomResult.UserId}");
                return;
            }
            
            //AddInfo($"[JoinRoomOk] Elapsed:{rtcJoinRoomResult.Elapsed} JoinType:{rtcJoinRoomResult.JoinType} RoomId:{rtcJoinRoomResult.RoomId} UserName:{rtcJoinRoomResult.UserId}");
        }


        //click the "LeaveRoom" button to leave room
        public void OnClickLeaveRoom()
        {
            AddInfo("Click Leave Room Button");
            int result = RtcService.LeaveRoom(InputRoomId.text);
            AddInfo($"[LeaveRoomResult]={result},RoomId={InputRoomId.text}");
        }

        //When user leave room, call this function 
        private void OnLeaveRoom(Message<RtcLeaveRoomResult> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                AddInfo($"[LeaveRoomResult]code={err.Code} message={err.Message}");
                return;
            }

            var res = msg.Data;
            RtcService.StopAudioCapture();//stop the user audio capture
            RtcService.UnPublishRoom(InputRoomId.text);//stop the audio publish 
            AddInfo($"[LeaveRoomOk]RoomId={res.RoomId}");
        }


        private void OnUserLeaveRoom(Message<RtcUserLeaveInfo> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                return;
            }

            var res = msg.Data;
            AddInfo($"[UserLeave]User[{res.UserId}] left room[{res.RoomId}],offline reason£º{res.OfflineReason}");
        }

        private void OnUserJoinRoom(Message<RtcUserJoinInfo> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                return;
            }

            var res = msg.Data;
            AddInfo($"[UserJoin]user={res.UserId} join room={res.RoomId},UserExtra={res.UserExtra},TimeElapsed{res.Elapsed}");
        }

        private void OnRoomStats(Message<RtcRoomStats> msg)
        {
            if (msg.IsError)
            {
                var err = msg.GetError();
                AddInfo($"[RoomStats]Error {err.Code} {err.Message}");
                return;
            }

            if (Time.realtimeSinceStartup - _lastRoomStatsTime < 10)
            {
                return;
            }

            _lastRoomStatsTime = Time.realtimeSinceStartup;
            var res = msg.Data;
            AddInfo($"[RoomStats]RoomId={res.RoomId} UserCount={res.UserCount} Duration={res.TotalDuration}");
        }

        private void OnWarn(Message<int> message)
        {
            AddInfo($"[RtcWarn] {message.Data}");
        }

        private void OnError(Message<int> message)
        {
            AddInfo($"[RtcError] {message.Data}");
        }

        private void OnRoomWarn(Message<RtcRoomWarn> message)
        {
            var e = message.Data;
            AddInfo($"[RtcRoomWarn]RoomId={e.RoomId} Code={e.Code}");
        }

        private void OnRoomError(Message<RtcRoomError> message)
        {
            var e = message.Data;
            AddInfo($"[RtcRoomError]RoomId={e.RoomId} Code={e.Code}");
        }

        private void OnConnectionStateChange(Message<RtcConnectionState> message)
        {
            AddInfo($"[ConnectionStateChange] {message.Data}");
        }

        private void OnUserMuteAudio(Message<RtcMuteInfo> message)
        {
            var d = message.Data;
            AddInfo($"[UserMuteAudio]userId={d.UserId} muteState={d.MuteState}");
        }

        private void OnUserStartAudioCapture(Message<string> message)
        {
            var d = message.Data;
            AddInfo($"[UserStartAudioCapture]UserId={d}");
        }

        private void OnUserStopAudioCapture(Message<string> message)
        {
            var d = message.Data;
            AddInfo($"[UserStopAudioCapture]UserId={d}");
        }

        private void OnLocalAudioPropertiesReport(Message<RtcLocalAudioPropertiesReport> message)
        {
            var d = message.Data;
            StringBuilder builder = new StringBuilder();
            foreach (var i in d.AudioPropertiesInfos)
            {
                builder.Append(i.AudioPropertyInfo.Volume).Append(",");
            }

            AddInfo($@"[LocalAudioPropertiesReport] {d.AudioPropertiesInfos.Length} LocalVolume={builder}");
        }

        private void OnRemoteAudioPropertiesReport(Message<RtcRemoteAudioPropertiesReport> message)
        {
            var d = message.Data;
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


