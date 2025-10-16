using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Weapon_AutomaticGun : Weapon
{
    [Header("Shoot Info")]
    [Tooltip("���λ��")]public Transform rayShootPoint;             // ���ߴ��λ��
    public Transform bulletShootPoint;
    [Tooltip("�����׳�λ��")]public Transform casingBulletSpawnPoint; // �׵���λ��


    [Header("Gun Stats")]
    public float range;
    public float fireRate;
    private float originRate;
    private float spreadFactor; // ������
    private float bulletForce;  // �ӵ��������

    [Header("Bullets Stats")]
    [SerializeField] private int bulletMagCount;
    [SerializeField] private int currentBulletsInMag;
    [SerializeField] private int bulletsLeft;

    [Header("Status")]
    public bool isReloading;

    private float fireTimer;     // �������ٵļ�ʱ��

    private void Start()
    {
        range = 300f;

        bulletsLeft = bulletMagCount * 5;       // Ĭ�������ϻ����
        currentBulletsInMag = bulletMagCount;        // Ĭ�ϵ�ǰ�ӵ���Ϊ��ϻ�ӵ���

        rayShootPoint = GameObject.Find("RayShootPoint").transform;
        bulletShootPoint = GameObject.Find("BulletShootPoint").transform;
        casingBulletSpawnPoint = GameObject.Find("CasingBulletSpawnPoint").transform;
    }

    private void Update()
    {
        if (fireTimer < fireRate)
            fireTimer += Time.deltaTime;

        if (Input.GetMouseButton(0))
        {
            GunFire();
        }

        if (currentBulletsInMag == 0 || Input.GetKeyDown(KeyCode.R)) 
        {
            isReloading = true;
            StartCoroutine(ReloadLogic());
            Reload();
        }
    }


    public override void GunFire()
    {
        if (fireTimer < fireRate || currentBulletsInMag <= 0 || isReloading) return;

        currentBulletsInMag--;

        fireTimer = 0;

        Vector3 shootDir = rayShootPoint.forward;
        RaycastHit hit;
        shootDir = shootDir + rayShootPoint.TransformDirection(new Vector3(Random.Range(-spreadFactor, spreadFactor), Random.Range(-spreadFactor, spreadFactor)));

        if (Physics.Raycast(rayShootPoint.position, shootDir, out hit, range)) 
        {
            Debug.Log("Hit!");
        }
    }

    public override void AimIn()
    {
        throw new System.NotImplementedException();
    }

    public override void AimOut()
    {
        throw new System.NotImplementedException();
    }

    public override void DoReloadAnimation()
    {
        Debug.Log("Redloading(Animation!)");
    }

    public override void ExpaningCrossUpdate(float expanDegree)
    {
        throw new System.NotImplementedException();
    }


    public override void Reload()
    {
        currentBulletsInMag = bulletMagCount;
        bulletsLeft -= bulletMagCount;
    }

    private IEnumerator ReloadLogic()
    {
        DoReloadAnimation();
        yield return new WaitForSeconds(1);
        isReloading = false;
    }
}
