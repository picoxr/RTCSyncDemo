using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEngine.Assertions;
using AOT;
using UnityEditor;
using System.IO;
using UnityEngine.UI; 

using Pico.Platform;
using Pico.Platform.Models;


namespace Pico.Platform.Samples.Game
{
    

    public class RoomAndMatchmakingEntry : MonoBehaviour
    {
        public enum CurState
        {
            None,
            Inited,
            EnqueueSend,
            EnqueueResultRecved,
            MatchmakingFound,
            RoomJoinSend,
            RoomJoined,
            RoomUpdatingSend,
            RoomUpdatingRecved,
            RoomGetCurrentSend,
            RoomLeaveSend,
            RoomLeaveRecved,
            SimplestTestEnd,
        }
        public Text ExecuteResult;
        public Text RoomMsg;
        public Button InitBtn;
        public Button InitGameBtn;
        public Button UninitBtn;
        public Button LeaveBtn;
        public Button SendMessageBtn;
        public Button StartBtn;
        public Button loginBtn;
        public Button bindBtn;
        public static string accessToken = "token:null";
        public static User myUser = null;

        public InputField PoolName;
        public const int RoomMaxMsgNum = 99;
        public InputField TestMsg;
        public static Room currentRoom;

        public string roomStates;
        public string curRoomStates;


        int messageNum = 0;
        int logMaxCount = 10;
        int logCount = 0;
        public CurState curState = CurState.None;
        public const string TAG = "RoomAndMatchmakingDemo";
        Models.Room matchRoom;
        
        void Start()
        {
            SetExecuteResult("Start");

            InitBtn.onClick.AddListener(OnInitBtnClick);
            StartBtn.onClick.AddListener(StartEnqueue);
            UninitBtn.onClick.AddListener(OnUninitBtnClick);
            LeaveBtn.onClick.AddListener(OnLeaveBtnClick);
            SendMessageBtn.onClick.AddListener(OnSendMessageBtnClick);
            loginBtn.onClick.AddListener(UserLogin);
            bindBtn.onClick.AddListener(BindConnection);
            InitGameBtn.onClick.AddListener(InitGame);
            

            MatchmakingService.SetMatchFoundNotificationCallback(ProcessMatchmakingMatchFound);
            NetworkService.SetNotification_Game_ConnectionEventCallback(OnGameConnectionEvent);
        }
        void OnGameConnectionEvent(Message<GameConnectionEvent> msg)
        {
            var state = msg.Data;
            LogHelper.LogInfo(TAG, $"OnGameConnectionEvent: {state}");
            if (state == GameConnectionEvent.Connected)
            {
                LogHelper.LogInfo(TAG, "GameConnection: success！");
            }
            else if (state == GameConnectionEvent.Closed)
            {
                Uninitialize();
                LogHelper.LogInfo(TAG, "GameConnection: fail！Please re-initialize！");
            }
            else if (state == GameConnectionEvent.GameLogicError)
            {
                Uninitialize();
                LogHelper.LogInfo(TAG, "GameConnection: fail！After successful reconnection, the logic state is found to be wrong，Please re-initialize！");
            }
            else if (state == GameConnectionEvent.Lost)
            {
                LogHelper.LogInfo(TAG, "GameConnection: Reconnecting, please wait！");
            }
            else if (state == GameConnectionEvent.Resumed)
            {
                LogHelper.LogInfo(TAG, "GameConnection: successful reconnection！");
            }
            else if (state == GameConnectionEvent.KickedByRelogin)
            {
                Uninitialize();
                LogHelper.LogInfo(TAG, "GameConnection: Repeat login! Please reinitialize！");
            }
            else if (state == GameConnectionEvent.KickedByGameServer)
            {
                Uninitialize();
                LogHelper.LogInfo(TAG, "GameConnection: Server kicks people! Please reinitialize！");
            }
            else
            {
                LogHelper.LogInfo(TAG, "GameConnection: unknown error！");
            }
        }
        void OnStartBtnClick()
        {
            curState = CurState.Inited;
        }
        // Initialize
        void OnInitBtnClick()
        {
            SetExecuteResult("Start initialize");
            CoreService.Initialize("ffadf96b1b525aaa77bacf9562e6c828");
            if (!CoreService.Initialized)
            {
                SetExecuteResult("pico initialize failed");
                return;
            }
            SetRoomMsg($"CoreService.IsInitialized() {CoreService.IsInitialized()}");

            UserService.GetAccessToken().OnComplete(delegate (Message<string> message)
            {
                if (message.IsError)
                {
                    var err = message.GetError();
                    SetExecuteResult($"Got access token error {err.Message} code={err.Code}");
                    return;
                }

                accessToken = message.Data;
                SetExecuteResult($"Got access token: {accessToken}, GameInitialize begin");
                
            });
        }

