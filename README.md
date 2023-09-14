# Unity-DDS-bridge
This repository provides the ability to send robot operation commands from Unity via Zenoh. to send data from Unity to Zenoh for use in ROS2, a process is required to convert the data to the DDS protocol, which is realized using CSCDR.

## Settings
- First of all, you need to deploy the Zenoh router on your cloud server. See below.

  https://trello.com/c/lVtw0CRt/51-zenoh-router%E3%81%AE%E5%BB%BA%E3%81%A6%E6%96%B9

- Install webserver plugin on Zenoh router referring to the following article.

  https://trello.com/c/qiWscnCv/50-%E9%81%A0%E9%9A%94%E6%93%8D%E4%BD%9C%E7%94%A8web%E3%82%A2%E3%83%97%E3%83%AA

- Install zenoh-dds-bridge on robot side.

  https://trello.com/c/0h6WmO6H/52-zenoh-bridge-dds%E3%81%AE%E5%BB%BA%E3%81%A6%E6%96%B9

## How to use

*Zenoh router*

```
zenohd -P webserver:/usr/lib/libzplugin_webserver.so --cfg "plugins/webserver:{http_port:8080,}" --cfg "plugins/rest:{http_port:8000,}"
```

*Robot*
```
zenoh-bridge-dds -e tcp/<cloud_ip>:7447 -m client --rest-http-port 8000 --scope "<simu>"
```
Refer to the following [link](https://trello.com/c/vDoDqjL4/53-zenoh%E3%82%92%E7%94%A8%E3%81%84%E3%81%9F%E4%BD%8E%E9%81%85%E5%BB%B6%E6%98%A0%E5%83%8F%E9%85%8D%E4%BF%A1) to obtain c1.py.
```
python3 c1.py -m client -e tcp/<cloud_ip>:7447 -k "<scope_name>" -n <camera id>
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
