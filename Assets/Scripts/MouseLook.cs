using UnityEngine;

/// <summary>
/// �������ת
/// </summary>

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivaty = 400f;
    private Transform playerTrans;
    private float yRotation = 0f;   // �����������ת��ֵ��

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
        yRotation = Mathf.Clamp(yRotation, -60f, 60f); // ����������ת�Ƕȣ�
        transform.localRotation = Quaternion.Euler(yRotation, 0f, 0f);  // Ӧ�õ�ǰ��ת������� - �ӽ������ƶ���

        playerTrans.Rotate(Vector3.up * mouseX); // Vector3.up����y�� - �⼴����y�����ˮƽ��ת - �ӽ������ƶ���
    }
}