        private void InitGame()
        {
            CoreService.GameUninitialize();
            CoreService.GameInitialize(accessToken).OnComplete(OnGameInit);
        }

        private void OnGameInit(Message<GameInitializeResult> msg)
        {
            if (msg == null)
            {
                SetRoomMsg("Fail msg = null");
                return;
            }

            if (msg.Data == GameInitializeResult.Success)
            {
                SetRoomMsg("OnGameInitialize Success");
            }
            else
            {
                CoreService.GameUninitialize();
                SetRoomMsg("GameInitialize Fail");
            }
        }

        // Uninitialize
        void OnUninitBtnClick()
        {
            Uninitialize();
        }
        void Uninitialize()
        {
            CoreService.GameUninitialize();
        }
        void OnDestroy()
        {
            Uninitialize();
        }
        void Update()
        {
            if (CoreService.Initialized)
            {
                // Loop to check the current state
                CheckState();
            }
        }

        public void Login()
        {
            UserService.GetLoggedInUser().OnComplete
            (
                msg=>
                {
                    if(msg.IsError)
                    {
                        SetExecuteResult("LoginFailed");
                        return;
                    }
                    User me = msg.Data;
                    if(me==null)
                    {
                        string errorMsg = "Failed to get logged in user: user is null";
                        SetExecuteResult(errorMsg);
                    }
                    else
                    {
                        myUser = me;
                        SetExecuteResult("登录成功");
                        SetExecuteResult($"name:{me.DisplayName}\n Id:{me.ID}");
                    }
                }
            );
        }


        private void CheckState()
        {
            TestMatchmakingAndRoom();
        }
        void TestMatchmakingAndRoom()
        {
            switch (curState)
            {
                case CurState.Inited:
                    StartEnqueue();
                    break;
                case CurState.MatchmakingFound:
                    StartJoinRoom();
                    break;
                case CurState.RoomJoinSend:
                    break;
                case CurState.RoomJoined:
                    StartRoomUpdating();
                    break;
                case CurState.RoomUpdatingSend:
                    break;
                case CurState.RoomUpdatingRecved:
                    break;
                case CurState.RoomGetCurrentSend:
                    break;
                case CurState.RoomLeaveSend:
                    break;
                case CurState.RoomLeaveRecved:
                    EndTest();
                    break;
                default:
                    break;
            }
        }
        // enter the match queue

        void BindConnection()
        {
            SetRoomMsg("BindCommection");
            RoomService.SetUpdateOwnerNotificationCallback(OwnerNotificationCallback);
            RoomService.SetUpdateNotificationCallback(OnRoomUpdate);
            RoomService.SetJoin2NotificationCallback(OnRoomUpdate);
            RoomService.SetLeaveNotificationCallback(OnRoomUpdate);
            RoomService.SetKickUserNotificationCallback(OnRoomUpdate);
            MatchmakingService.SetCancel2NotificationCallback(Cancel2NotificationCallback);
            MatchmakingService.SetMatchFoundNotificationCallback(MatchFoundNotificationCallback);
        }

        private void OwnerNotificationCallback(Message message)
        {
            SetRoomMsg("OwnerNotificationCallback");
            if (message.IsError)
            {
                SetExecuteResult($"OwnerNotificationCallback Error {message.Error.Code} {message.Error.Message}");
                return;
            }

            SetExecuteResult($"OwnerNotificationCallback Success");
        }

