using UnityEngine;

public class Bullet2D : MonoBehaviour
{
    [SerializeField] float lifeTime = 3f;
    [SerializeField] int Damage = 10;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("2DEnemy"))
        {
            Enemy2DScript enemy = col.GetComponent<Enemy2DScript>();
            if (enemy != null)
            {
                enemy.TakeDamage(Damage);
            }
            Destroy(gameObject);
        }
        else if (col.CompareTag("2DObstacle"))
        {
            Destroy(gameObject);
        }
    }
}
