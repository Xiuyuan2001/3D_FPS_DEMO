using UnityEngine;

/// <summary>
/// 摄像机旋转
/// </summary>

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivaty = 400f;
    private Transform playerTrans;
    private float yRotation = 0f;   // 摄像机上下旋转数值；

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        playerTrans = GetComponentInParent<PlayerController>().transform;
    }

    private void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivaty * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivaty * Time.deltaTime;

        yRotation -= mouseY;
        yRotation = Mathf.Clamp(yRotation, -60f, 60f); // 限制上下旋转角度；
        transform.localRotation = Quaternion.Euler(yRotation, 0f, 0f);  // 应用当前旋转到摄像机 - 视角上下移动；

        playerTrans.Rotate(Vector3.up * mouseX); // Vector3.up代表y轴 - 意即沿着y轴进行水平旋转 - 视角左右移动；
    }
}
