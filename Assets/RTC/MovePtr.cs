using UnityEngine;
using Unity.XR.PXR;
using UnityEngine.XR; 

public class MovePtr : MonoBehaviour
{
    float speed_r = 50f;
    public Transform Player; 
   
    void Update()
    { 
        Move(); 
    }
     
    private void Move()
    {
        // 移动
        if (PXR_Input.IsControllerConnected(PXR_Input.Controller.LeftController))
        {
            var leftCtr = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (leftCtr.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
            { 
                Player.Translate(new Vector3(axis.x, 0, axis.y) * Time.deltaTime * 5);
            }
        }

        // 转向
        if (PXR_Input.IsControllerConnected(PXR_Input.Controller.RightController))
        {
            var rightCtr = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightCtr.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
            {
                var h = axis.x;
                Player.Rotate(Vector3.up, h * speed_r * Time.deltaTime);
                var hight = axis.y;
                Player.Translate(Vector3.up * hight * Time.deltaTime * 5);
            }
        }

#if UNITY_EDITOR
        {
            var h = Input.GetAxis("Horizontal");
            if (Mathf.Abs(h) > 0.05f)
            {
                Player.Rotate(Vector3.up, speed_r * h * Time.deltaTime);
            }
        }
#endif
    } 
}