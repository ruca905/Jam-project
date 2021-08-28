using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    private Rigidbody2D rb;
    List<GameObject> objectsToBlowUp = new List<GameObject>();
    public void OnTriggerEnter2D(Collider2D collider)
    {
        if (!colliderOfAOE.enabled)
            return;
        GameObject colliderGO = collider.gameObject;
        if (!objectsToBlowUp.Contains(colliderGO))
        {
            objectsToBlowUp.Add(colliderGO);
        }
    }
    void Awake()
    {
        bombs.Add(this);
        rb = GetComponent<Rigidbody2D>();
    }
    public static List<Bomb> bombs = new List<Bomb>();
    Vector2 rbVelocity = Vector2.zero;
    [SerializeField]
    private Transform[] raycasters;
    public float radius;
    public void Initialize(float radius, float secondsToBlowUp, float speed, LayerMask layersToStopExplosion, int damage)
    {
        this.secondsToBlowUp = secondsToBlowUp;
        this.radius=radius;
        colliderOfAOE.radius = radius;
        spriteOfAOE.gameObject.transform.localScale = new Vector3(radius * 2, radius * 2, 1);
        rbVelocity = transform.right * speed;
        this.layersToStopExplosion = layersToStopExplosion;
        this.damage=damage;
    }
    private bool coroutineStarted=false;
    public void OnTriggerStay2D(Collider2D collider)
    {
        if (coroutineStarted)
            return;
        if (collider.gameObject.tag == "Wall" || collider.gameObject.tag == "NDWall")
        {
            // We assume that wall is a 1x1 square
            const float radiusOfWall = 1.3f;
            Vector3 wallPosition = collider.gameObject.transform.position;
            if (transform.position.x >= wallPosition.x - radiusOfWall &&
            transform.position.y >= wallPosition.y - radiusOfWall &&
            transform.position.x <= wallPosition.x + radiusOfWall &&
            transform.position.y <= wallPosition.y + radiusOfWall)
            {
                // We touched the wall, we blow up
                StartCoroutine(blowUp());
                coroutineStarted=true;
                rbVelocity = Vector2.zero;
            }
        }
    }
    public void OnTriggerExit2D(Collider2D collider)
    {
        if (!colliderOfAOE.enabled)
            return;
        GameObject colliderGO = collider.gameObject;
        if (objectsToBlowUp.Contains(colliderGO))
        {
            objectsToBlowUp.Remove(colliderGO);
        }
    }
    void Start()
    {
        AreaOfEffectAppears();
    }

    float secondsToBlowUp = 0;
    private int damage = 10;
    [SerializeField]
    private CircleCollider2D colliderOfAOE;
    private LayerMask layersToStopExplosion;

    [SerializeField]
    private SpriteRenderer spriteOfAOE;

    private void AreaOfEffectAppears()
    {
        spriteOfAOE.enabled = true;
    }

    private bool HasDirectLineOfVisionWith(GameObject obj)
    {
        foreach (var raycaster in raycasters)
        {
            // Check if we have direct contact with this raycaster
            Vector3 change = transform.position - raycaster.position;
            RaycastHit2D hit = Physics2D.Raycast(raycaster.position, change, change.magnitude, layersToStopExplosion);
            if (hit.collider != null)
            {
                continue;
            }

            // Check the line of vision
            change = obj.transform.position - raycaster.position;
            hit = Physics2D.Raycast(raycaster.position, change, change.magnitude, layersToStopExplosion);
            // If it doesn't hit something that stops explosion...
            if (hit.collider == null || hit.collider.gameObject.GetInstanceID() == obj.GetInstanceID())
            {
                // Then we have direct line of vision!
                return true;
            }
        }
        return false;
    }

    private void AreaOfEffectGoesOff()
    {
        // Disabling collider makes both trigger functions above know about the fact, that no more changes to list is allowed.
        colliderOfAOE.enabled = false;
        List<GameObject> realObjectsToBlowUp = new List<GameObject>(); 
        foreach (var obj in objectsToBlowUp)
        {
            if (obj != null)
            {
                // Not worth it to raycast for unbreakable walls
                if (obj.tag == "NDWall")
                    continue;
                
                if (!HasDirectLineOfVisionWith(obj))
                    continue;
                realObjectsToBlowUp.Add(obj);
            }
        }
        foreach (var obj in realObjectsToBlowUp){
                if (obj.tag == "Wall")
                {
                    // Destroy wall
                    Destroy(obj);
                }
                else
                {
                    Character character = obj.GetComponent<Character>();
                    if (character != null)
                    {
                        // Damage characters
                        character.TakeDamage(damage);
                    }
                }
            }
    }
    private void ExplosionAnimationStarts()
    {
        //TODO explosion animation
    }


    public IEnumerator blowUp()
    {
        BombRotation br = GetComponentInChildren<BombRotation>();
        br.enabled=false;
        yield return new WaitForSeconds(secondsToBlowUp);
        AreaOfEffectGoesOff();
        CameraFollower.singleton.explosionSound2.Play();
        bombs.Remove(this);
        Destroy(gameObject);
    }

    void Update()
    {
        rb.velocity = rbVelocity;
    }
}
