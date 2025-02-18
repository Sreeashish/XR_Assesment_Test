using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    [Header("Shooting Attributes")]
    public Transform firePoint, spellBottle;
    public float damagePerSpell;
    public float fireRate, spellSpeed, spellRange;
    public int spellPoolSize;
    public GameObject spellObject;

    [Header("Crosshair & Aiming")]
    public RectTransform crosshair;
    public LayerMask enemyLayer;

    private float nextFireTime = 0f;
    private Queue<GameObject> bulletPool;
    public bool isCasting = false;

    void Start()
    {
        bulletPool = new Queue<GameObject>();

        for (int i = 0; i < spellPoolSize; i++)
        {
            GameObject bullet = Instantiate(spellObject, spellBottle);
            bullet.SetActive(false);
            bulletPool.Enqueue(bullet);
        }
    }

    public void CastSpell()
    {
       if (Input.GetMouseButton(0)) 
        {
            if (!isCasting)
            {
                isCasting = true;
                StartCoroutine(ShootSpell());
            }
        }
        else
        {
            isCasting = false;
        }
    }

    IEnumerator ShootSpell()
    {
        while (isCasting)
        {
            if (Time.time > nextFireTime)
            {
                FireBullet();
                nextFireTime = Time.time + fireRate;
            }

            yield return null;
        }
    }

    void FireBullet()
{
    if (bulletPool.Count > 0)
    {
        GameObject bullet = bulletPool.Dequeue();
        bullet.SetActive(true);

        bullet.transform.position = firePoint.position;

        Vector3 crosshairAimPoint = crosshair.position;
        Ray ray = MainController.instance.playerController.mainCamera.ScreenPointToRay(crosshairAimPoint);
        RaycastHit hit;

                if (Physics.Raycast(ray, out hit, spellRange, enemyLayer))
        {
            Vector3 target = hit.point;
            Vector3 direction = (target - bullet.transform.position).normalized;

            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.velocity = direction * spellSpeed;
            }
        }
        else
        {
            bullet.SetActive(false);
            bulletPool.Enqueue(bullet);
        }

        StartCoroutine(ReturnBulletToPool(bullet));
    }
}


    IEnumerator ReturnBulletToPool(GameObject bullet)
    {
        yield return new WaitForSeconds(1f);
        bullet.SetActive(false);
        bulletPool.Enqueue(bullet);
    }
}