        private void OnRoomUpdate(Message<Room> msg)
        { 
            SetRoomMsg("OnRoomUpdate");
            SetRoomMsg(Time.time.ToString());
            if (msg.IsError)
            { 
                SetRoomMsg($"\n{msg.Error.Code} {msg.Error.Message}");
                return;
            }

            currentRoom = msg.Data;

            if (msg.Data == null)
            { 
                SetRoomMsg($"\n Data null");
                return;
            }
             
            SetRoomMsg($"\nroomId{currentRoom.RoomId}");
            SetRoomMsg("\nowener:" + currentRoom.OwnerOptional);
            if (msg.Data.UsersOptional == null)
            {
                SetRoomMsg($"\n UsersOptional null"); 
                return;
            }

            if (msg.Data.UsersOptional.Count <= 0)
            {
                SetRoomMsg($"\n UsersOptional.Count 0"); 
                return;
            }
            
            msg.Data.UsersOptional.ForEach(u => { SetRoomMsg($"\nname:{u.DisplayName}\n id:{u.ID}"); });
        }

        private void Cancel2NotificationCallback(Message message)
        {
            SetRoomMsg("Cancel2NotificationCallback");
            if (message.IsError)
            {
                SetExecuteResult($"Cancel2NotificationCallback Error {message.Error.Code} {message.Error.Message}");
                return;
            }

            SetExecuteResult($"Cancel2NotificationCallback Success");
        }

        private void MatchFoundNotificationCallback(Message<Room> msg)
        {
            SetRoomMsg("MatchFoundNotificationCallback");
            SetExecuteResult("----");
            if (msg.IsError)
            {
                SetExecuteResult($"[Error] {msg.Error.Code} {msg.Error.Message}");
                SetExecuteResult("----");
                return;
            }

            currentRoom = msg.Data;
            SetExecuteResult($"OnCreateRoomComplete \nroomId:{currentRoom.RoomId}");
            currentRoom.UsersOptional.ForEach(u =>
            {
                SetExecuteResult("[name]" + u.DisplayName);
                SetExecuteResult("[ID]" + u.ID);
            });
            SetExecuteResult("----");
        }

        void StartEnqueue()
        {
            MatchmakingRoom();
            /*SetExecuteResult($"enter the match queue...PoolName:{PoolName.text}");
            MatchmakingOptions options = new MatchmakingOptions();
            options.SetCreateRoomMaxUsers(2);
            var rst = MatchmakingService.Enqueue2(PoolName.text, options).OnComplete(ProcessMatchmakingEnqueue);
            var result = rst.TaskId;
            SetExecuteResult("enter the match queue result = " + result);
            if (0 != result)
            {
                SetExecuteResult("set current state：EnqueueSend");
                curState = CurState.EnqueueSend;
            }*/
        }

        public void MatchmakingRoom()
        {
            var options = new MatchmakingOptions();
            options.SetCreateRoomMaxUsers(2);
            options.SetCreateRoomJoinPolicy(RoomJoinPolicy.Everyone);
            options.SetEnqueueIsDebug(true);

            MatchmakingService.Enqueue2("thedashboardofapi", options).OnComplete(msg =>
            {
                if (msg.IsError)
                {
                    SetExecuteResult($"加入匹配队列失败 {msg.Error.Code} {msg.Error.Message}");
                    return;
                }

                SetExecuteResult("加入匹配队列成功");
            });
            SetExecuteResult("matchmakingroom");
        }


