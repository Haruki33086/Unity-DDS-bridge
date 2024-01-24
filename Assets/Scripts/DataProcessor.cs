using UnityEngine;

public class DataProcessor : MonoBehaviour
{
    private Float64Subscriber dataSubscriber;

    private void Start()
    {
        dataSubscriber = GetComponent<Float64Subscriber>();
    }

    void Update()
    {
        ProcessData(dataSubscriber.LatestData);
    }

    private void ProcessData(double data)
    {
        if (data <= 0.2)
        {
            Camera.main.backgroundColor = Color.red;
        }
        else
        {
            Camera.main.backgroundColor = Color.blue;
        }
    }
}