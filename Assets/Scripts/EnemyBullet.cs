using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] float speed = 10f;
    [SerializeField] float lifeTime = 5f;
    [SerializeField] int damage = 2;

    Vector3 direction;
    bool initialized = false;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(Vector3 dir)
    {
        // Use XZ-plane direction and keep Y constant
        direction = new Vector3(dir.x, 0f, dir.z).normalized;
        initialized = true;

        // Instantly rotate the bullet to face the movement direction
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }

    void Update()
    {
        Vector3 moveDir = initialized ? direction : transform.forward;

        transform.position += moveDir * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player2DScript player = other.GetComponent<Player2DScript>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
        else if (other.CompareTag("2DObstacle")) // hit wall or solid object
        {
            Destroy(gameObject);
        }
    }
}
