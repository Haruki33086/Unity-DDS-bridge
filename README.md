# Unity-DDS-bridge
This repository provides the ability to send robot operation commands from Unity via Zenoh. to send data from Unity to Zenoh for use in ROS 2, a process is required to convert the data to the DDS protocol, which is realized using CSCDR.

## Settings
- First of all, you need to deploy the Zenoh router on your cloud server. See below. You need to open ports 7447, 8000, and 8080 of the server.

- Install webserver plugin on Zenoh router referring to [link](https://github.com/eclipse-zenoh/zenoh-plugin-webserver).

- Install zenoh-dds-bridge on robot side reffering to [link](https://github.com/eclipse-zenoh/zenoh-plugin-dds).

## How to use

*Zenoh router*

```
zenohd --cfg "plugins/webserver:{http_port:8080,}" --cfg "plugins/rest:{http_port:8000,}"
```

*Robot*
```
zenoh-bridge-dds -e tcp/<cloud_ip>:7447 -m client --rest-http-port 8000 --scope "<scope_name>"
```
Refer to the following [link]([https://trello.com/c/vDoDqjL4/53-zenoh%E3%82%92%E7%94%A8%E3%81%84%E3%81%9F%E4%BD%8E%E9%81%85%E5%BB%B6%E6%98%A0%E5%83%8F%E9%85%8D%E4%BF%A1](https://github.com/eclipse-zenoh/zenoh-demos/blob/master/computer-vision/zcam/zcam-python/zcapture.py)) to obtain zcapture.py.
```
python3 zcapture.py -m client -e tcp/<cloud_ip>:7447 -k "<scope_name>"
```
Receive cmd_vel to allow the robot to run.

*Operator*
- Open this repository in Unity.

- Change restApi, scope and driveTopic in PubTwist.cs to match your environment. However, do not forget to prefix the topic name with "\rt".

- You can make the robot run by pressing the Arrow key on your keyboard.

## How to introduce to MetaQuest

- Set up your project on Unity for MetaQuest, referring to [this site](https://note.com/npaka/n/n749a134d0c11)

- Refer to [this site](https://tech.framesynthesis.co.jp/unity/metaquest/) to enable the use of the controller and camera.
  Find the OVRCameraRig in Assets/Oculus/VR/Prefabs and place it in the Hierarchy. However, delete the existing Camera in the Hierarchy at this time.

  Next, find OVRControllerPrefabs from where OVRCameraRig was located and attach the Controller to the LeftHandAnchor and RightHandAnchor of OVRCameraRig in the Hierarchy. At this time, apply L Touch to the controller for the left hand and R Touch to the controller for the right hand.

  Finally, select Canvas in the Hierarchy, set the Render Mode of Canvas in the Inspector to World Space, and apply OVRCameraRig's CenterEyeAnchor to the Event Camera.

## References

- [zenoh](https://github.com/eclipse-zenoh/zenoh)

- [zenoh-plugin-dds](https://github.com/eclipse-zenoh/zenoh-plugin-dds)

- [zenoh-demos](https://github.com/eclipse-zenoh/zenoh-demos)

- [zenoh-plugin-webserver](https://github.com/eclipse-zenoh/zenoh-plugin-webserver)

- [CSCDR](https://github.com/atolab/cscdr)

- [MJPEGStreamDecoder.cs](https://gist.github.com/lightfromshadows/79029ca480393270009173abc7cad858)

- [はじめての Oculus Quest アプリの作成](https://note.com/npaka/n/n749a134d0c11)

- [Unity + Meta Quest開発メモ](https://tech.framesynthesis.co.jp/unity/metaquest/)
