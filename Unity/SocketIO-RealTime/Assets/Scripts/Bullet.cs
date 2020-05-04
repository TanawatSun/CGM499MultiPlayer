using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
   
    [SerializeField] float bulletSpeed = 0.5f;
    float time = 2f;
    CircleCollider2D b;
    SpriteRenderer sr;
    public string hitName = "";
    // Start is called before the first frame update
    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.AddForce(transform.up * bulletSpeed, ForceMode2D.Impulse);
        // b = GetComponent<CircleCollider2D>();
        //b.enabled = false;
        sr = GetComponent<SpriteRenderer>();
        sr.enabled = true;
    }
    private void Update()
    {
       /* time -= Time.deltaTime;
        if(time<=0)
        {
            time = 2f;
            //b.enabled = true;
        }*/
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        sr.enabled = false;
        hitName = collision.gameObject.name;
    }
}
