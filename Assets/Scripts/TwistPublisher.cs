using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TwistPublisher : MonoBehaviour
{
    public string restApi = "http://13.208.62.139:8000";
    public string scope = "/simu";
    public string driveTopic = "/rt/turtle1/cmd_vel";
    public float linearScale = 0.5f;
    public float angularScale = 0.5f;
    public enum DriveMode { Keyboard, Joystick };
    public DriveMode driveMode = DriveMode.Keyboard;

    void Update()
    {
        if (driveMode == DriveMode.Keyboard)
        {
            UpdateKeyboard();
        }
        else if (driveMode == DriveMode.Joystick)
        {
            UpdateJoystick();
        }
    }

    private void UpdateKeyboard()
    {
        // 監視したいキーボードの入力を指定
        float newLinear = 0.0f;
        float newAngular = 0.0f;

        float moveDirection = Input.GetAxis("Vertical");
        if (moveDirection > 0)
        {
            newLinear = 1.0f;
        }
        else if (moveDirection < 0)
        {
            newLinear = -1.0f;
        }else
        {
            newLinear = 0.0f;
        }

        float turnDirection = Input.GetAxis("Horizontal");
        if (turnDirection < 0)
        {
            newAngular = 1.0f;
        }
        else if (turnDirection > 0)
        {
            newAngular = -1.0f;
        } else
        {
            newAngular = 0.0f;
        }

        PublishTwist(newLinear, newAngular);
    }

    private void UpdateJoystick()
    {
        float newLinear = Input.GetAxis("MetaVertical");
        float newAngular = Input.GetAxis("MetaHorizontal");
        // Debug.Log(newLinear + " " + newAngular);

        PublishTwist(newLinear, -newAngular);
    }

    public void PublishTwist(float linear, float angular)
    {
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

    private IEnumerator SendTwistRequest(string keyExpr, byte[] twistData)
    {
        string url = restApi + keyExpr;
        using UnityWebRequest request = new UnityWebRequest(url, "PUT"); // メモリリークが起こるので先頭にusing
        request.uploadHandler = new UploadHandlerRaw(twistData);
        request.uploadHandler.contentType = "application/octet-stream";
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Debug.Log("Sent cmd_vel to " + url);
        }
        else
        {
            Debug.LogError("Failed to send cmd_vel: " + request.error);
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