        void UserLogin()
        {
            SetExecuteResult("Logining");

            //oculus is always logged in!
            UserService.GetLoggedInUser().OnComplete
            (
                msg =>
                {
                    if (msg.IsError)
                    {
                        SetExecuteResult("LoginFailed");
                        return;
                    }

                    User me = msg.Data;
                    if (me == null)
                    {
                        string errorMsg = "Failed To Get Logged In User. User is null";
                        SetExecuteResult(errorMsg);
                    }
                    else
                    {
                        myUser = me;
                        SetExecuteResult("登录成功！！");
                        SetExecuteResult($"name:{me.DisplayName} \nId:{me.ID}");
                    }
                }
            );
        }
        // enter the room
        void StartJoinRoom()
        {
            SetExecuteResult("StartJoinRoom...");
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.SetTurnOffUpdates(false); // receive updates
            roomOptions.SetRoomId(matchRoom.RoomId);
            SetExecuteResult($"enter the room Room.ID : {matchRoom.RoomId}");
            var rst = RoomService.Join2(matchRoom.RoomId, roomOptions).OnComplete(ProcessRoomJoin2);
            var result = rst.TaskId;
            SetExecuteResult("enter the room result = " + result);
            if (0 != result)
            {
                SetExecuteResult("set current state：RoomJoinSend");
                curState = CurState.RoomJoinSend;
            }
        }
        // In room
        void StartRoomUpdating()
        {
            // SetExecuteResult("StartRoomUpdating...");
            if (messageNum > RoomMaxMsgNum)
            {
                SetExecuteResult("The number of messages in the room is greater than the maximum value, leave the room");
                StartRoomLeave();
                return;
            }
            // read packet
            var packet = NetworkService.ReadPacket();
            while (packet != null)
            {
                var sender = packet.SenderId;
                ++messageNum;
                var bytes = new byte[packet.Size];
                var bytesSize = packet.GetBytes(bytes);
                string str = System.Text.Encoding.UTF8.GetString(bytes);

                SetExecuteResult($"Read in-room messages: sender: {sender}, content: {str}");
                packet.Dispose();
                packet = NetworkService.ReadPacket();
            }
        }
        // Click to leave the room
        void OnLeaveBtnClick()
        {
            StartRoomLeave();
        }
        // Click to send message
        void OnSendMessageBtnClick()
        {
            byte[] decBytes = System.Text.Encoding.UTF8.GetBytes(TestMsg.text);
            NetworkService.SendPacketToCurrentRoom(decBytes);
        }
        // leave the room
        void StartRoomLeave()
        {
            SetExecuteResult("leave the room...");
            SetExecuteResult("roomId = " + matchRoom.RoomId);
            var rst = RoomService.Leave(matchRoom.RoomId).OnComplete(ProcessRoomLeave);
            var result = rst.TaskId;
            SetExecuteResult("leave the room result = " + result);
            if (0 != result)
            {
                curState = CurState.RoomLeaveSend;
                SetExecuteResult("set current state：RoomLeaveSend");
            }
        }
        // end
        void EndTest()
        {
            curState = CurState.SimplestTestEnd;
            SetExecuteResult("End test！set current state：SimplestTestEnd! Test succeed! \n");
        }

        
        // ----Response or Notification----
        // Join the matching queue callback
        void ProcessMatchmakingEnqueue(Message<MatchmakingEnqueueResult> message)
        {
            if (!message.IsError)
            {
                var result = message.Data;
                curState = CurState.EnqueueResultRecved;
                SetExecuteResult($"Join the matching queue pool name: {result.Pool}，set current state: EnqueueResultRecved");
            }
            else
            {
                var error = message.GetError();
                SetExecuteResult($"Join the matching queue error : {error.Message}");
            }
        }
        // match found
        void ProcessMatchmakingMatchFound(Message<Models.Room> message)
        {
            if (!message.IsError)
            {
                matchRoom = message.Data;
                SetExecuteResult("match found Room.ID: " + matchRoom.RoomId);
                curState = CurState.MatchmakingFound;
            }
            else
            {
                var error = message.GetError();
                SetExecuteResult($"match found error : {error.Message}");
            }
        }
        // join room
        void ProcessRoomJoin2(Message <Models.Room>message)
        {
            if (!message.IsError)
            {
                var room = message.Data;
                curState = CurState.RoomJoined;
                SetExecuteResult($"join room Room.ID: {room.RoomId}, set current state: RoomJoined");
            }
            else
            {
                var error = message.GetError();
                SetExecuteResult($"join room error: {error.Message}");
            }
        }
        // leave room
        void ProcessRoomLeave(Message<Models.Room> message)
        {
            if (!message.IsError)
            {
                var room = message.Data;
                curState = CurState.RoomLeaveRecved;
                SetExecuteResult($"leaven room Room.ID: {room.RoomId}, set current state: RoomLeaveRecved");
            }
            else
            {
                var error = message.GetError();
                SetExecuteResult($"leave room error: {error.Message}");
            }
        }

        void SetExecuteResult(string result)
        {
            logCount++;
            LogHelper.LogInfo(TAG, result);
            /*if (logCount > logMaxCount)
                ExecuteResult.text = "";*/
            ExecuteResult.text = result + "\n" + ExecuteResult.text;
        }

        void SetRoomMsg(string result)
        {
            LogHelper.LogInfo("UserLoginTest", result);
            RoomMsg.text = result + "\n" + RoomMsg.text;
        }
    }
}

