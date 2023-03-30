# RTCSyncDemo

- If you have any questions/comments, please visit [**Pico Developer Support Portal**](https://picodevsupport.freshdesk.com/support/home) and raise your question there. 

## Environment：

- Unity 2020.3.431f1
- Pico Unity Integration SDK v2.1.2

## Applicable devices:

- Neo 3 series, PICO 4

## Description：

-  This demo shows how to use the Pico Unity Platform SDK to implement Real-Time Communication. 

​	![screenshot](https://github.com/picoxr/RTCSyncDemo/blob/main/Assets/Screenshot/screenshot%201.jpg)

Here is the instruction of RTC demo:
- **Note: ** 
1. In order to test the Platform SDK you will need an AppID after creating a game on our Developer portal
2. Use the AppID in the PXR_SDK->PlatformSetting->User Entitlement Check
3. Enable Entitlement Check Simulation
4. Add your Device Serial Number(Found in Headset under Settings->General->About->Device Serial Number)
5. Build the project to your headset to test since it will require your logged in Pico Account for the Login step

- **Login：** click to initialize the Pico platform SDK, initialize the RTC engine and login user account.

- **RoomId：** enter the room id you want to enter or create (note: only user inputs same room id can join the same room).

- **JoinRtcRoom:** join the room and open the audio.

- **MuteOthers:** receive or not to receive the sound of everyone else in the room.

- **MuteSelf:** make oneself voice able/unable to be heard be others in the same room.

- **LeaveRtcRoom:** leave the room and stop the audio.

- **X:** clear the text.
