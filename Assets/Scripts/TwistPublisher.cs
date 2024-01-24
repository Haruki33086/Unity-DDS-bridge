using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TwistPublisher : MonoBehaviour
{
    public string restApi = "http://<your ip>:8000/";
    public string scope = "simu";
    public string driveTopic = "/rt/turtle1/cmd_vel";
    public string rotationTopic = "/rt/rotaion";
    public float linearScale = 0.5f;  // ロボットの最大速度に合わせて調整
    public float angularScale = 0.5f; // ロボットの最大角速度に合わせて調整
    public float rotaionScale = 0.3f; // ロボットの最大角速度に合わせて調整
    public enum DriveMode { Keyboard, Joystick };
    public DriveMode driveMode = DriveMode.Keyboard;

    private UnityWebRequest request; // UnityWebRequestを保持する変数を追加

    void FixedUpdate()
    {
        if (driveMode == DriveMode.Keyboard)
        {
            UpdateKeyboardInput();
        }
        else if (driveMode == DriveMode.Joystick)
        {
            UpdateJoystickInput();
        }
    }

    private void UpdateKeyboardInput()
    {
        float newLinear = 0.0f;
        float newAngular = 0.0f;
        bool inputDetected = false;

        float moveDirection = Input.GetAxis("Vertical");
        if (moveDirection > 0)
        {
            newLinear = 1.0f;
            inputDetected = true;
        }
        else if (moveDirection < 0)
        {
            newLinear = -1.0f;
            inputDetected = true;
        }

        float turnDirection = Input.GetAxis("Horizontal");
        if (turnDirection < 0)
        {
            newAngular = 1.0f;
            inputDetected = true;
        }
        else if (turnDirection > 0)
        {
            newAngular = -1.0f;
            inputDetected = true;
        }

        if (inputDetected)
        {
            PublishTwist(newLinear, newAngular);
        }
    }

    private void UpdateJoystickInput()
    {
        float newLinear = OVRInput.Get(OVRInput.RawAxis2D.LThumbstick).y;
        float newAngular = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick).x;
        bool newLeftRotation = OVRInput.GetDown(OVRInput.RawButton.LHandTrigger);
        bool newRightRotation = OVRInput.GetDown(OVRInput.RawButton.RHandTrigger);
        bool inputDetected = newLinear != 0.0f || newAngular != 0.0f;
        bool rotaionInputDetected = newLeftRotation || newRightRotation;

        if (inputDetected || rotaionInputDetected)
        {
            PublishTwist(newLinear, -newAngular);
            if (rotaionInputDetected)
            {
                PublishRotation(newLeftRotation, newRightRotation);
            }
        }
    }

    public void PublishTwist(float linear, float angular)
    {
        // 先に実行中のリクエストがある場合、キャンセルしてクリア
        if (request != null && !request.isDone)
        {
            request.Abort();
            request.Dispose();
        }

        // Create a Twist message
        TwistMessage twist = new TwistMessage
        {
            linear = new Vector3 { x = linear * linearScale, y = 0.0f, z = 0.0f },
            angular = new Vector3 { x = 0.0f, y = 0.0f, z = angular * angularScale }
        };

        // Encode the Twist message as binary data
        byte[] twistData = twist.Encode();

        // The key expression for publication
        string keyExpr = scope + driveTopic;

        // Send the Twist message to zenoh via its REST API
        StartCoroutine(SendTwistRequest(keyExpr, twistData));
    }

    public void PublishRotation(bool leftRotation, bool rightRotation)
    {
        // 先に実行中のリクエストがある場合、キャンセルしてクリア
        if (request != null && !request.isDone)
        {
            request.Abort();
            request.Dispose();
        }

        // Create a Twist message
        TwistMessage twist = new TwistMessage
        {
            linear = new Vector3 { x = 0.0f, y = 0.0f, z = 0.0f },
            angular = new Vector3 { x = 0.0f, y = 0.0f, z = 0.0f }
        };

        if (leftRotation)
        {
            twist.angular.z = rotaionScale;
        }
        else if (rightRotation)
        {
            twist.angular.z = -rotaionScale;
        }

        // Encode the Twist message as binary data
        byte[] twistData = twist.Encode();

        // The key expression for publication
        string keyExpr = scope + rotationTopic;

        // Send the Twist message to zenoh via its REST API
        StartCoroutine(SendTwistRequest(keyExpr, twistData));
    }

    private IEnumerator SendTwistRequest(string keyExpr, byte[] twistData)
    {
        string url = restApi + keyExpr;
        request = new UnityWebRequest(url, "PUT");
        request.uploadHandler = new UploadHandlerRaw(twistData);
        request.uploadHandler.contentType = "application/octet-stream";
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Sent cmd_vel to " + url);
        }
        else
        {
            Debug.LogError("Failed to send cmd_vel: " + request.error);
        }
    }

    private void OnDestroy()
    {
        // スクリプトが破棄される際に未完了のリクエストをキャンセルしてクリア
        if (request != null && !request.isDone)
        {
            request.Abort();
            request.Dispose();
        }
    }

    public class Vector3
    {
        public float x;
        public float y;
        public float z;
    }

    public class TwistMessage
    {
        public Vector3 linear;
        public Vector3 angular;

        public byte[] Encode()
        {
            CSCDR.CDRWriter writer = new CSCDR.CDRWriter();

            // Encode linear and angular values
            writer.WriteDouble(linear.x);
            writer.WriteDouble(linear.y);
            writer.WriteDouble(linear.z);
            writer.WriteDouble(angular.x);
            writer.WriteDouble(angular.y);
            writer.WriteDouble(angular.z);

            return writer.GetBuffer().ToArray();
        }
    }
}
