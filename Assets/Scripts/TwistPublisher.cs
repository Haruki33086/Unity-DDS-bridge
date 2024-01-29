using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class TwistPublisher : MonoBehaviour
{
    public string restApi = "http://<your ip>:8000/";
    public string scope = "simu";
    public string driveTopic = "/rt/turtle1/cmd_vel";
    public string rotationTopic = "/rt/rotaion";
    public float linearScale = 2.0f;  // ロボットの最大速度に合わせて調整
    public float angularScale = 2.0f; // ロボットの最大角速度に合わせて調整
    public float rotationScale = 0.3f; // ロボットの最大角速度に合わせて調整
    public enum DriveMode { Keyboard, Joystick };
    public DriveMode driveMode = DriveMode.Keyboard;

    private UnityWebRequest request; // UnityWebRequestを保持する変数を追加

    private bool wasInputDetectedLastFrame = false; // クラスレベルの変数として追加
    private bool wasRotationDetectedLastFrame = false; // 回転入力の追跡用フラグ

    public TMP_InputField restApiInputField;
    public TMP_InputField scopeInputField;
    public TMP_InputField driveTopicInputField;
    public TMP_InputField rotationTopicInputField;
    public TMP_InputField linearScaleInputField;
    public TMP_InputField angularScaleInputField;
    public TMP_InputField rotationScaleInputField;

    public void UpdateSettings()
    {
        restApi = restApiInputField.text;
        scope = scopeInputField.text;
        driveTopic = driveTopicInputField.text;
        rotationTopic = rotationTopicInputField.text;
        linearScale = float.Parse(linearScaleInputField.text);
        angularScale = float.Parse(angularScaleInputField.text);
        rotationScale = float.Parse(rotationScaleInputField.text);
    }

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

        float moveDirection = Input.GetAxisRaw("Vertical");
        if (moveDirection != 0 && !wasInputDetectedLastFrame)
        {
            newLinear = moveDirection;
            PublishTwist(newLinear, newAngular);
        }

        float turnDirection = Input.GetAxisRaw("Horizontal");
        if (turnDirection != 0 && !wasInputDetectedLastFrame)
        {
            newAngular = -turnDirection;
            PublishTwist(newLinear, newAngular);
        }

        bool inputDetected = moveDirection != 0 || turnDirection != 0;
        if (!inputDetected && wasInputDetectedLastFrame)
        {
            PublishTwist(0.0f, 0.0f); // 入力が終了したら0を送信
        }

        wasInputDetectedLastFrame = inputDetected; // 入力状態を更新
    }

    private float lastPublishedLinear = 0.0f;
    private float lastPublishedAngular = 0.0f;

    private void UpdateJoystickInput()
    {
        float newLinear = OVRInput.Get(OVRInput.RawAxis2D.LThumbstick).y;
        float newAngular = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick).x;
        bool newLeftRotation = OVRInput.Get(OVRInput.RawButton.LHandTrigger);
        bool newRightRotation = OVRInput.Get(OVRInput.RawButton.RHandTrigger);
        bool rotationInputDetected = newLeftRotation || newRightRotation;

        float linearValue = (newLinear > 0.0f) ? 1.0f : (newLinear < 0.0f) ? -1.0f : 0.0f;
        float angularValue = (newAngular > 0.0f) ? -1.0f : (newAngular < 0.0f) ? 1.0f : 0.0f;

        if (linearValue != lastPublishedLinear || angularValue != lastPublishedAngular)
        {
            PublishTwist(linearValue, angularValue);
            Debug.Log(newLinear);
            Debug.Log(newAngular);
            lastPublishedLinear = linearValue;
            lastPublishedAngular = angularValue;
        }

        if (rotationInputDetected)
        {
            if (!wasRotationDetectedLastFrame)
            {
                PublishRotation(newLeftRotation, newRightRotation); // 回転入力が開始されたら値をパブリッシュ
            }
            wasRotationDetectedLastFrame = true;
        }
        else if (wasRotationDetectedLastFrame)
        {
            PublishRotation(false, false); // 回転入力が終了したら0をパブリッシュ
            wasRotationDetectedLastFrame = false;
        }

        if (newLinear == 0.0f && newAngular == 0.0f && (lastPublishedLinear != 0.0f || lastPublishedAngular != 0.0f))
        {
            PublishTwist(0.0f, 0.0f); // 入力が終了したら0を一度だけパブリッシュ
            lastPublishedLinear = 0.0f;
            lastPublishedAngular = 0.0f;
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
            twist.angular.z = rotationScale;
        }
        else if (rightRotation)
        {
            twist.angular.z = -rotationScale;
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
